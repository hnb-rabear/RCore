# RevCore.Addressables Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `com.rabear.revcore.addressables` v1.0.0 — a standalone RevCore package that wraps Unity Addressables behind a UniTask-first API plus serializable wrapper types, replacing RCore's `AddressableUtil` + asset-ref family without depending on any other RevCore package.

**Architecture:** Single Unity package with `Runtime/` only (no `Editor/`). Four static helper classes (`AddressableLoader`, `AddressableDownloader`, `AddressableCatalog`, `AddressableScene`) plus four serializable wrappers (`AssetRef<T>`, `KeyedAssetRef<TKey, T>`, `ComponentRef<TComponent>`, `PrefabRef<TComponent>`) and one exception (`AddressableLoadException`). All async methods return `UniTask`/`UniTask<T>`, take `CancellationToken ct = default`, take `IProgress<float> progress = null` when applicable, throw `AddressableLoadException` on failure. Cancellation does NOT eagerly release in-flight handles — helpers attach a `Completed += op => Addressables.Release(op)` continuation. EditMode tests use a fake `IResourceLocator` registered via `Addressables.AddResourceLocator`; PlayMode tests cover scene + instantiation.

**Tech Stack:** Unity 2022.3, `com.unity.addressables` 1.22.x, UniTask 2.5.10, NUnit, Unity Test Framework.

**Spec:** [`docs/superpowers/specs/2026-05-19-revcore-addressables-design.md`](../specs/2026-05-19-revcore-addressables-design.md). All API signatures, error semantics, lifetime contract, and migration map are in the spec — refer to it when this plan is ambiguous.

**Conventions (from CLAUDE.md + neighbor modules):**

- Private instance fields: `m_camelCase`. Static: `s_camelCase`. Public: `PascalCase`.
- `.cs` files: tabs + CRLF (enforced via `.gitattributes`).
- Test methods: `snake_case_descriptive`.
- Every public member needs `/// <summary>` XML doc.
- Each new public symbol → line in `Runtime/PublicAPI.Unshipped.txt`.
- CHANGELOG entry per commit. Each commit is its own PR-sized scope.
- Commits: scope-prefixed (`feat(addressables): ...`, `test(addressables): ...`, `docs(addressables): ...`).

---

## File Structure

```text
Assets/RevCore/Addressables/
├── package.json                                    # CREATE
├── README.md                                       # CREATE
├── CHANGELOG.md                                    # CREATE
├── Runtime/
│   ├── RevCore.Addressables.Runtime.asmdef         # CREATE
│   ├── csc.rsp                                     # CREATE
│   ├── PublicAPI.Shipped.txt                       # CREATE (empty + nullable header)
│   ├── PublicAPI.Unshipped.txt                     # CREATE
│   ├── Exceptions/
│   │   └── AddressableLoadException.cs             # CREATE
│   ├── Helpers/
│   │   ├── AddressableLoader.cs                    # CREATE
│   │   ├── AddressableDownloader.cs                # CREATE
│   │   ├── AddressableCatalog.cs                   # CREATE
│   │   └── AddressableScene.cs                     # CREATE
│   └── Wrappers/
│       ├── AssetRef.cs                             # CREATE
│       ├── KeyedAssetRef.cs                        # CREATE
│       ├── ComponentRef.cs                         # CREATE
│       └── PrefabRef.cs                            # CREATE
└── Tests/
    ├── RevCore.Addressables.Tests.asmdef           # CREATE
    ├── Fixtures/
    │   ├── FakeResourceLocator.cs                  # CREATE (helper, not under test)
    │   └── AddressableTestFixture.cs               # CREATE (NUnit fixture base)
    ├── Editor/
    │   ├── AddressableLoaderTests.cs               # CREATE
    │   ├── AddressableDownloaderTests.cs           # CREATE
    │   ├── AddressableCatalogTests.cs              # CREATE
    │   ├── AssetRefTests.cs                        # CREATE
    │   ├── KeyedAssetRefTests.cs                   # CREATE
    │   └── ComponentRefTests.cs                    # CREATE
    └── Runtime/
        ├── AddressableSceneTests.cs                # CREATE (PlayMode)
        ├── InstantiateAsyncTests.cs                # CREATE (PlayMode)
        └── PrefabRefTests.cs                       # CREATE (PlayMode)

scripts/xmldoc-baseline.json                        # MODIFY (add new module entry)
docs/migration/rcore-to-revcore-api-map.csv         # MODIFY (append rows)
```

Each file has one responsibility. Helpers/Wrappers/Exceptions split mirrors the spec's §4–§6 split. Test fixtures are isolated so production code never references test types.

---

## Task 0: Pre-flight & branch setup

**Files:** none (git state only).

- [ ] **Step 0.1: Confirm working tree clean on `main`**

  Run:
  ```powershell
  git status
  git branch --show-current
  ```
  Expected: branch is `main` (or you switch to it), no staged changes related to this work.

  If unrelated dirty files (e.g., `Assets/SheetXExample/Scripts/Generated/*` line-ending drift mentioned in `SESSION_HANDOFF.md`), leave them unstaged; do NOT include them.

- [ ] **Step 0.2: Create feature branch**

  Run:
  ```powershell
  git checkout -b feat/addressables-v1.0
  ```

- [ ] **Step 0.3: Verify project's Addressables + UniTask versions**

  Run:
  ```powershell
  Select-String "addressables|unitask" Packages/manifest.json
  ```
  Expected output contains `com.unity.addressables` `1.22.x` and `com.cysharp.unitask` (the project may pin UniTask via Git URL instead of semantic version).

  If Addressables drifts outside 1.22.x or UniTask is missing, halt and update the spec before proceeding.

- [ ] **Step 0.4: Run xmldoc coverage gate baseline**

  Run:
  ```powershell
  python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
  ```
  Expected: passes with current coverage (no regressions). Record the baseline number for later comparison.

---

## Task 1: Package scaffold

Create the empty package skeleton so Unity recognises it before any code lands.

**Files:**
- Create: `Assets/RevCore/Addressables/package.json`
- Create: `Assets/RevCore/Addressables/README.md`
- Create: `Assets/RevCore/Addressables/CHANGELOG.md`
- Create: `Assets/RevCore/Addressables/Runtime/RevCore.Addressables.Runtime.asmdef`
- Create: `Assets/RevCore/Addressables/Runtime/csc.rsp`
- Create: `Assets/RevCore/Addressables/Runtime/PublicAPI.Shipped.txt`
- Create: `Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt`

- [ ] **Step 1.1: Write `package.json`**

  ```json
  {
    "name": "com.rabear.revcore.addressables",
    "version": "1.0.0",
    "displayName": "RevCore.Addressables",
    "description": "UniTask-first helpers and serializable references over Unity Addressables for the RevCore framework.",
    "unity": "2022.3",
    "documentationUrl": "https://github.com/hnb-rabear/RCore",
    "author": {
      "name": "HNB RaBear",
      "email": "nbhung71711@gmail.com",
      "url": "https://github.com/hnb-rabear"
    },
    "keywords": ["addressables", "asset", "loader", "unitask", "framework"],
    "dependencies": {
      "com.unity.addressables": "1.22.0",
      "com.cysharp.unitask": "2.5.10"
    }
  }
  ```

  Note: `dependencies` uses the minimum supported version, not the currently pinned `1.22.3`. UPM will accept any compatible higher version installed at the project level.

- [ ] **Step 1.2: Write `README.md`**

  Use the structure from `Assets/RevCore/Timer/README.md` as a template. Include:
  - Install section (UPM local path + published name).
  - 60-second quickstart showing `AddressableLoader.LoadAssetAsync<AudioClip>("MainTheme")` and a wrapper field usage.
  - Section "Unity serialization caveat" pasted from spec §5.
  - Migration link to `docs/migration/rcore-to-revcore-api-map.csv`.

  Full README text is written in Task 14; for this scaffold step a one-line placeholder body is acceptable:

  ```markdown
  # RevCore.Addressables

  UniTask-first wrapper over Unity Addressables for RevCore. See spec for full design.
  ```

- [ ] **Step 1.3: Write `CHANGELOG.md`**

  ```markdown
  # Changelog

  ## [Unreleased]

  ### Added

  - Package scaffold (`package.json`, `Runtime` asmdef, `PublicAPI` baselines, `csc.rsp`).
  ```

- [ ] **Step 1.4: Write `Runtime/RevCore.Addressables.Runtime.asmdef`**

  ```json
  {
    "name": "RevCore.Addressables.Runtime",
    "rootNamespace": "RevCore",
    "references": [
      "Unity.Addressables",
      "Unity.ResourceManager",
      "UniTask",
      "UniTask.Addressables"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
  }
  ```

- [ ] **Step 1.5: Write `Runtime/csc.rsp`**

  ```text
  /additionalfile:Assets/RevCore/Addressables/Runtime/PublicAPI.Shipped.txt
  /additionalfile:Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt
  ```

  (LF line endings, no trailing newline if neighbour modules don't have one — match `Assets/RevCore/Timer/Runtime/csc.rsp`.)

- [ ] **Step 1.6: Write `Runtime/PublicAPI.Shipped.txt`**

  Content (single line + trailing newline):
  ```text
  #nullable enable
  ```

- [ ] **Step 1.7: Write `Runtime/PublicAPI.Unshipped.txt`**

  Content:
  ```text
  #nullable enable
  ```

- [ ] **Step 1.8: Open Unity, let it import the package, verify no compile errors**

  Use the Unity editor; check the Console for asmdef/import errors. Expected: clean compile, no Addressables-related script load errors. If the editor is not available locally, skip this step but note it in the commit message; CI must run before merge.

- [ ] **Step 1.9: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/package.json Assets/RevCore/Addressables/README.md Assets/RevCore/Addressables/CHANGELOG.md Assets/RevCore/Addressables/Runtime/RevCore.Addressables.Runtime.asmdef Assets/RevCore/Addressables/Runtime/csc.rsp Assets/RevCore/Addressables/Runtime/PublicAPI.Shipped.txt Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt
  git commit -m "feat(addressables): scaffold RevCore.Addressables package (v1.0.0)"
  ```

  Unity `.meta` files will be created on next editor open — include them when staging by re-running `git add Assets/RevCore/Addressables/`.

---

## Task 2: AddressableLoadException

The exception type is needed by every helper, so it lands first and gets a dedicated test.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Exceptions/AddressableLoadException.cs`
- Create: `Assets/RevCore/Addressables/Tests/RevCore.Addressables.Tests.asmdef`
- Create: `Assets/RevCore/Addressables/Tests/Editor/AddressableLoadExceptionTests.cs`

- [ ] **Step 2.1: Write the test asmdef**

  Path: `Assets/RevCore/Addressables/Tests/RevCore.Addressables.Tests.asmdef`

  ```json
  {
    "name": "RevCore.Addressables.Tests",
    "rootNamespace": "RevCore.Tests",
    "references": [
      "RevCore.Addressables.Runtime",
      "Unity.Addressables",
      "Unity.ResourceManager",
      "UnityEngine.TestRunner",
      "UnityEditor.TestRunner",
      "UniTask",
      "UniTask.Addressables"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
      "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
      "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
  }
  ```

  Note: `includePlatforms: []` (not `["Editor"]`) — needed for PlayMode tests added later. EditMode/PlayMode split happens via `[Test]` vs `[UnityTest]` attributes, not asmdef platform.

- [ ] **Step 2.2: Write the failing test**

  Path: `Assets/RevCore/Addressables/Tests/Editor/AddressableLoadExceptionTests.cs`

  ```csharp
  using System;
  using NUnit.Framework;
  using RevCore;
  using UnityEngine.ResourceManagement.AsyncOperations;

  namespace RevCore.Tests
  {
  	public class AddressableLoadExceptionTests
  	{
  		[Test]
  		public void constructor_populates_key_status_and_inner_exception()
  		{
  			var inner = new InvalidOperationException("boom");
  			var ex = new AddressableLoadException("MyKey", AsyncOperationStatus.Failed, inner);

  			Assert.AreEqual("MyKey", ex.Key);
  			Assert.AreEqual(AsyncOperationStatus.Failed, ex.Status);
  			Assert.AreSame(inner, ex.InnerException);
  			StringAssert.Contains("MyKey", ex.Message);
  			StringAssert.Contains("Failed", ex.Message);
  		}
  	}
  }
  ```

- [ ] **Step 2.3: Run the test, verify it fails**

  From Unity Test Runner (Window → General → Test Runner) or via CLI:
  ```powershell
  Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory "" -testResults TestResults.xml -quit
  ```
  Expected: compile error — `AddressableLoadException` not defined.

- [ ] **Step 2.4: Implement `AddressableLoadException`**

  Path: `Assets/RevCore/Addressables/Runtime/Exceptions/AddressableLoadException.cs`

  ```csharp
  using System;
  using UnityEngine.ResourceManagement.AsyncOperations;

  namespace RevCore
  {
  	/// <summary>
  	/// Thrown when an Addressables operation fails. Wraps the underlying operation exception and carries the originating key.
  	/// </summary>
  	public sealed class AddressableLoadException : Exception
  	{
  		/// <summary>The key (address, <see cref="UnityEngine.AddressableAssets.AssetReference"/>, or location) that failed to load.</summary>
  		public object Key { get; }

  		/// <summary>The terminal status reported by the failed operation.</summary>
  		public AsyncOperationStatus Status { get; }

  		/// <summary>Initialises a new <see cref="AddressableLoadException"/>.</summary>
  		/// <param name="key">The originating key for the failed load.</param>
  		/// <param name="status">The operation's final status.</param>
  		/// <param name="inner">The underlying exception reported by Addressables, if any.</param>
  		public AddressableLoadException(object key, AsyncOperationStatus status, Exception inner)
  			: base($"Addressable load failed: {key} (status={status})", inner)
  		{
  			Key = key;
  			Status = status;
  		}
  	}
  }
  ```

- [ ] **Step 2.5: Run the test, verify it passes**

  Expected: 1 test passes, 0 fail.

- [ ] **Step 2.6: Append to `PublicAPI.Unshipped.txt`**

  Append (after the existing `#nullable enable` line):
  ```text
  RevCore.AddressableLoadException
  RevCore.AddressableLoadException.AddressableLoadException(object key, UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus status, System.Exception inner) -> void
  RevCore.AddressableLoadException.Key.get -> object
  RevCore.AddressableLoadException.Status.get -> UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus
  ```

- [ ] **Step 2.7: Update CHANGELOG `[Unreleased]`**

  Under `### Added`, append:
  ```markdown
  - `AddressableLoadException` exception type with `Key` and `Status` properties.
  ```

- [ ] **Step 2.8: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Exceptions/ Assets/RevCore/Addressables/Tests/RevCore.Addressables.Tests.asmdef Assets/RevCore/Addressables/Tests/Editor/AddressableLoadExceptionTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): add AddressableLoadException (red→green: constructor_populates_key_status_and_inner_exception)"
  ```

---

## Task 3: Test fixtures (FakeResourceLocator + AddressableTestFixture)

Shared test scaffolding so every helper test can register fake addressables without hitting disk. Lands as its own commit — these are utilities, not production code, and they have no failing test of their own (they exist to make other tests possible).

**Files:**
- Create: `Assets/RevCore/Addressables/Tests/Fixtures/FakeResourceLocator.cs`
- Create: `Assets/RevCore/Addressables/Tests/Fixtures/AddressableTestFixture.cs`

- [ ] **Step 3.1: Write `FakeResourceLocator`**

  Path: `Assets/RevCore/Addressables/Tests/Fixtures/FakeResourceLocator.cs`

  ```csharp
  using System;
  using System.Collections.Generic;
  using UnityEngine.AddressableAssets.ResourceLocators;
  using UnityEngine.ResourceManagement.ResourceLocations;
  using UnityEngine.ResourceManagement.ResourceProviders;

  namespace RevCore.Tests
  {
  	/// <summary>
  	/// Test-only <see cref="IResourceLocator"/> that returns canned <see cref="IResourceLocation"/> entries
  	/// for a fixed set of keys. Allows EditMode tests to exercise <c>Addressables.LoadAssetAsync</c> without
  	/// the real catalog or asset bundles.
  	/// </summary>
  	internal sealed class FakeResourceLocator : IResourceLocator
  	{
  		private readonly Dictionary<object, IList<IResourceLocation>> m_map = new();

  		public string LocatorId => nameof(FakeResourceLocator);
  		public IEnumerable<object> Keys => m_map.Keys;

  		public void Register(object key, IResourceLocation location)
  		{
  			if (!m_map.TryGetValue(key, out var list))
  			{
  				list = new List<IResourceLocation>();
  				m_map[key] = list;
  			}
  			list.Add(location);
  		}

  		public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
  		{
  			return m_map.TryGetValue(key, out locations);
  		}

  		public sealed class FailingLocation : IResourceLocation
  		{
  			public FailingLocation(string key, Type type)
  			{
  				PrimaryKey = key;
  				InternalId = key;
  				ResourceType = type;
  				ProviderId = "RevCore.Tests.FailingProvider";
  			}

  			public string InternalId { get; }
  			public string ProviderId { get; }
  			public IList<IResourceLocation> Dependencies => null;
  			public int DependencyHashCode => 0;
  			public bool HasDependencies => false;
  			public object Data => null;
  			public string PrimaryKey { get; }
  			public Type ResourceType { get; }
  			public int Hash(Type resultType) => PrimaryKey.GetHashCode();
  		}
  	}
  }
  ```

- [ ] **Step 3.2: Write `AddressableTestFixture`**

  Path: `Assets/RevCore/Addressables/Tests/Fixtures/AddressableTestFixture.cs`

  ```csharp
  using NUnit.Framework;
  using UnityEngine.AddressableAssets;
  using UnityEngine.AddressableAssets.ResourceLocators;

  namespace RevCore.Tests
  {
  	/// <summary>
  	/// NUnit base fixture that registers a <see cref="FakeResourceLocator"/> with the global
  	/// Addressables system in <see cref="SetUp"/> and removes it in <see cref="TearDown"/>.
  	/// Tests derive from this and call <see cref="Locator"/>.Register(...) inside the test body.
  	/// </summary>
  	public abstract class AddressableTestFixture
  	{
  		internal FakeResourceLocator Locator { get; private set; }

  		[SetUp]
  		public void BaseSetUp()
  		{
  			Locator = new FakeResourceLocator();
  			Addressables.AddResourceLocator(Locator);
  		}

  		[TearDown]
  		public void BaseTearDown()
  		{
  			if (Locator != null)
  			{
  				Addressables.RemoveResourceLocator(Locator);
  				Locator = null;
  			}
  		}
  	}
  }
  ```

- [ ] **Step 3.3: Run the test suite, verify it still compiles**

  ```powershell
  Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit
  ```
  Expected: existing `AddressableLoadExceptionTests` still passes; no compile errors from new fixture files.

- [ ] **Step 3.4: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/Tests/Fixtures/
  git commit -m "test(addressables): add FakeResourceLocator + AddressableTestFixture base"
  ```

---

## Task 4: AddressableLoader — LoadAssetAsync (string address)

First helper. Single-address load with progress + cancellation, throws `AddressableLoadException` on failure.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableLoader.cs`
- Create: `Assets/RevCore/Addressables/Tests/Editor/AddressableLoaderTests.cs`

- [ ] **Step 4.1: Write failing test — happy path**

  Path: `Assets/RevCore/Addressables/Tests/Editor/AddressableLoaderTests.cs`

  ```csharp
  using System;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using NUnit.Framework;
  using RevCore;
  using UnityEngine;
  using UnityEngine.ResourceManagement.AsyncOperations;

  namespace RevCore.Tests
  {
  	public class AddressableLoaderTests : AddressableTestFixture
  	{
  		[Test]
  		public void load_asset_async_with_unknown_address_throws_AddressableLoadException()
  		{
  			Assert.ThrowsAsync<AddressableLoadException>(async () =>
  			{
  				await AddressableLoader.LoadAssetAsync<Texture2D>("does-not-exist");
  			});
  		}

  		[Test]
  		public void load_asset_async_carries_address_as_key_on_failure()
  		{
  			var ex = Assert.ThrowsAsync<AddressableLoadException>(async () =>
  			{
  				await AddressableLoader.LoadAssetAsync<Texture2D>("missing-key");
  			});
  			Assert.AreEqual("missing-key", ex.Key);
  			Assert.AreEqual(AsyncOperationStatus.Failed, ex.Status);
  		}

  		[Test]
  		public void load_asset_async_honours_cancellation_token()
  		{
  			using var cts = new CancellationTokenSource();
  			cts.Cancel();
  			Assert.ThrowsAsync<OperationCanceledException>(async () =>
  			{
  				await AddressableLoader.LoadAssetAsync<Texture2D>("any", ct: cts.Token);
  			});
  		}
  	}
  }
  ```

- [ ] **Step 4.2: Run, verify it fails**

  Expected: compile error — `AddressableLoader` not defined.

- [ ] **Step 4.3: Implement `AddressableLoader.LoadAssetAsync<T>(string, ...)`**

  Path: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableLoader.cs`

  ```csharp
  using System;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using UnityEngine;
  using UnityEngine.AddressableAssets;
  using UnityEngine.ResourceManagement.AsyncOperations;
  using Object = UnityEngine.Object;

  namespace RevCore
  {
  	/// <summary>
  	/// UniTask-first wrappers over <see cref="Addressables"/> for loading single assets, batches, and instances.
  	/// All methods throw <see cref="AddressableLoadException"/> on terminal failure and honour the supplied
  	/// <see cref="CancellationToken"/>. Cancellation does not eagerly release in-flight handles; a
  	/// <c>Completed</c> continuation releases each handle as soon as the underlying operation finishes.
  	/// </summary>
  	public static class AddressableLoader
  	{
  		/// <summary>Asynchronously loads an asset of type <typeparamref name="T"/> by string address.</summary>
  		/// <typeparam name="T">The asset type to load.</typeparam>
  		/// <param name="address">The Addressables key/address.</param>
  		/// <param name="progress">Optional progress reporter (0–1).</param>
  		/// <param name="ct">Cancellation token. On cancellation the handle is auto-released via a <c>Completed</c> continuation.</param>
  		/// <returns>The loaded asset.</returns>
  		/// <exception cref="AddressableLoadException">Thrown when the operation terminates in <see cref="AsyncOperationStatus.Failed"/>.</exception>
  		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
  		public static async UniTask<T> LoadAssetAsync<T>(string address, IProgress<float> progress = null, CancellationToken ct = default) where T : Object
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.LoadAssetAsync<T>(address);
  			return await AwaitOrThrow(handle, address, progress, ct);
  		}

  		internal static async UniTask<T> AwaitOrThrow<T>(AsyncOperationHandle<T> handle, object key, IProgress<float> progress, CancellationToken ct)
  		{
  			try
  			{
  				return await handle.ToUniTask(progress, cancellationToken: ct);
  			}
  			catch (OperationCanceledException)
  			{
  				ReleaseOnComplete(handle);
  				throw;
  			}
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				if (handle.IsValid()) Addressables.Release(handle);
  				throw new AddressableLoadException(key, status, ex);
  			}
  		}

  		private static void ReleaseOnComplete<T>(AsyncOperationHandle<T> handle)
  		{
  			if (!handle.IsValid()) return;
  			if (handle.IsDone)
  			{
  				Addressables.Release(handle);
  				return;
  			}
  			handle.Completed += op => Addressables.Release(op);
  		}
  	}
  }
  ```

  Note: `ToUniTask(progress, cancellationToken)` lives in `UniTask.Addressables` assembly (already in asmdef refs).

- [ ] **Step 4.4: Run, verify all three tests pass**

  Expected: 4 tests pass total (1 from Task 2 + 3 new).

- [ ] **Step 4.5: Append to `PublicAPI.Unshipped.txt`**

  ```text
  RevCore.AddressableLoader
  static RevCore.AddressableLoader.LoadAssetAsync<T>(string address, System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<T>
  ```

- [ ] **Step 4.6: Update CHANGELOG**

  Under `### Added`, append:
  ```markdown
  - `AddressableLoader.LoadAssetAsync<T>(string address, IProgress<float>, CancellationToken)` — single-address load with throw-on-failure semantics.
  ```

- [ ] **Step 4.7: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Helpers/AddressableLoader.cs Assets/RevCore/Addressables/Tests/Editor/AddressableLoaderTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): AddressableLoader.LoadAssetAsync(string) (red→green: load_asset_async_*)"
  ```

---

## Task 5: AddressableLoader — AssetReference overloads + handle variant

Add the `AssetReference` and `AssetReferenceT<T>` overloads plus the advanced `LoadAssetWithHandleAsync<T>` form (caller owns the handle, no auto-release).

**Files:**
- Modify: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableLoader.cs`
- Modify: `Assets/RevCore/Addressables/Tests/Editor/AddressableLoaderTests.cs`

- [ ] **Step 5.1: Append failing tests for the reference overloads + handle variant**

  Append inside the `AddressableLoaderTests` class:

  ```csharp
  [Test]
  public void load_asset_async_with_invalid_AssetReference_throws()
  {
  	var reference = new UnityEngine.AddressableAssets.AssetReference("00000000000000000000000000000000");
  	Assert.ThrowsAsync<AddressableLoadException>(async () =>
  	{
  		await AddressableLoader.LoadAssetAsync<Texture2D>(reference);
  	});
  }

  [Test]
  public void load_asset_with_handle_async_returns_handle_for_caller_release()
  {
  	Assert.ThrowsAsync<AddressableLoadException>(async () =>
  	{
  		var handle = await AddressableLoader.LoadAssetWithHandleAsync<Texture2D>("missing");
  		Assert.Fail("Expected exception, got " + handle);
  	});
  }
  ```

- [ ] **Step 5.2: Run, verify the two new tests fail to compile**

  Expected: `LoadAssetAsync(AssetReference)` and `LoadAssetWithHandleAsync` missing.

- [ ] **Step 5.3: Extend `AddressableLoader`**

  Append inside `AddressableLoader`:

  ```csharp
  /// <summary>Asynchronously loads an asset via an <see cref="AssetReference"/>.</summary>
  public static async UniTask<T> LoadAssetAsync<T>(AssetReference reference, IProgress<float> progress = null, CancellationToken ct = default) where T : Object
  {
  	if (reference == null) throw new ArgumentNullException(nameof(reference));
  	ct.ThrowIfCancellationRequested();
  	var handle = reference.LoadAssetAsync<T>();
  	return await AwaitOrThrow(handle, reference, progress, ct);
  }

  /// <summary>Asynchronously loads a strongly-typed asset via an <see cref="AssetReferenceT{TObject}"/>.</summary>
  public static UniTask<T> LoadAssetAsync<T>(AssetReferenceT<T> reference, IProgress<float> progress = null, CancellationToken ct = default) where T : Object
  	=> LoadAssetAsync<T>((AssetReference)reference, progress, ct);

  /// <summary>
  /// Loads an asset and returns the underlying <see cref="AsyncOperationHandle{TObject}"/>. The caller owns the
  /// handle and is responsible for calling <see cref="Addressables.Release{TObject}"/> when finished.
  /// </summary>
  public static async UniTask<AsyncOperationHandle<T>> LoadAssetWithHandleAsync<T>(string address, IProgress<float> progress = null, CancellationToken ct = default) where T : Object
  {
  	ct.ThrowIfCancellationRequested();
  	var handle = Addressables.LoadAssetAsync<T>(address);
  	try
  	{
  		await handle.ToUniTask(progress, cancellationToken: ct);
  		return handle;
  	}
  	catch (OperationCanceledException)
  	{
  		ReleaseOnComplete(handle);
  		throw;
  	}
  	catch (Exception ex)
  	{
  		var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  		if (handle.IsValid()) Addressables.Release(handle);
  		throw new AddressableLoadException(address, status, ex);
  	}
  }
  ```

- [ ] **Step 5.4: Run, verify all loader tests pass**

- [ ] **Step 5.5: Append to `PublicAPI.Unshipped.txt`**

  ```text
  static RevCore.AddressableLoader.LoadAssetAsync<T>(UnityEngine.AddressableAssets.AssetReference reference, System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<T>
  static RevCore.AddressableLoader.LoadAssetAsync<T>(UnityEngine.AddressableAssets.AssetReferenceT<T> reference, System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<T>
  static RevCore.AddressableLoader.LoadAssetWithHandleAsync<T>(string address, System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<T>>
  ```

- [ ] **Step 5.6: CHANGELOG entry**

  ```markdown
  - `AddressableLoader.LoadAssetAsync` overloads for `AssetReference` / `AssetReferenceT<T>` and `LoadAssetWithHandleAsync` for caller-owned lifetimes.
  ```

- [ ] **Step 5.7: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Helpers/AddressableLoader.cs Assets/RevCore/Addressables/Tests/Editor/AddressableLoaderTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): AddressableLoader reference overloads + handle variant"
  ```

---

## Task 6: AddressableLoader — batch load + instantiate + locations + release

Round out the loader: batch loads, instantiate, resource-location query, release helpers.

**Files:**
- Modify: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableLoader.cs`
- Modify: `Assets/RevCore/Addressables/Tests/Editor/AddressableLoaderTests.cs`
- Create: `Assets/RevCore/Addressables/Tests/Runtime/InstantiateAsyncTests.cs` (PlayMode)

- [ ] **Step 6.1: Failing test — batch load failure surface**

  Append to `AddressableLoaderTests`:

  ```csharp
  [Test]
  public void load_assets_async_with_unknown_address_in_batch_throws()
  {
  	Assert.ThrowsAsync<AddressableLoadException>(async () =>
  	{
  		await AddressableLoader.LoadAssetsAsync<Texture2D>(new[] { "missing-a", "missing-b" });
  	});
  }

  [UnityTest]
  public System.Collections.IEnumerator load_resource_locations_async_returns_empty_for_unknown_key() => UniTask.ToCoroutine(async () =>
  {
  	var locations = await AddressableLoader.LoadResourceLocationsAsync("definitely-not-a-key", typeof(Texture2D));
  	Assert.IsNotNull(locations);
  	Assert.AreEqual(0, locations.Count);
  });
  ```

- [ ] **Step 6.2: Failing PlayMode test — instantiate**

  Path: `Assets/RevCore/Addressables/Tests/Runtime/InstantiateAsyncTests.cs`

  ```csharp
  using System.Collections;
  using Cysharp.Threading.Tasks;
  using NUnit.Framework;
  using RevCore;
  using UnityEngine;
  using UnityEngine.TestTools;

  namespace RevCore.Tests
  {
  	public class InstantiateAsyncTests
  	{
  		[UnityTest]
  		public IEnumerator instantiate_async_with_unknown_address_throws_AddressableLoadException() => UniTask.ToCoroutine(async () =>
  		{
  			try
  			{
  				await AddressableLoader.InstantiateAsync("does-not-exist", null);
  				Assert.Fail("Expected AddressableLoadException");
  			}
  			catch (AddressableLoadException)
  			{
  				Assert.Pass();
  			}
  		});
  	}
  }
  ```

- [ ] **Step 6.3: Run, verify failures**

  Expected: compile errors for `LoadAssetsAsync`, `LoadResourceLocationsAsync`, `InstantiateAsync`.

- [ ] **Step 6.4: Extend `AddressableLoader`**

  Append inside `AddressableLoader`:

  ```csharp
  /// <summary>Loads multiple assets in parallel by address list. Throws if any single load fails.</summary>
  public static async UniTask<System.Collections.Generic.IList<T>> LoadAssetsAsync<T>(System.Collections.Generic.IList<string> addresses, CancellationToken ct = default) where T : Object
  {
  	if (addresses == null) throw new ArgumentNullException(nameof(addresses));
  	var tasks = new UniTask<T>[addresses.Count];
  	for (int i = 0; i < addresses.Count; i++)
  		tasks[i] = LoadAssetAsync<T>(addresses[i], progress: null, ct: ct);
  	return await UniTask.WhenAll(tasks);
  }

  /// <summary>Asynchronously instantiates an Addressables prefab. Throws on failure.</summary>
  public static async UniTask<GameObject> InstantiateAsync(string address, Transform parent, bool worldPositionStays = false, CancellationToken ct = default)
  {
  	ct.ThrowIfCancellationRequested();
  	var handle = Addressables.InstantiateAsync(address, parent, worldPositionStays);
  	try
  	{
  		return await handle.ToUniTask(cancellationToken: ct);
  	}
  	catch (OperationCanceledException)
  	{
  		if (handle.IsValid())
  		{
  			if (handle.IsDone && handle.Result != null) Addressables.ReleaseInstance(handle.Result);
  			else handle.Completed += op => { if (op.Result != null) Addressables.ReleaseInstance(op.Result); };
  		}
  		throw;
  	}
  	catch (Exception ex)
  	{
  		var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  		throw new AddressableLoadException(address, status, ex);
  	}
  }

  /// <summary>Queries the Addressables catalog for resource locations matching <paramref name="key"/>.</summary>
  public static async UniTask<System.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> LoadResourceLocationsAsync(object key, Type type = null, CancellationToken ct = default)
  {
  	ct.ThrowIfCancellationRequested();
  	var handle = Addressables.LoadResourceLocationsAsync(key, type);
  	try { return await handle.ToUniTask(cancellationToken: ct); }
  	catch (OperationCanceledException) { if (handle.IsValid()) handle.Completed += op => Addressables.Release(op); throw; }
  	catch (Exception ex)
  	{
  		var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  		if (handle.IsValid()) Addressables.Release(handle);
  		throw new AddressableLoadException(key, status, ex);
  	}
  }

  /// <summary>Releases a previously loaded asset handle. Safe no-op if the handle is invalid.</summary>
  public static void Release<T>(AsyncOperationHandle<T> handle)
  {
  	if (handle.IsValid()) Addressables.Release(handle);
  }

  /// <summary>Releases an instantiated game object created via <see cref="InstantiateAsync"/>.</summary>
  public static bool ReleaseInstance(GameObject instance)
  	=> instance != null && Addressables.ReleaseInstance(instance);
  ```

- [ ] **Step 6.5: Run EditMode + PlayMode, verify all pass**

- [ ] **Step 6.6: Append PublicAPI entries**

  ```text
  static RevCore.AddressableLoader.LoadAssetsAsync<T>(System.Collections.Generic.IList<string> addresses, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<System.Collections.Generic.IList<T>>
  static RevCore.AddressableLoader.InstantiateAsync(string address, UnityEngine.Transform parent, bool worldPositionStays = false, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<UnityEngine.GameObject>
  static RevCore.AddressableLoader.LoadResourceLocationsAsync(object key, System.Type type = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<System.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>>
  static RevCore.AddressableLoader.Release<T>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<T> handle) -> void
  static RevCore.AddressableLoader.ReleaseInstance(UnityEngine.GameObject instance) -> bool
  ```

- [ ] **Step 6.7: CHANGELOG**

  ```markdown
  - `AddressableLoader.LoadAssetsAsync`, `InstantiateAsync`, `LoadResourceLocationsAsync`, `Release`, `ReleaseInstance`.
  ```

- [ ] **Step 6.8: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Helpers/AddressableLoader.cs Assets/RevCore/Addressables/Tests/ Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): AddressableLoader batch + instantiate + locations + release"
  ```

---

## Task 7: AddressableDownloader

Wraps `GetDownloadSizeAsync`, `DownloadDependenciesAsync`, `ClearDependencyCacheAsync`.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableDownloader.cs`
- Create: `Assets/RevCore/Addressables/Tests/Editor/AddressableDownloaderTests.cs`

- [ ] **Step 7.1: Write failing tests**

  ```csharp
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using NUnit.Framework;
  using RevCore;

  namespace RevCore.Tests
  {
  	public class AddressableDownloaderTests
  	{
  		[Test]
  		public void get_download_size_async_for_unknown_key_returns_zero()
  		{
  			var size = AddressableDownloader.GetDownloadSizeAsync("missing").GetAwaiter().GetResult();
  			Assert.AreEqual(0L, size);
  		}

  		[Test]
  		public void clear_dependency_cache_async_for_unknown_key_returns_true_or_false_without_throwing()
  		{
  			Assert.DoesNotThrowAsync(async () =>
  			{
  				await AddressableDownloader.ClearDependencyCacheAsync("missing");
  			});
  		}
  	}
  }
  ```

- [ ] **Step 7.2: Run, verify fails — class missing**

- [ ] **Step 7.3: Implement `AddressableDownloader`**

  Path: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableDownloader.cs`

  ```csharp
  using System;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using UnityEngine.AddressableAssets;
  using UnityEngine.ResourceManagement.AsyncOperations;

  namespace RevCore
  {
  	/// <summary>Helpers around Addressables remote download lifecycle: size query, download, cache clear.</summary>
  	public static class AddressableDownloader
  	{
  		/// <summary>Returns the remote download size in bytes for the given key. 0 means fully cached or unknown.</summary>
  		public static async UniTask<long> GetDownloadSizeAsync(object key, CancellationToken ct = default)
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.GetDownloadSizeAsync(key);
  			try { return await handle.ToUniTask(cancellationToken: ct); }
  			catch (OperationCanceledException) { if (handle.IsValid()) handle.Completed += op => Addressables.Release(op); throw; }
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				if (handle.IsValid()) Addressables.Release(handle);
  				throw new AddressableLoadException(key, status, ex);
  			}
  		}

  		/// <summary>Downloads remote dependencies for <paramref name="key"/>. Throws on failure.</summary>
  		public static async UniTask DownloadDependenciesAsync(object key, IProgress<float> progress = null, CancellationToken ct = default)
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.DownloadDependenciesAsync(key, autoReleaseHandle: false);
  			try
  			{
  				await handle.ToUniTask(progress, cancellationToken: ct);
  				if (handle.IsValid()) Addressables.Release(handle);
  			}
  			catch (OperationCanceledException) { if (handle.IsValid()) handle.Completed += op => Addressables.Release(op); throw; }
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				if (handle.IsValid()) Addressables.Release(handle);
  				throw new AddressableLoadException(key, status, ex);
  			}
  		}

  		/// <summary>Clears the dependency cache for <paramref name="key"/>. Returns the underlying success flag.</summary>
  		public static async UniTask<bool> ClearDependencyCacheAsync(object key, CancellationToken ct = default)
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.ClearDependencyCacheAsync(key, autoReleaseHandle: false);
  			try
  			{
  				var ok = await handle.ToUniTask(cancellationToken: ct);
  				if (handle.IsValid()) Addressables.Release(handle);
  				return ok;
  			}
  			catch (OperationCanceledException) { if (handle.IsValid()) handle.Completed += op => Addressables.Release(op); throw; }
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				if (handle.IsValid()) Addressables.Release(handle);
  				throw new AddressableLoadException(key, status, ex);
  			}
  		}
  	}
  }
  ```

- [ ] **Step 7.4: Run, verify pass**

- [ ] **Step 7.5: PublicAPI entries**

  ```text
  RevCore.AddressableDownloader
  static RevCore.AddressableDownloader.GetDownloadSizeAsync(object key, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<long>
  static RevCore.AddressableDownloader.DownloadDependenciesAsync(object key, System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask
  static RevCore.AddressableDownloader.ClearDependencyCacheAsync(object key, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<bool>
  ```

- [ ] **Step 7.6: CHANGELOG**

  ```markdown
  - `AddressableDownloader` with `GetDownloadSizeAsync`, `DownloadDependenciesAsync`, `ClearDependencyCacheAsync`.
  ```

- [ ] **Step 7.7: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Helpers/AddressableDownloader.cs Assets/RevCore/Addressables/Tests/Editor/AddressableDownloaderTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): AddressableDownloader (size/download/clear-cache)"
  ```

---

## Task 8: AddressableCatalog

Wraps catalog update flow.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableCatalog.cs`
- Create: `Assets/RevCore/Addressables/Tests/Editor/AddressableCatalogTests.cs`

- [ ] **Step 8.1: Failing test**

  ```csharp
  using Cysharp.Threading.Tasks;
  using NUnit.Framework;
  using RevCore;

  namespace RevCore.Tests
  {
  	public class AddressableCatalogTests
  	{
  		[Test]
  		public void check_for_catalog_updates_async_completes_without_throwing()
  		{
  			Assert.DoesNotThrowAsync(async () =>
  			{
  				var updates = await AddressableCatalog.CheckForCatalogUpdatesAsync();
  				Assert.IsNotNull(updates);
  			});
  		}
  	}
  }
  ```

- [ ] **Step 8.2: Run, verify class missing**

- [ ] **Step 8.3: Implement `AddressableCatalog`**

  ```csharp
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using UnityEngine.AddressableAssets;
  using UnityEngine.AddressableAssets.ResourceLocators;
  using UnityEngine.ResourceManagement.AsyncOperations;

  namespace RevCore
  {
  	/// <summary>Helpers around Addressables catalog discovery and update.</summary>
  	public static class AddressableCatalog
  	{
  		/// <summary>Returns the list of catalog IDs that have pending updates. Empty list means none.</summary>
  		public static async UniTask<List<string>> CheckForCatalogUpdatesAsync(bool autoReleaseHandle = true, CancellationToken ct = default)
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.CheckForCatalogUpdates(autoReleaseHandle);
  			try { return await handle.ToUniTask(cancellationToken: ct); }
  			catch (OperationCanceledException) { if (handle.IsValid() && !autoReleaseHandle) handle.Completed += op => Addressables.Release(op); throw; }
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				if (handle.IsValid() && !autoReleaseHandle) Addressables.Release(handle);
  				throw new AddressableLoadException("CheckForCatalogUpdates", status, ex);
  			}
  		}

  		/// <summary>Applies catalog updates for the supplied catalog IDs (or all if <paramref name="catalogIds"/> is null).</summary>
  		public static async UniTask<List<IResourceLocator>> UpdateCatalogsAsync(IEnumerable<string> catalogIds = null, bool autoReleaseHandle = true, CancellationToken ct = default)
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.UpdateCatalogs(catalogIds, autoReleaseHandle);
  			try { return await handle.ToUniTask(cancellationToken: ct); }
  			catch (OperationCanceledException) { if (handle.IsValid() && !autoReleaseHandle) handle.Completed += op => Addressables.Release(op); throw; }
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				if (handle.IsValid() && !autoReleaseHandle) Addressables.Release(handle);
  				throw new AddressableLoadException("UpdateCatalogs", status, ex);
  			}
  		}
  	}
  }
  ```

- [ ] **Step 8.4: Run, verify pass**

- [ ] **Step 8.5: PublicAPI**

  ```text
  RevCore.AddressableCatalog
  static RevCore.AddressableCatalog.CheckForCatalogUpdatesAsync(bool autoReleaseHandle = true, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<System.Collections.Generic.List<string>>
  static RevCore.AddressableCatalog.UpdateCatalogsAsync(System.Collections.Generic.IEnumerable<string> catalogIds = null, bool autoReleaseHandle = true, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<System.Collections.Generic.List<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator>>
  ```

- [ ] **Step 8.6: CHANGELOG + Commit**

  ```markdown
  - `AddressableCatalog.CheckForCatalogUpdatesAsync`, `UpdateCatalogsAsync`.
  ```

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Helpers/AddressableCatalog.cs Assets/RevCore/Addressables/Tests/Editor/AddressableCatalogTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): AddressableCatalog (check + update)"
  ```

---

## Task 9: AddressableScene (PlayMode)

Scene load/unload helpers.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Helpers/AddressableScene.cs`
- Create: `Assets/RevCore/Addressables/Tests/Runtime/AddressableSceneTests.cs`

- [ ] **Step 9.1: Failing PlayMode test**

  ```csharp
  using System.Collections;
  using Cysharp.Threading.Tasks;
  using NUnit.Framework;
  using RevCore;
  using UnityEngine.TestTools;

  namespace RevCore.Tests
  {
  	public class AddressableSceneTests
  	{
  		[UnityTest]
  		public IEnumerator load_scene_async_with_unknown_key_throws() => UniTask.ToCoroutine(async () =>
  		{
  			try { await AddressableScene.LoadSceneAsync("does-not-exist"); Assert.Fail(); }
  			catch (AddressableLoadException) { Assert.Pass(); }
  		});
  	}
  }
  ```

- [ ] **Step 9.2: Implement `AddressableScene`**

  ```csharp
  using System;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using UnityEngine.AddressableAssets;
  using UnityEngine.ResourceManagement.AsyncOperations;
  using UnityEngine.ResourceManagement.ResourceProviders;
  using UnityEngine.SceneManagement;

  namespace RevCore
  {
  	/// <summary>Helpers around Addressables scene load/unload.</summary>
  	public static class AddressableScene
  	{
  		/// <summary>Loads an Addressables scene. Returns the resulting <see cref="SceneInstance"/>.</summary>
  		public static async UniTask<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100, IProgress<float> progress = null, CancellationToken ct = default)
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.LoadSceneAsync(key, loadMode, activateOnLoad, priority);
  			try { return await handle.ToUniTask(progress, cancellationToken: ct); }
  			catch (OperationCanceledException) { if (handle.IsValid()) handle.Completed += op => Addressables.Release(op); throw; }
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				if (handle.IsValid()) Addressables.Release(handle);
  				throw new AddressableLoadException(key, status, ex);
  			}
  		}

  		/// <summary>Unloads a previously loaded Addressables scene.</summary>
  		public static async UniTask<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true, CancellationToken ct = default)
  		{
  			ct.ThrowIfCancellationRequested();
  			var handle = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
  			try { return await handle.ToUniTask(cancellationToken: ct); }
  			catch (Exception ex)
  			{
  				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
  				throw new AddressableLoadException(scene.Scene.name, status, ex);
  			}
  		}
  	}
  }
  ```

- [ ] **Step 9.3: PublicAPI**

  ```text
  RevCore.AddressableScene
  static RevCore.AddressableScene.LoadSceneAsync(object key, UnityEngine.SceneManagement.LoadSceneMode loadMode = UnityEngine.SceneManagement.LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100, System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
  static RevCore.AddressableScene.UnloadSceneAsync(UnityEngine.ResourceManagement.ResourceProviders.SceneInstance scene, bool autoReleaseHandle = true, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
  ```

- [ ] **Step 9.4: CHANGELOG + Commit**

  ```markdown
  - `AddressableScene.LoadSceneAsync`, `UnloadSceneAsync`.
  ```

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Helpers/AddressableScene.cs Assets/RevCore/Addressables/Tests/Runtime/AddressableSceneTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): AddressableScene (load/unload)"
  ```

---

## Task 10: AssetRef&lt;T&gt; wrapper

Inspector-serializable wrapper over `AssetReferenceT<T>` with cached `Asset`, `IsLoading`, `IsLoaded`, `LoadAsync`, `Release`.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Wrappers/AssetRef.cs`
- Create: `Assets/RevCore/Addressables/Tests/Editor/AssetRefTests.cs`

- [ ] **Step 10.1: Failing tests**

  ```csharp
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using NUnit.Framework;
  using RevCore;
  using UnityEngine;

  namespace RevCore.Tests
  {
  	public class AssetRefTests
  	{
  		[Test]
  		public void newly_constructed_ref_is_not_loaded_and_not_loading()
  		{
  			var r = new AssetRef<Texture2D>();
  			Assert.IsFalse(r.IsLoaded);
  			Assert.IsFalse(r.IsLoading);
  			Assert.IsNull(r.Asset);
  		}

  		[Test]
  		public void load_async_with_invalid_reference_throws_AddressableLoadException()
  		{
  			var r = new AssetRef<Texture2D>();
  			Assert.ThrowsAsync<AddressableLoadException>(async () => await r.LoadAsync());
  		}
  	}
  }
  ```

- [ ] **Step 10.2: Implement `AssetRef<T>`**

  ```csharp
  using System;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using UnityEngine;
  using UnityEngine.AddressableAssets;
  using UnityEngine.ResourceManagement.AsyncOperations;
  using Object = UnityEngine.Object;

  namespace RevCore
  {
  	/// <summary>
  	/// Inspector-serialisable wrapper over <see cref="AssetReferenceT{TObject}"/>. Caches the loaded asset and
  	/// the in-flight handle so repeated calls to <see cref="LoadAsync"/> are idempotent.
  	/// </summary>
  	/// <remarks>
  	/// Unity's generic serialisation only inspects concrete subclasses. To expose a field in the inspector,
  	/// declare a concrete subclass, e.g. <c>[Serializable] public class AudioClipRef : AssetRef&lt;AudioClip&gt; {}</c>.
  	/// </remarks>
  	[Serializable]
  	public class AssetRef<T> where T : Object
  	{
  		[SerializeField] private AssetReferenceT<T> m_reference;

  		private AsyncOperationHandle<T> m_handle;
  		private T m_asset;

  		/// <summary>The underlying <see cref="AssetReferenceT{TObject}"/>.</summary>
  		public AssetReferenceT<T> Reference => m_reference;

  		/// <summary>The cached, loaded asset (null until <see cref="LoadAsync"/> completes).</summary>
  		public T Asset => m_asset;

  		/// <summary>True while a load operation is in flight.</summary>
  		public bool IsLoading => m_handle.IsValid() && !m_handle.IsDone;

  		/// <summary>True after a successful load.</summary>
  		public bool IsLoaded => m_asset != null;

  		/// <summary>Loads the referenced asset (idempotent on success).</summary>
  		public async UniTask<T> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default)
  		{
  			if (m_asset != null) return m_asset;
  			if (m_reference == null) throw new AddressableLoadException("<null AssetReference>", AsyncOperationStatus.Failed, new InvalidOperationException("AssetRef.Reference is null"));

  			if (!m_handle.IsValid()) m_handle = m_reference.LoadAssetAsync<T>();
  			try
  			{
  				m_asset = await m_handle.ToUniTask(progress, cancellationToken: ct);
  				return m_asset;
  			}
  			catch (OperationCanceledException)
  			{
  				if (m_handle.IsValid()) m_handle.Completed += op => Addressables.Release(op);
  				m_handle = default;
  				throw;
  			}
  			catch (Exception ex)
  			{
  				var status = m_handle.IsValid() ? m_handle.Status : AsyncOperationStatus.Failed;
  				if (m_handle.IsValid()) Addressables.Release(m_handle);
  				m_handle = default;
  				throw new AddressableLoadException(m_reference.RuntimeKey, status, ex);
  			}
  		}

  		/// <summary>Releases the cached handle and clears <see cref="Asset"/>.</summary>
  		public void Release()
  		{
  			if (m_handle.IsValid()) Addressables.Release(m_handle);
  			m_handle = default;
  			m_asset = null;
  		}
  	}
  }
  ```

- [ ] **Step 10.3: Run, verify pass**

- [ ] **Step 10.4: PublicAPI**

  ```text
  RevCore.AssetRef<T>
  RevCore.AssetRef<T>.AssetRef() -> void
  RevCore.AssetRef<T>.Reference.get -> UnityEngine.AddressableAssets.AssetReferenceT<T>
  RevCore.AssetRef<T>.Asset.get -> T
  RevCore.AssetRef<T>.IsLoading.get -> bool
  RevCore.AssetRef<T>.IsLoaded.get -> bool
  RevCore.AssetRef<T>.LoadAsync(System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<T>
  RevCore.AssetRef<T>.Release() -> void
  ```

- [ ] **Step 10.5: CHANGELOG + Commit**

  ```markdown
  - `AssetRef<T>` serializable wrapper with cached `Asset`, `IsLoading`, `IsLoaded`, `LoadAsync`, `Release`.
  ```

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Wrappers/AssetRef.cs Assets/RevCore/Addressables/Tests/Editor/AssetRefTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): AssetRef<T> serializable wrapper"
  ```

---

## Task 11: KeyedAssetRef&lt;TKey, T&gt;

Single generic that replaces RCore's three keyed variants.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Wrappers/KeyedAssetRef.cs`
- Create: `Assets/RevCore/Addressables/Tests/Editor/KeyedAssetRefTests.cs`

- [ ] **Step 11.1: Failing test**

  ```csharp
  using NUnit.Framework;
  using RevCore;
  using UnityEngine;

  namespace RevCore.Tests
  {
  	public class KeyedAssetRefTests
  	{
  		private enum Hero { Warrior, Mage }

  		[Test]
  		public void key_property_round_trips()
  		{
  			var r = new KeyedAssetRef<Hero, GameObject> { Key = Hero.Mage };
  			Assert.AreEqual(Hero.Mage, r.Key);
  		}

  		[Test]
  		public void inherits_AssetRef_lifecycle_flags()
  		{
  			var r = new KeyedAssetRef<int, Texture2D>();
  			Assert.IsFalse(r.IsLoaded);
  		}
  	}
  }
  ```

- [ ] **Step 11.2: Implement `KeyedAssetRef<TKey, T>`**

  ```csharp
  using System;
  using UnityEngine;
  using Object = UnityEngine.Object;

  namespace RevCore
  {
  	/// <summary>
  	/// An <see cref="AssetRef{T}"/> tagged with a user-defined key. Replaces RCore's
  	/// <c>AssetBundleWithEnumKey</c>, <c>AssetBundleWith2EnumKeys</c>, <c>AssetBundleWithIntKey</c>
  	/// with a single generic.
  	/// </summary>
  	[Serializable]
  	public class KeyedAssetRef<TKey, T> : AssetRef<T> where T : Object
  	{
  		[SerializeField] private TKey m_key;

  		/// <summary>The user-defined key associated with this asset.</summary>
  		public TKey Key { get => m_key; set => m_key = value; }
  	}
  }
  ```

- [ ] **Step 11.3: PublicAPI**

  ```text
  RevCore.KeyedAssetRef<TKey, T>
  RevCore.KeyedAssetRef<TKey, T>.KeyedAssetRef() -> void
  RevCore.KeyedAssetRef<TKey, T>.Key.get -> TKey
  RevCore.KeyedAssetRef<TKey, T>.Key.set -> void
  ```

- [ ] **Step 11.4: CHANGELOG + Commit**

  ```markdown
  - `KeyedAssetRef<TKey, T>` — generic keyed variant of `AssetRef<T>`.
  ```

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Wrappers/KeyedAssetRef.cs Assets/RevCore/Addressables/Tests/Editor/KeyedAssetRefTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): KeyedAssetRef<TKey, T>"
  ```

---

## Task 12: ComponentRef&lt;TComponent&gt;

Replaces RCore's `ComponentRef` — properties not public fields, single `Release()` method, no shortcut subclasses.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Wrappers/ComponentRef.cs`
- Create: `Assets/RevCore/Addressables/Tests/Editor/ComponentRefTests.cs`

- [ ] **Step 12.1: Failing tests**

  ```csharp
  using NUnit.Framework;
  using RevCore;
  using UnityEngine;

  namespace RevCore.Tests
  {
  	public class ComponentRefTests
  	{
  		[Test]
  		public void HasType_returns_true_for_assignable_subclass()
  		{
  			var r = new ComponentRef<Renderer>("00000000000000000000000000000000");
  			Assert.IsTrue(r.HasType(typeof(MeshRenderer)));
  			Assert.IsFalse(r.HasType(typeof(Collider)));
  		}

  		[Test]
  		public void ValidateAsset_returns_false_for_GameObject_missing_component()
  		{
  			var r = new ComponentRef<Rigidbody>("00000000000000000000000000000000");
  			var go = new GameObject();
  			try { Assert.IsFalse(r.ValidateAsset(go)); }
  			finally { Object.DestroyImmediate(go); }
  		}
  	}
  }
  ```

- [ ] **Step 12.2: Implement `ComponentRef<TComponent>`**

  ```csharp
  using System;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using UnityEngine;
  using UnityEngine.AddressableAssets;
  using UnityEngine.ResourceManagement.AsyncOperations;
  #if UNITY_EDITOR
  using UnityEditor;
  #endif
  using Object = UnityEngine.Object;

  namespace RevCore
  {
  	/// <summary>
  	/// Serialisable Addressables reference that resolves to a specific <see cref="Component"/> type
  	/// on a prefab GameObject. Loads return the component itself, not the GameObject.
  	/// </summary>
  	[Serializable]
  	public class ComponentRef<TComponent> : AssetReference where TComponent : Component
  	{
  		[SerializeField] private string m_type;

  		private AsyncOperationHandle<GameObject> m_handle;
  		private TComponent m_asset;

  		/// <summary>Fully-qualified type name cached at validation time. Editor-only diagnostic.</summary>
  		public string TypeName => m_type;

  		/// <summary>The loaded component (null until <see cref="LoadAsync"/> completes).</summary>
  		public TComponent Asset => m_asset;

  		/// <summary>True while a load is in flight.</summary>
  		public bool IsLoading => m_handle.IsValid() && !m_handle.IsDone;

  		/// <summary>True after a successful load.</summary>
  		public bool IsLoaded => m_asset != null;

  		/// <summary>Initialises a new <see cref="ComponentRef{TComponent}"/>.</summary>
  		public ComponentRef(string guid) : base(guid) { }

  		/// <inheritdoc/>
  		public override bool ValidateAsset(Object obj)
  		{
  			if (obj is GameObject go)
  			{
  				var c = go.GetComponent<TComponent>();
  				if (c != null) { m_type = c.GetType().FullName; return true; }
  			}
  			return false;
  		}

  		/// <inheritdoc/>
  		public override bool ValidateAsset(string path)
  		{
  #if UNITY_EDITOR
  			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
  			if (go == null) return false;
  			var c = go.GetComponent<TComponent>();
  			if (c != null) { m_type = c.GetType().FullName; return true; }
  			return false;
  #else
  			return false;
  #endif
  		}

  		/// <summary>Loads the prefab and returns its <typeparamref name="TComponent"/> component.</summary>
  		public async UniTask<TComponent> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default)
  		{
  			if (m_asset != null) return m_asset;
  			if (!m_handle.IsValid()) m_handle = LoadAssetAsync<GameObject>();
  			try
  			{
  				var go = await m_handle.ToUniTask(progress, cancellationToken: ct);
  				m_asset = go != null ? go.GetComponent<TComponent>() : null;
  				if (m_asset == null) throw new InvalidOperationException($"Loaded prefab has no {typeof(TComponent).Name}");
  				return m_asset;
  			}
  			catch (OperationCanceledException)
  			{
  				if (m_handle.IsValid()) m_handle.Completed += op => Addressables.Release(op);
  				m_handle = default;
  				throw;
  			}
  			catch (Exception ex)
  			{
  				var status = m_handle.IsValid() ? m_handle.Status : AsyncOperationStatus.Failed;
  				if (m_handle.IsValid()) Addressables.Release(m_handle);
  				m_handle = default;
  				throw new AddressableLoadException(RuntimeKey, status, ex);
  			}
  		}

  		/// <summary>Releases the cached handle and clears <see cref="Asset"/>.</summary>
  		public void Release()
  		{
  			if (m_handle.IsValid()) Addressables.Release(m_handle);
  			m_handle = default;
  			m_asset = null;
  		}

  		/// <summary>True if <paramref name="checkType"/> is <typeparamref name="TComponent"/> or a subclass.</summary>
  		public bool HasType(Type checkType) => typeof(TComponent).IsAssignableFrom(checkType);
  	}
  }
  ```

- [ ] **Step 12.3: PublicAPI**

  ```text
  RevCore.ComponentRef<TComponent>
  RevCore.ComponentRef<TComponent>.ComponentRef(string guid) -> void
  RevCore.ComponentRef<TComponent>.TypeName.get -> string
  RevCore.ComponentRef<TComponent>.Asset.get -> TComponent
  RevCore.ComponentRef<TComponent>.IsLoading.get -> bool
  RevCore.ComponentRef<TComponent>.IsLoaded.get -> bool
  override RevCore.ComponentRef<TComponent>.ValidateAsset(UnityEngine.Object obj) -> bool
  override RevCore.ComponentRef<TComponent>.ValidateAsset(string path) -> bool
  RevCore.ComponentRef<TComponent>.LoadAsync(System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<TComponent>
  RevCore.ComponentRef<TComponent>.Release() -> void
  RevCore.ComponentRef<TComponent>.HasType(System.Type checkType) -> bool
  ```

- [ ] **Step 12.4: CHANGELOG + Commit**

  ```markdown
  - `ComponentRef<TComponent>` serialisable reference resolving to a `Component` on a prefab.
  ```

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Wrappers/ComponentRef.cs Assets/RevCore/Addressables/Tests/Editor/ComponentRefTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): ComponentRef<TComponent>"
  ```

---

## Task 13: PrefabRef&lt;TComponent&gt; (PlayMode)

Wraps `ComponentRef<T>` + a serialised parent transform; provides `InstantiateAsync` and `Release` that destroys the instance.

**Files:**
- Create: `Assets/RevCore/Addressables/Runtime/Wrappers/PrefabRef.cs`
- Create: `Assets/RevCore/Addressables/Tests/Runtime/PrefabRefTests.cs`

- [ ] **Step 13.1: Failing PlayMode test**

  ```csharp
  using System.Collections;
  using Cysharp.Threading.Tasks;
  using NUnit.Framework;
  using RevCore;
  using UnityEngine;
  using UnityEngine.TestTools;

  namespace RevCore.Tests
  {
  	public class PrefabRefTests
  	{
  		[UnityTest]
  		public IEnumerator instantiate_async_with_unknown_key_throws() => UniTask.ToCoroutine(async () =>
  		{
  			var r = new PrefabRef<Transform>("00000000000000000000000000000000");
  			try { await r.InstantiateAsync(null); Assert.Fail(); }
  			catch (AddressableLoadException) { Assert.Pass(); }
  		});
  	}
  }
  ```

- [ ] **Step 13.2: Implement `PrefabRef<TComponent>`**

  ```csharp
  using System;
  using System.Threading;
  using Cysharp.Threading.Tasks;
  using UnityEngine;
  using Object = UnityEngine.Object;

  namespace RevCore
  {
  	/// <summary>
  	/// Inspector-serialisable bundle of a <see cref="ComponentRef{TComponent}"/> plus a default parent transform.
  	/// Provides <see cref="InstantiateAsync"/> for one-shot prefab instancing with cached load.
  	/// </summary>
  	[Serializable]
  	public class PrefabRef<TComponent> where TComponent : Component
  	{
  		[SerializeField] private ComponentRef<TComponent> m_reference;
  		[SerializeField] private Transform m_parent;

  		private TComponent m_instance;

  		/// <summary>The underlying <see cref="ComponentRef{TComponent}"/>.</summary>
  		public ComponentRef<TComponent> Reference => m_reference;

  		/// <summary>The default parent transform used by <see cref="InstantiateAsync"/> when none is supplied.</summary>
  		public Transform DefaultParent { get => m_parent; set => m_parent = value; }

  		/// <summary>The loaded prefab asset (null until <see cref="LoadAsync"/> or <see cref="InstantiateAsync"/> completes).</summary>
  		public TComponent Asset => m_reference != null ? m_reference.Asset : null;

  		/// <summary>The instantiated component, if any.</summary>
  		public TComponent Instance => m_instance;

  		/// <summary>True after <see cref="InstantiateAsync"/> has produced an instance.</summary>
  		public bool IsInstantiated => m_instance != null;

  		/// <summary>Initialises a new <see cref="PrefabRef{TComponent}"/>.</summary>
  		public PrefabRef() { }

  		/// <summary>Initialises a new <see cref="PrefabRef{TComponent}"/> with the supplied GUID.</summary>
  		public PrefabRef(string guid) { m_reference = new ComponentRef<TComponent>(guid); }

  		/// <summary>Loads the prefab without instantiating.</summary>
  		public UniTask<TComponent> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default)
  		{
  			if (m_reference == null) throw new AddressableLoadException("<null ComponentRef>", UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed, new InvalidOperationException("PrefabRef.Reference is null"));
  			return m_reference.LoadAsync(progress, ct);
  		}

  		/// <summary>Instantiates the prefab under <paramref name="parent"/> (or <see cref="DefaultParent"/>).</summary>
  		public async UniTask<TComponent> InstantiateAsync(Transform parent = null, bool defaultActive = true, CancellationToken ct = default)
  		{
  			if (m_instance != null) return m_instance;
  			var asset = await LoadAsync(ct: ct);
  			var p = parent != null ? parent : m_parent;
  			m_instance = Object.Instantiate(asset, p);
  			m_instance.name = asset.name;
  			m_instance.gameObject.SetActive(defaultActive);
  			return m_instance;
  		}

  		/// <summary>Destroys the instance (if any) and releases the cached asset handle.</summary>
  		public void Release()
  		{
  			if (m_instance != null) Object.Destroy(m_instance.gameObject);
  			m_instance = null;
  			m_reference?.Release();
  		}
  	}
  }
  ```

- [ ] **Step 13.3: PublicAPI**

  ```text
  RevCore.PrefabRef<TComponent>
  RevCore.PrefabRef<TComponent>.PrefabRef() -> void
  RevCore.PrefabRef<TComponent>.PrefabRef(string guid) -> void
  RevCore.PrefabRef<TComponent>.Reference.get -> RevCore.ComponentRef<TComponent>
  RevCore.PrefabRef<TComponent>.DefaultParent.get -> UnityEngine.Transform
  RevCore.PrefabRef<TComponent>.DefaultParent.set -> void
  RevCore.PrefabRef<TComponent>.Asset.get -> TComponent
  RevCore.PrefabRef<TComponent>.Instance.get -> TComponent
  RevCore.PrefabRef<TComponent>.IsInstantiated.get -> bool
  RevCore.PrefabRef<TComponent>.LoadAsync(System.IProgress<float> progress = null, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<TComponent>
  RevCore.PrefabRef<TComponent>.InstantiateAsync(UnityEngine.Transform parent = null, bool defaultActive = true, System.Threading.CancellationToken ct = default) -> Cysharp.Threading.Tasks.UniTask<TComponent>
  RevCore.PrefabRef<TComponent>.Release() -> void
  ```

- [ ] **Step 13.4: CHANGELOG + Commit**

  ```markdown
  - `PrefabRef<TComponent>` — `ComponentRef` + parent transform + cached instance.
  ```

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/Wrappers/PrefabRef.cs Assets/RevCore/Addressables/Tests/Runtime/PrefabRefTests.cs Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt Assets/RevCore/Addressables/CHANGELOG.md
  git commit -m "feat(addressables): PrefabRef<TComponent>"
  ```

---

## Task 14: README full documentation

Replace the placeholder README with the production version. Mirrors the structure of `Assets/RevCore/Timer/README.md`.

**Files:**
- Modify: `Assets/RevCore/Addressables/README.md`

- [ ] **Step 14.1: Inspect the Timer README as the template**

  Run:
  ```powershell
  Get-Content Assets/RevCore/Timer/README.md -TotalCount 80
  ```
  Note the section order: title, badges/install, quickstart, features, FAQ/caveats, migration.

- [ ] **Step 14.2: Write the production README**

  Overwrite `Assets/RevCore/Addressables/README.md`:

  ````markdown
  # RevCore.Addressables

  UniTask-first wrapper over Unity Addressables for the RevCore framework. Zero dependencies on other RevCore packages.

  ## Install

  Local UPM path (recommended during development):

  ```text
  "com.rabear.revcore.addressables": "file:../Assets/RevCore/Addressables"
  ```

  Or via UPM git URL once published:

  ```text
  "com.rabear.revcore.addressables": "https://github.com/hnb-rabear/RCore.git?path=Assets/RevCore/Addressables#v1.1.0"
  ```

  ## 60-second quickstart

  ```csharp
  using Cysharp.Threading.Tasks;
  using RevCore;
  using UnityEngine;

  public class TitleMusic : MonoBehaviour
  {
      public AssetRef<AudioClip> mainTheme;

      private async void Start()
      {
          var clip = await mainTheme.LoadAsync();
          GetComponent<AudioSource>().PlayOneShot(clip);
      }

      private void OnDestroy() => mainTheme.Release();
  }
  ```

  Or use static helpers directly:

  ```csharp
  var clip = await AddressableLoader.LoadAssetAsync<AudioClip>("MainTheme");
  ```

  ## Public surface

  | Symbol | Purpose |
  | --- | --- |
  | `AddressableLoader` | static load / instantiate / locations / release |
  | `AddressableDownloader` | size query / download / cache clear |
  | `AddressableCatalog` | catalog check + update |
  | `AddressableScene` | scene load / unload |
  | `AssetRef<T>` | serialisable wrapper, cached load |
  | `KeyedAssetRef<TKey, T>` | `AssetRef` + user-defined key |
  | `ComponentRef<TComponent>` | reference to a Component on a prefab |
  | `PrefabRef<TComponent>` | `ComponentRef` + parent + cached instance |
  | `AddressableLoadException` | thrown on terminal failures |

  ## Unity serialisation caveat

  Unity only inspects concrete generic subclasses. To expose a wrapper field in the inspector, declare a concrete subclass:

  ```csharp
  [System.Serializable] public class AudioClipRef : AssetRef<AudioClip> { }
  [System.Serializable] public class HeroPrefab : PrefabRef<Hero> { }
  ```

  Alternatively use the wrapper directly inside a class that is itself `[Serializable]` and Unity will inspect the field through composition.

  ## Lifetime contract

  All async methods accept `CancellationToken ct = default` and `IProgress<float> progress = null` where applicable. On terminal failure they throw `AddressableLoadException` carrying the originating key + `AsyncOperationStatus`. Cancellation does **not** eagerly release in-flight handles — helpers attach a `Completed += op => Addressables.Release(op)` continuation so resources unwind once the underlying operation finishes.

  Need handle ownership? Use `LoadAssetWithHandleAsync<T>` — the caller is then responsible for `Addressables.Release(handle)`.

  ## Migration from RCore

  See [`docs/migration/rcore-to-revcore-api-map.csv`](../../../docs/migration/rcore-to-revcore-api-map.csv) for the row-by-row map from `RCore.AddressableUtil` / `AssetBundleRef<T>` / `ComponentRef<T>` to the RevCore equivalents.

  ## License

  Same license as the rest of the RevCore framework. See repository root `LICENSE.md`.
  ````

- [ ] **Step 14.3: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/README.md
  git commit -m "docs(addressables): production README with quickstart, surface table, lifetime contract"
  ```

---

## Task 15: Migration map rows

Append the RCore → RevCore row map to the project's migration CSV.

**Files:**
- Modify: `docs/migration/rcore-to-revcore-api-map.csv`

- [ ] **Step 15.1: Inspect the current header + tail**

  Run:
  ```powershell
  Get-Content docs/migration/rcore-to-revcore-api-map.csv -TotalCount 3
  Get-Content docs/migration/rcore-to-revcore-api-map.csv -Tail 3
  ```
  Note the column order so the new rows match.

- [ ] **Step 15.2: Append rows**

  Append at end of file using the actual repository schema `rcore_module,rcore_type,kind,status,revcore_module,revcore_type,note`:

  ```text
  Addressable,AddressableUtil.LoadAssetAsync<T>(string),method,REPLACED,Addressables,AddressableLoader.LoadAssetAsync<T>(string),Now throws AddressableLoadException on failure
  Addressable,AddressableUtil.LoadAssetAsync<T>(AssetReference),method,REPLACED,Addressables,AddressableLoader.LoadAssetAsync<T>(AssetReference),Same semantics
  Addressable,AddressableUtil.LoadAssetAsync<T>(AssetReferenceT<T>),method,REPLACED,Addressables,AddressableLoader.LoadAssetAsync<T>(AssetReferenceT<T>),Strongly-typed overload
  Addressable,AddressableUtil.LoadAssetsAsync<T>,method,REPLACED,Addressables,AddressableLoader.LoadAssetsAsync<T>,Batch surface unchanged
  Addressable,AddressableUtil.InstantiateAsync,method,REPLACED,Addressables,AddressableLoader.InstantiateAsync,Returns UniTask<GameObject>
  Addressable,AddressableUtil.LoadSceneAsync,method,REPLACED,Addressables,AddressableScene.LoadSceneAsync,Moved to dedicated helper
  Addressable,AddressableUtil.UnloadSceneAsync,method,REPLACED,Addressables,AddressableScene.UnloadSceneAsync,Moved to dedicated helper
  Addressable,AddressableUtil.GetDownloadSizeAsync,method,REPLACED,Addressables,AddressableDownloader.GetDownloadSizeAsync,Returns long bytes
  Addressable,AddressableUtil.DownloadDependenciesAsync,method,REPLACED,Addressables,AddressableDownloader.DownloadDependenciesAsync,Throws on failure
  Addressable,AddressableUtil.ClearDependencyCacheAsync,method,REPLACED,Addressables,AddressableDownloader.ClearDependencyCacheAsync,Returns UniTask<bool>
  Addressable,AddressableUtil.CheckForCatalogUpdatesAsync,method,REPLACED,Addressables,AddressableCatalog.CheckForCatalogUpdatesAsync,Returns List<string>
  Addressable,AddressableUtil.UpdateCatalogsAsync,method,REPLACED,Addressables,AddressableCatalog.UpdateCatalogsAsync,Returns List<IResourceLocator>
  Addressable,AssetBundleRef<M>,class,REPLACED,Addressables,AssetRef<T>,Single generic
  Addressable,AssetBundleWithEnumKey<T,M>,class,REPLACED,Addressables,KeyedAssetRef<TKey,T>,Single generic replaces three RCore variants
  Addressable,AssetBundleWith2EnumKeys<T1,T2,M>,class,DROPPED,—,—,Use struct/tuple key with KeyedAssetRef
  Addressable,AssetBundleWithIntKey<M>,class,REPLACED,Addressables,KeyedAssetRef<int,T>,Single generic
  Addressable,AssetBundleWrap<T>,class,REPLACED,Addressables,PrefabRef<T>,Renamed; same shape
  Addressable,ComponentRef<TComponent>,class,REPLACED,Addressables,ComponentRef<TComponent>,Public fields → properties; ReleaseAsset → Release
  Addressable,ComponentRef_SpriteRenderer,class,DROPPED,Addressables,ComponentRef<SpriteRenderer>,Specialised subclass no longer needed
  Addressable,AssetRef_SpriteAtlas,class,DROPPED,—,—,Use AssetReferenceT<SpriteAtlas> in a serializable subclass
  Addressable,AssetRef_FontAsset,class,DROPPED,—,—,Use AssetReferenceT<TMP_FontAsset> in a serializable subclass
  ```

- [ ] **Step 15.3: Commit**

  ```powershell
  git add docs/migration/rcore-to-revcore-api-map.csv
  git commit -m "docs(migration): add RCore→RevCore Addressables row map"
  ```

---

## Task 16: PublicAPI seal + xmldoc baseline update

Promote `Unshipped` → `Shipped`, regenerate inventory, update baseline.

**Files:**
- Modify: `Assets/RevCore/Addressables/Runtime/PublicAPI.Shipped.txt`
- Modify: `Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt`
- Verify: `scripts/xmldoc-baseline.json`
- Modify: `docs/api-inventory.csv`

- [ ] **Step 16.1: Run xmldoc coverage gate**

  ```powershell
  python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
  ```
  Expected: passes with the new public members all documented (every public symbol has `/// <summary>`). If any undocumented are reported, add `<summary>` blocks until 100%.

- [ ] **Step 16.2: Verify baseline requires no edit**

  `scripts/xmldoc-baseline.json` maps file paths to allowed-undocumented counts (schema: `dict[str, int]`). Current file is `{}` — meaning zero undocumented allowed everywhere. New Addressables files must achieve 100% coverage; no baseline edit needed. If the gate fails, add missing `/// <summary>` blocks until it passes.

- [ ] **Step 16.3: Seal PublicAPI surface**

  Run:
  ```powershell
  python scripts/seal-public-api.py --root Assets/RevCore
  ```
  Expected: every line in `Runtime/PublicAPI.Unshipped.txt` (except the `#nullable enable` header) is moved into `Runtime/PublicAPI.Shipped.txt`. `Unshipped.txt` is left with just the `#nullable enable` header.

- [ ] **Step 16.4: Refresh the API inventory CSV**

  ```powershell
  python scripts/extract-api-surface.py --out docs/api-inventory.csv
  ```

- [ ] **Step 16.5: Commit**

  ```powershell
  git add Assets/RevCore/Addressables/Runtime/PublicAPI.Shipped.txt Assets/RevCore/Addressables/Runtime/PublicAPI.Unshipped.txt docs/api-inventory.csv
  git commit -m "chore(addressables): seal PublicAPI surface for v1.0.0 + refresh inventory"
  ```

---

## Task 17: Final CHANGELOG entry + push + PR prep

Roll up `[Unreleased]` into a `[1.0.0]` heading, push the branch, document the PR.

**Files:**
- Modify: `Assets/RevCore/Addressables/CHANGELOG.md`
- Modify: `CHANGELOG.md` (repo root)

- [ ] **Step 17.1: Promote `[Unreleased]` → `[1.0.0] - 2026-05-19` in module CHANGELOG**

  Replace the `## [Unreleased]` heading with:

  ```markdown
  ## [1.0.0] - 2026-05-19
  ```

  Then add a fresh empty `## [Unreleased]` section above it so future commits have a target.

- [ ] **Step 17.2: Append to root `CHANGELOG.md` `[Unreleased]` section**

  Under `### Added`, append:

  ```markdown
  - **RevCore.Addressables v1.0.0** — new standalone package: UniTask-first helpers (`AddressableLoader`, `AddressableDownloader`, `AddressableCatalog`, `AddressableScene`), serialisable wrappers (`AssetRef<T>`, `KeyedAssetRef<TKey,T>`, `ComponentRef<TComponent>`, `PrefabRef<TComponent>`), and `AddressableLoadException`. Replaces RCore's `AddressableUtil` + asset-ref family. See module README and `docs/migration/rcore-to-revcore-api-map.csv`.
  ```

- [ ] **Step 17.3: Re-run all gates one final time**

  ```powershell
  python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
  Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml -quit
  Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults-PlayMode.xml -quit
  ```
  Expected: xmldoc 100%, all EditMode + PlayMode tests green.

- [ ] **Step 17.4: Commit + push**

  ```powershell
  git add Assets/RevCore/Addressables/CHANGELOG.md CHANGELOG.md
  git commit -m "release(addressables): v1.0.0"
  git push -u origin feat/addressables-v1.0
  ```

- [ ] **Step 17.5: Open the PR**

  ```powershell
  $body = @'
  ## Summary

  - New standalone package `com.rabear.revcore.addressables` v1.0.0.
  - Wraps Unity Addressables behind a UniTask-first API; throws `AddressableLoadException` on terminal failure; honours `CancellationToken` + `IProgress<float>` everywhere.
  - Adds serialisable wrappers (`AssetRef<T>`, `KeyedAssetRef<TKey,T>`, `ComponentRef<TComponent>`, `PrefabRef<TComponent>`) to replace RCore's `AssetBundleRef` family.
  - Zero dependency on other RevCore packages.

  See [`docs/superpowers/specs/2026-05-19-revcore-addressables-design.md`](../../../docs/superpowers/specs/2026-05-19-revcore-addressables-design.md) for the design and [`docs/migration/rcore-to-revcore-api-map.csv`](../../../docs/migration/rcore-to-revcore-api-map.csv) for the row-by-row migration map.

  ## Test plan

  - [ ] Unity EditMode tests pass (loader, downloader, catalog, wrappers — fake `IResourceLocator`).
  - [ ] Unity PlayMode tests pass (instantiate, scene, prefab ref).
  - [ ] `python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json` reports 100%.
  - [ ] PublicAPI surface sealed (`Unshipped.txt` empty except header).
  - [ ] Manual smoke: load a known address from a sample scene, instantiate, release, verify no leak in Addressables profiler.

  🤖 Generated with [Claude Code](https://claude.com/claude-code)
  '@
  gh pr create --title "feat(addressables): RevCore.Addressables v1.0.0" --body $body
  ```

  Capture the resulting PR URL and link it in the next session's handoff.

---

## Self-Review Checklist

After completing all tasks above, run this final pass before declaring the work shippable.

- [ ] **Spec coverage:** Every section of `docs/superpowers/specs/2026-05-19-revcore-addressables-design.md` is implemented or explicitly deferred in a follow-up issue.
- [ ] **No placeholders:** No `TBD`, `TODO`, or "see later" left in production code, tests, README, or PublicAPI files.
- [ ] **Type consistency:** Method names match between plan, spec, code, PublicAPI files, and migration CSV.
- [ ] **Lifetime contract uniform:** Every `LoadAssetAsync` / `LoadAssetsAsync` / `InstantiateAsync` / scene / download method handles success, failure, and cancellation per the same pattern documented in the spec §6.
- [ ] **Zero-dep verified:** `Runtime/RevCore.Addressables.Runtime.asmdef` references only Unity + UniTask assemblies; no other `RevCore.*` asmdef.
- [ ] **Tests passing:** EditMode + PlayMode green, xmldoc 100%, PublicAPI sealed.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-19-revcore-addressables-implementation.md`.

Two execution options:

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks, fast iteration. Uses `superpowers:subagent-driven-development`.
2. **Inline Execution** — execute tasks in this session with checkpoints, using `superpowers:executing-plans`.

Which approach?
