# SheetX Collection Depth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a developer choose, per collection, whether its data is serialized inside the Global ScriptableObject (`Inline`) or stored in its own ScriptableObject asset (`SeparateAsset`, today's behaviour), with `global.player.Characters` reading identically in both.

**Architecture:** One `depth` field on `SheetXCollectionDefinition` drives three layers. The generator emits the same `<Name>ConfigCollection.cs` file in both modes, changing only the declaration (`: SheetXConfigCollectionBase` versus `[Serializable]` with no base). The baker gains a depth branch at six sites: it skips the ScriptableObject gate, asset creation, reference assignment and `SetLoaded` for inline collections, and writes their rows into a nested property of Global instead. A durable snapshot outside `SessionState` makes a failed depth change recoverable across the domain reload that the existing per-bake rollback cannot cross.

**Tech Stack:** Unity 2022.3 editor-only C#, NUnit EditMode tests, Newtonsoft.Json, `SerializedObject`/`SerializedProperty`, `AssetDatabase`.

**Spec:** `docs/superpowers/specs/2026-09-15-sheetx-collection-depth-design.md`

## Global Constraints

- `.cs` files use **tabs** for indentation and **CRLF** line endings. Generated source uses the `NL = "\r\n"` and `T = "\t"` constants already in `SheetXCollectionGenerator`.
- Private instance fields are `m_camelCase`. Test methods are `snake_case_descriptive`.
- `SeparateAsset` is the default value of `depth`, so an existing project's layout never changes without the developer choosing it.
- SheetX **never deletes** a leftover collection asset or generated script. Leaving them is what makes `Inline` → `SeparateAsset` reversible.
- Editor-only code. Nothing in `Assets/RCore.SheetX/Runtime` changes in this plan.
- Tests reach internals through `[assembly: InternalsVisibleTo("RCore.SheetX.Tests")]` at `Assets/RCore.SheetX/Editor/AssemblyInfo.cs:3`.
- Tests do **not** compile generated code at run time. Anything the baker must load is a hand-written fixture type in `Assets/RCore.SheetX/Tests/`.
- Build gate, run from `E:/Projects/_/RCore`, **without** `--no-restore`:
  `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
  Five pre-existing CS0649 warnings in `SheetXRowTypeTests.cs` are expected noise.
- Headless EditMode run (no `-quit`), new `tN.xml` per run:
  ```
  & "D:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "E:/Projects/_/RCore" -runTests -testPlatform EditMode -testFilter "RCore.SheetX.Tests" -testResults "E:/Projects/_/RCore/Temp/tN.xml" -logFile "E:/Projects/_/RCore/Temp/tN.log"
  ```
  The PowerShell call returns **before** Unity finishes writing the XML. Wait for it:
  `for i in $(seq 1 40); do [ -f Temp/tN.xml ] && break; sleep 5; done`
  On a **compile error no XML is ever written** — read the tail of the `.log` instead of waiting out the loop.
- Parse results with `python "$TEMP/parse.py" Temp/tN.xml` (grep/sed mangle CDATA). If `$TEMP/parse.py` is missing, recreate it:
  ```python
  import re,sys
  s=open(sys.argv[1],encoding='utf-8').read()
  print(re.search(r'total="\d+" passed="\d+" failed="\d+"',s).group(0))
  for m in re.finditer(r'<test-case[^>]*name="([a-z_0-9]+)"[^>]*result="Failed"',s):
      n=m.group(1); i=s.find('CDATA[',m.end())
      print('FAIL', n, repr(s[i+6:i+500].split(']]>')[0]))
  ```
- Baseline before this plan: **349 test cases, 0 failures**.
- **Commits:** the repository rule is *never commit or push without an explicit request from the user*, and `Co-Authored-By` is omitted. Each task below ends with a ready-to-run commit command. Run it only when the user has asked for commits; otherwise leave the work staged and say so.
- Update the root `CHANGELOG.md` before any commit (Task 9 covers the release entry).

---

## File Structure

| File | Responsibility | Task |
| --- | --- | --- |
| `Assets/RCore.SheetX/Editor/SheetXSettings.cs` | `SheetXCollectionDepth` enum, `depth` field on `SheetXCollectionDefinition` | 1 |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs` | Emit inline declaration; `using System;` for inline files | 2 |
| `Assets/RCore.SheetX/Tests/InlinePlayerFixture.cs` *(new)* | Hand-written Global + inline group types the baker tests load | 3 |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs` | Depth on `Collection`; depth branch at the six sites; nested write | 3, 4 |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSettings.cs` | `DetectDepthChanges` from generated source on disk | 5 |
| `Assets/RCore.SheetX/Editor/Collection/SheetXMigrationSnapshot.cs` *(new)* | Durable snapshot written before generate, restore after failure | 6 |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs` | Capture the snapshot before writing sources | 6 |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionsWindow.cs` | Depth dropdown; `Auto Load` disabled for inline | 7 |
| `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs` | Generator cases | 2 |
| `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs` | Bake, AutoLoad, mixed-mode, round-trip cases | 3, 4 |
| `Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs` *(new)* | Depth detection and migration restore | 5, 6 |
| `Assets/RCore.SheetX/Document/Document.md`, both `CHANGELOG.md` | User-facing contract | 9 |

---

## Task 1: Depth on the settings model

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs:48-74`
- Test: `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `public enum SheetXCollectionDepth { Inline = 1, SeparateAsset = 2 }` in namespace `RCore.SheetX.Editor`; `public SheetXCollectionDepth depth` on `SheetXCollectionDefinition`, defaulting to `SheetXCollectionDepth.SeparateAsset`. Every later task branches on this field.

- [ ] **Step 1: Write the failing test**

Append to `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`, inside `class CollectionGenerationTests`:

```csharp
		[Test]
		public void collection_definitions_default_to_separate_asset_depth()
		{
			// SeparateAsset is what every project shipped before this feature had, so it has to be
			// the value a freshly constructed definition carries — otherwise upgrading rewrites layouts.
			var definition = new SheetXCollectionDefinition { name = "Player" };

			Assert.That(definition.depth, Is.EqualTo(SheetXCollectionDepth.SeparateAsset));
			Assert.That((int)SheetXCollectionDepth.Inline, Is.EqualTo(1));
			Assert.That((int)SheetXCollectionDepth.SeparateAsset, Is.EqualTo(2));
		}
```

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: FAIL with `CS0246: The type or namespace name 'SheetXCollectionDepth' could not be found`.

- [ ] **Step 3: Add the enum and the field**

In `Assets/RCore.SheetX/Editor/SheetXSettings.cs`, immediately above `public sealed class SheetXCollectionDefinition`:

```csharp
	/// <summary>
	/// Where a collection's data is stored. Global is always an asset and has no depth.
	/// </summary>
	public enum SheetXCollectionDepth
	{
		/// <summary>Serialized inside the Global asset. No separate asset is created.</summary>
		Inline = 1,
		/// <summary>Its own ScriptableObject asset, referenced from Global.</summary>
		SeparateAsset = 2,
	}
```

Then inside `SheetXCollectionDefinition`, after the `autoLoad` field:

```csharp
		/// <summary>Gets or sets where this collection's data is stored. Ignored for Global.</summary>
		public SheetXCollectionDepth depth = SheetXCollectionDepth.SeparateAsset;
```

- [ ] **Step 4: Run the build and the focused test**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-1.xml` per the Global Constraints, then parse it.
Expected: `total="350" passed="350" failed="0"`

- [ ] **Step 5: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/SheetXSettings.cs Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs
git commit -m "feat(sheetx): add per-collection depth to the settings model"
```

---

## Task 2: Generator emits the inline declaration

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs:68-100, 102-123, 416-472`
- Test: `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`

**Interfaces:**
- Consumes: `SheetXCollectionDepth` from Task 1.
- Produces: `EmitFiles` still returns one entry per collection type — `"PlayerConfigCollection.cs"` exists in **both** modes. Under `Inline` that file declares `[Serializable] public partial class PlayerConfigCollection` with no base type and carries `using System;`. `GlobalConfigCollection.cs` is unchanged: `public PlayerConfigCollection player;` either way.
- Also produces: `internal static SheetXCollectionDepth DepthOf(SheetXSettings settings, string collectionName)` — returns `SeparateAsset` for Global and for any name with no matching definition. Tasks 3-7 call it.

Keeping the per-collection file in both modes is deliberate. `DeleteLegacyGeneratedSource` (`SheetXCollectionExportSession.cs:427`) removes only the one hardcoded legacy filename, so folding the inline group elsewhere would strand the old `PlayerConfigCollection.cs` and fail the build with CS0101.

- [ ] **Step 1: Write the failing test**

Append to `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`:

```csharp
		[Test]
		public void inline_depth_emits_a_serializable_group_in_its_own_file()
		{
			var settings = Settings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition
				{
					name = "Player",
					depth = SheetXCollectionDepth.Inline,
				});

				var files = SheetXCollectionGenerator.EmitFiles(settings, new[]
				{
					Generated("excel-a", "Characters", "Player", "Characters", "id:int"),
				});
				string playerSource = files["PlayerConfigCollection.cs"];
				string globalSource = files["GlobalConfigCollection.cs"];

				// The file must still exist: DeleteLegacyGeneratedSource only removes the one legacy
				// name, so dropping this file would leave the old ": SheetXConfigCollectionBase"
				// declaration on disk and break the build with CS0101.
				Assert.That(playerSource, Does.Contain("[Serializable]"));
				Assert.That(playerSource, Does.Contain("public partial class PlayerConfigCollection"));
				Assert.That(playerSource, Does.Not.Contain("SheetXConfigCollectionBase"));
				Assert.That(playerSource, Does.Contain("using System;"));
				Assert.That(playerSource, Does.Contain("public CharactersSX[] Characters;"));

				// The read path is what must not move: global.player.Characters in both modes.
				Assert.That(globalSource, Does.Contain("public PlayerConfigCollection player;"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void separate_asset_depth_still_emits_the_scriptable_object_declaration()
		{
			var settings = Settings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition
				{
					name = "Player",
					depth = SheetXCollectionDepth.SeparateAsset,
				});

				string playerSource = SheetXCollectionGenerator.EmitFiles(settings, new[]
				{
					Generated("excel-a", "Characters", "Player", "Characters", "id:int"),
				})["PlayerConfigCollection.cs"];

				Assert.That(playerSource, Does.Contain(
					"public partial class PlayerConfigCollection : SheetXConfigCollectionBase"));
				Assert.That(playerSource, Does.Not.Contain("[Serializable]"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void depth_of_reports_separate_asset_for_global_and_undefined_collections()
		{
			var settings = Settings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition
				{
					name = "Player",
					depth = SheetXCollectionDepth.Inline,
				});

				Assert.That(SheetXCollectionGenerator.DepthOf(settings, "Player"),
					Is.EqualTo(SheetXCollectionDepth.Inline));
				// Global is the composition root and is always an asset.
				Assert.That(SheetXCollectionGenerator.DepthOf(settings, "Global"),
					Is.EqualTo(SheetXCollectionDepth.SeparateAsset));
				// CollectionOrder accepts a table naming a collection with no definition; it must not throw.
				Assert.That(SheetXCollectionGenerator.DepthOf(settings, "Missing"),
					Is.EqualTo(SheetXCollectionDepth.SeparateAsset));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}
```

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: FAIL with `CS0117: 'SheetXCollectionGenerator' does not contain a definition for 'DepthOf'`.

- [ ] **Step 3: Implement `DepthOf` and the inline declaration**

In `SheetXCollectionGenerator.cs`, add next to `IsGlobal` at the bottom of the class:

```csharp
		/// <summary>
		/// Resolves a collection's storage depth. Global is the composition root and is always an
		/// asset; a name with no definition (CollectionOrder accepts table-only names) keeps the
		/// default so an unknown collection never silently becomes inline.
		/// </summary>
		internal static SheetXCollectionDepth DepthOf(SheetXSettings settings, string collectionName)
		{
			if (IsGlobal(collectionName))
				return SheetXCollectionDepth.SeparateAsset;
			var definition = settings?.collections?.FirstOrDefault(candidate =>
				candidate != null && string.Equals(candidate.name, collectionName, StringComparison.Ordinal));
			return definition?.depth ?? SheetXCollectionDepth.SeparateAsset;
		}
```

In `EmitFiles`, replace the body of the `foreach (string collection in collectionOrder)` loop (`:88-98`) with:

```csharp
			foreach (string collection in collectionOrder)
			{
				bool isGlobal = IsGlobal(collection);
				bool inline = DepthOf(settings, collection) == SheetXCollectionDepth.Inline;
				// An inline group is declared [Serializable], which needs System in scope. Global
				// only needed it when Configuration emitted nested classes.
				bool includeSystem = inline || isGlobal && configuration != null && configuration.Groups.Count > 0;
				bool includeUnityEngine = isGlobal && UsesUnityEngine(configuration);
				var collectionSource = BeginSource(settings, includeSystem, includeUnityEngine);
				AppendCollection(collectionSource, indent, collection, collectionOrder, ordered, configuration, inline);
				EndSource(collectionSource, settings);
				string typeName = SheetXCollectionNaming.CollectionTypeName(collection);
				files.Add(typeName + ".cs", collectionSource.ToString());
			}
```

Change the `AppendCollection` signature and its declaration lines (`:416-426`):

```csharp
		private static void AppendCollection(
			StringBuilder source, string indent, string collection,
			IEnumerable<string> collectionOrder, List<SheetXCollectionGeneratedTable> tables,
			ConfigSheetData configuration, bool inline)
		{
			bool global = IsGlobal(collection);
			if (inline)
				source.Append(indent).Append("[Serializable]").Append(NL);
			source.Append(indent).Append("public partial class ")
				.Append(SheetXCollectionNaming.CollectionTypeName(collection));
			if (!inline)
			{
				source.Append(" : ").Append(global ? "GlobalConfigCollectionBase" : "SheetXConfigCollectionBase");
			}
			source.Append(NL);
			source.Append(indent).Append('{').Append(NL);
```

The rest of `AppendCollection` is unchanged — the table-field loop and the Global feature-reference loop (`:464-469`) already emit the right thing for both modes.

- [ ] **Step 4: Run the build and the tests**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-2.xml`, then parse.
Expected: `total="353" passed="353" failed="0"` — the three new cases pass and every pre-existing `CollectionGenerationTests` case still passes, which is the regression gate for `SeparateAsset`.

- [ ] **Step 5: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs
git commit -m "feat(sheetx): emit inline collection groups as serializable classes"
```

---

## Task 3: Baker bakes an inline group into Global

**Files:**
- Create: `Assets/RCore.SheetX/Tests/InlinePlayerFixture.cs`
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs:58-64, 240-274, 432-465, 543-570, 572-610, 628-649`
- Test: `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs`

**Interfaces:**
- Consumes: `SheetXCollectionDepth` (Task 1), `SheetXCollectionGenerator.DepthOf` (Task 2).
- Produces: `Collection.Depth`; `ApplyRows(object target, Collection collection, string groupFieldName, Type rowOwnerType, out string error)` writing through a dotted `SerializedProperty` path when `groupFieldName` is non-empty. An inline collection creates no asset, assigns no Global reference, and has no `SetLoaded` call.

This is one task because the six sites must change together: fixing the `:642` type gate alone lets an inline collection reach `CreateOrLoadAssets`, where `ScriptableObject.CreateInstance` (`:499`) returns null for a non-ScriptableObject type and the bake fails at `:502`. There is no working intermediate state.

- [ ] **Step 1: Write the fixture**

Create `Assets/RCore.SheetX/Tests/InlinePlayerFixture.cs`. Tests never compile generated code, so the baker needs hand-written types shaped exactly like Task 2's output:

```csharp
using System;

namespace RCore.SheetX.Tests.Inline
{
	/// <summary>Stands in for a generated Inline group: [Serializable], no ScriptableObject base.</summary>
	[Serializable]
	public sealed class PlayerConfigCollection
	{
		public BakeItemsRow[] Characters;
	}

	/// <summary>Stands in for a generated Global that holds one inline group and one asset reference.</summary>
	public sealed class GlobalConfigCollection : GlobalConfigCollectionBase
	{
		public PlayerConfigCollection player;
		public BakeShopConfigCollection bakeShop;
		public string environment;
	}
}
```

- [ ] **Step 2: Write the failing tests**

Append to `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs`. Add `InlineGlobalPath` next to the other path constants and a settings helper beside `CreateSettings`:

```csharp
		private const string InlineGlobalPath = ResourcesFolder + "/GlobalConfigCollection.asset";
		private const string InlineJsonPath = JsonFolder + "/InlineCharacters.txt";

		private static SheetXSettings CreateInlineSettings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.enableCollections = true;
			settings.collectionNamespace = "RCore.SheetX.Tests.Inline";
			settings.collectionCodeFolder = TempRoot + "/Code";
			settings.collectionAssetFolder = AssetFolder;
			settings.collectionJsonFolder = JsonFolder;
			settings.globalResourcesFolder = ResourcesFolder;
			settings.collections.Add(new SheetXCollectionDefinition
			{
				name = "Player",
				autoLoad = true,
				depth = SheetXCollectionDepth.Inline,
			});
			settings.sheetBindings.Add(new SheetXSheetBinding
			{
				sourceId = "book.xlsx",
				sheetName = "InlineCharacters",
				outputMode = SheetXSheetOutputMode.ExistingDataClass,
				collectionName = "Player",
				rowTypeName = typeof(BakeItemsRow).AssemblyQualifiedName,
				fieldName = "Characters",
			});
			return settings;
		}

		[Test]
		public void inline_depth_bakes_rows_into_global_and_creates_no_feature_asset()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":7,\"name\":\"mage\"}]");
			var settings = CreateInlineSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "Player", out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				Assert.That(global, Is.Not.Null);
				Assert.That(global.player, Is.Not.Null);
				Assert.That(global.player.Characters, Has.Length.EqualTo(1));
				Assert.That(global.player.Characters[0].id, Is.EqualTo(7));
				Assert.That(global.player.Characters[0].name, Is.EqualTo("mage"));

				// An inline group has no asset of its own. Creating one would defeat the whole point.
				Assert.That(
					AssetDatabase.LoadMainAssetAtPath(AssetFolder + "/PlayerConfigCollection.asset"),
					Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void inline_bake_leaves_sibling_global_data_untouched()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":7,\"name\":\"mage\"}]");
			var settings = CreateInlineSettings();
			try
			{
				EnsureFolder(ResourcesFolder);
				var seeded = ScriptableObject.CreateInstance<Inline.GlobalConfigCollection>();
				seeded.environment = "staging";
				AssetDatabase.CreateAsset(seeded, InlineGlobalPath);
				AssetDatabase.SaveAssets();

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "Player", out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				// PopulateObject writes only the keys present in the JSON, so a sibling group that
				// this bake did not touch must survive it verbatim.
				Assert.That(global.environment, Is.EqualTo("staging"));
				Assert.That(global.player.Characters, Has.Length.EqualTo(1));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void inline_bake_builds_the_group_when_global_is_created_fresh()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":1,\"name\":\"rogue\"}]");
			var settings = CreateInlineSettings();
			try
			{
				// No Global asset exists yet, so `player` starts null and Newtonsoft's default
				// ObjectCreationHandling.Auto has to construct it rather than reuse one.
				Assert.That(AssetDatabase.LoadMainAssetAtPath(InlineGlobalPath), Is.Null);

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "Player", out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				Assert.That(global.player.Characters[0].name, Is.EqualTo("rogue"));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void mixed_depths_put_a_nested_group_and_an_asset_reference_on_one_global()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":7,\"name\":\"mage\"}]");
			File.WriteAllText(JsonPath, "[{\"id\":2,\"name\":\"potion\"}]");
			var settings = CreateInlineSettings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition
				{
					name = "BakeShop",
					autoLoad = true,
					depth = SheetXCollectionDepth.SeparateAsset,
				});
				settings.sheetBindings.Add(new SheetXSheetBinding
				{
					sourceId = "book.xlsx",
					sheetName = "BakeItems",
					outputMode = SheetXSheetOutputMode.ExistingDataClass,
					collectionName = "BakeShop",
					rowTypeName = typeof(BakeItemsRow).AssemblyQualifiedName,
					fieldName = "items",
				});

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, autoLoadOnly: false, out string error),
					Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				// This is the feature as it will actually be used: one of each on the same root.
				Assert.That(global.player.Characters[0].id, Is.EqualTo(7));
				Assert.That(global.bakeShop, Is.Not.Null);
				Assert.That(global.bakeShop.items[0].id, Is.EqualTo(2));
				Assert.That(AssetDatabase.GetAssetPath(global.bakeShop), Is.EqualTo(FeaturePath));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run the headless suite into `Temp/t-depth-3a.xml`, then parse.
Expected: FAIL on all four new cases with an error containing `must be a concrete SheetXConfigCollectionBase` — `TryFindCollectionType` (`:642`) is the first gate an inline collection hits.

- [ ] **Step 4: Carry depth on `Collection` and open the type gate**

In `SheetXCollectionBaker.cs`, add to the `Collection` class (`:58-64`):

```csharp
			internal SheetXCollectionDepth Depth;
```

In `TryBuildCollections` (`:294-304`), resolve depth first and pass it to the type lookup:

```csharp
			foreach (var definition in settings.collections)
			{
				var depth = SheetXCollectionGenerator.DepthOf(settings, definition.name);
				if (!TryFindCollectionType(settings, definition.name, depth, out var collectionType, out error))
					return false;
				collections.Add(new Collection
				{
					Name = definition.name,
					// An inline group is serialized inside Global, so it is loaded exactly when Global
					// is. Honouring its own stale autoLoad would skip its rows while Global is saved.
					AutoLoad = depth == SheetXCollectionDepth.Inline
						? settings.collections.First(c => c.builtInGlobal).autoLoad
						: definition.autoLoad,
					Depth = depth,
					Type = collectionType,
				});
			}
```

Replace the guard in `TryFindCollectionType` (`:642-647`) and widen its signature:

```csharp
		private static bool TryFindCollectionType(
			SheetXSettings settings, string collectionName, SheetXCollectionDepth depth,
			out Type type, out string error)
		{
			error = null;
			string collectionNamespace = settings.ResolveCollectionNamespace();
			string name = string.IsNullOrEmpty(collectionNamespace)
				? SheetXCollectionNaming.CollectionTypeName(collectionName)
				: collectionNamespace.Trim() + "." + SheetXCollectionNaming.CollectionTypeName(collectionName);
			type = FindType(name);
			if (type == null)
			{
				error = $"Collection type '{name}' was not found after reload.";
				return false;
			}
			if (depth == SheetXCollectionDepth.Inline)
			{
				// An inline group is a plain serializable class held by Global, not an asset type.
				if (type.IsAbstract || !type.IsSerializable)
				{
					error = $"Inline collection type '{name}' must be a concrete [Serializable] class.";
					type = null;
					return false;
				}
				return true;
			}
			if (!typeof(SheetXConfigCollectionBase).IsAssignableFrom(type) || type.IsAbstract)
			{
				error = $"Collection type '{name}' must be a concrete SheetXConfigCollectionBase.";
				type = null;
				return false;
			}
			return true;
		}
```

- [ ] **Step 5: Skip asset creation, references and `SetLoaded` for inline**

In `CreateOrLoadAssets` (`:443`), exclude inline collections from asset creation:

```csharp
			foreach (var collection in collections.Where(collection =>
				!IsGlobal(collection.Name) && collection.Depth != SheetXCollectionDepth.Inline))
```

In `ApplyGlobalReferences` (`:580` and `:602`), change **both** loops the same way so `:607` never indexes a missing key:

```csharp
			foreach (var collection in collections.Where(collection =>
				!IsGlobal(collection.Name) && collection.Depth != SheetXCollectionDepth.Inline))
```

In the `SetLoaded` loop (`:266-274`), skip inline collections — they have no `IsLoaded`:

```csharp
						foreach (var collection in collections)
						{
							if (collection.Depth == SheetXCollectionDepth.Inline)
								continue;
							if ((!autoLoadOnly || collection.AutoLoad)
								&& (string.IsNullOrEmpty(collectionName)
									|| string.Equals(collection.Name, collectionName, StringComparison.Ordinal)))
							{
								assets[collection.Name].SetLoaded();
							}
						}
```

- [ ] **Step 6: Write inline rows into the nested Global property**

Change the `ApplyRows` call site (`:248`) to route inline collections at Global:

```csharp
							bool inline = collection.Depth == SheetXCollectionDepth.Inline;
							object target = inline ? (object)global : assets[collection.Name];
							string groupField = inline
								? SheetXCollectionNaming.ToCamelIdentifier(collection.Name)
								: null;
							if (!ApplyRows(target, collection, groupField, out error))
								throw new InvalidOperationException(error);
```

Replace `ApplyRows` (`:543-570`):

```csharp
		/// <summary>
		/// Writes each table's rows into <paramref name="target"/>. When <paramref name="groupFieldName"/>
		/// is set the rows land one level down, inside that inline group on the Global asset.
		/// </summary>
		private static bool ApplyRows(
			object target, Collection collection, string groupFieldName, out string error)
		{
			error = null;
			var unityTarget = (UnityEngine.Object)target;
			var serializedObject = new SerializedObject(unityTarget);
			bool inline = !string.IsNullOrEmpty(groupFieldName);
			Type rowOwnerType = collection.Type;
			if (inline)
			{
				var groupField = unityTarget.GetType().GetField(
					groupFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (groupField == null || groupField.FieldType != collection.Type)
				{
					error = $"Global collection has no inline group '{groupFieldName}' of type '{collection.Type.FullName}'.";
					return false;
				}
			}

			foreach (var table in collection.Tables)
			{
				string fieldName = SheetXCollectionSettings.ResolveFieldName(table.Binding);
				// A dotted path resolves nested [Serializable] fields: "player.Characters".
				string propertyPath = inline ? groupFieldName + "." + fieldName : fieldName;
				var property = serializedObject.FindProperty(propertyPath);
				if (property == null || !property.isArray || property.propertyType == SerializedPropertyType.String)
				{
					error = TableError(table, "Collection field is missing or is not an array.");
					return false;
				}
				var field = rowOwnerType.GetField(
					fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null || !field.FieldType.IsArray
					|| field.FieldType.GetElementType() != table.RowType)
				{
					error = TableError(table, "Collection field array type does not match the selected row type.");
					return false;
				}
				// ObjectCreationHandling defaults to Auto: it reuses an existing group instance and
				// constructs one when the field is null, which a freshly created Global needs.
				string payload = "{\"" + field.Name + "\":" + table.Json + "}";
				JsonConvert.PopulateObject(
					inline ? "{\"" + groupFieldName + "\":" + payload + "}" : payload,
					unityTarget);
				serializedObject.UpdateIfRequiredOrScript();
			}
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			return true;
		}
```

Note `rowOwnerType` is `collection.Type` in both modes — for inline that is the group class, which is exactly where `Characters` lives.

- [ ] **Step 7: Run the build and the tests**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-3b.xml`, then parse.
Expected: `total="357" passed="357" failed="0"` — including every pre-existing `CollectionBakeTests` case, which is the regression gate for `SeparateAsset`.

- [ ] **Step 8: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs Assets/RCore.SheetX/Tests/InlinePlayerFixture.cs Assets/RCore.SheetX/Tests/InlinePlayerFixture.cs.meta Assets/RCore.SheetX/Tests/CollectionBakeTests.cs
git commit -m "feat(sheetx): bake inline collection groups into the Global asset"
```

---

## Task 4: Inline AutoLoad, Configuration ordering and round trip

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs:240-250` (ordering guard only)
- Test: `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs`

**Interfaces:**
- Consumes: everything from Task 3.
- Produces: no new API. This task proves three behaviours the spec promises that Task 3's cases do not reach.

- [ ] **Step 0: Add the Configuration marker to the Inline fixture namespace**

`TryReadConfiguration` (`SheetXCollectionBaker.cs:390-393`) resolves `<collectionNamespace>.SheetXCollectionPaths` by exact `FullName`. Task 3's fixtures live in `RCore.SheetX.Tests.Inline`, and the only marker in the project is at `RCore.SheetX.Tests.SheetXCollectionPaths` — so for every `CreateInlineSettings()` test, `FindType` returns null, `TryReadConfiguration` returns early, and `configuration` stays null. Without this step the Configuration test below fails because Configuration never loads at all, not because of the ordering it means to check.

Append to `Assets/RCore.SheetX/Tests/Inline/GlobalConfigCollection.cs`, inside the `RCore.SheetX.Tests.Inline` namespace:

```csharp
	public static partial class SheetXCollectionPaths
	{
		internal const string Configuration = "Assets/SheetXTestsTemp/Editor/Json/Configuration.txt";
	}
```

This is safe for the Task 3 tests already passing: `SetUp` writes `{}` to that path, and `PopulateObject("{}", global)` is a no-op.

- [ ] **Step 1: Write the failing tests**

Append to `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs`:

```csharp
		[Test]
		public void inline_group_bakes_under_auto_load_even_when_its_own_flag_is_false()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":9,\"name\":\"ranger\"}]");
			var settings = CreateInlineSettings();
			try
			{
				// A collection switched to Inline keeps whatever autoLoad it had. Honouring that flag
				// would skip its rows while Global is saved anyway, persisting a stale group.
				settings.collections.First(c => c.name == "Player").autoLoad = false;
				settings.collections.First(c => c.builtInGlobal).autoLoad = true;

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, autoLoadOnly: true, out string error),
					Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				Assert.That(global.player.Characters, Has.Length.EqualTo(1));
				Assert.That(global.player.Characters[0].id, Is.EqualTo(9));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void configuration_and_inline_group_do_not_clobber_each_other()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":4,\"name\":\"cleric\"}]");
			File.WriteAllText(JsonFolder + "/Configuration.txt", "{\"environment\":\"prod\"}");
			AssetDatabase.ImportAsset(JsonFolder + "/Configuration.txt");
			var settings = CreateInlineSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, autoLoadOnly: false, out string error),
					Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				// Configuration is applied to Global at Baker.cs:238, before inline rows. Both write
				// into the same asset, so each must survive the other.
				Assert.That(global.environment, Is.EqualTo("prod"));
				Assert.That(global.player.Characters[0].name, Is.EqualTo("cleric"));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void returning_to_separate_asset_reuses_the_original_asset_and_guid()
		{
			File.WriteAllText(JsonPath, "[{\"id\":2,\"name\":\"potion\"}]");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "BakeShop", out string first), Is.True, first);
				string guid = AssetDatabase.AssetPathToGUID(FeaturePath);
				Assert.That(guid, Is.Not.Empty);

				// Simulate the round trip: the asset is deliberately never deleted when a collection
				// goes Inline, so coming back must find it rather than mint a new GUID. That is what
				// lets Prefab references that were left alone start resolving again.
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "BakeShop", out string second), Is.True, second);

				Assert.That(AssetDatabase.AssetPathToGUID(FeaturePath), Is.EqualTo(guid));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}
```

- [ ] **Step 2: Prove each test fails before it passes**

A test that has never failed has not been shown to detect anything. Two of these three cover behaviour Task 3 already implemented, so they pass on arrival — each must be driven red once, by temporarily reverting the exact line that makes it pass, then restored.

Run the headless suite into `Temp/t-depth-4a.xml` and parse it first to see the starting state.

For `inline_group_bakes_under_auto_load_even_when_its_own_flag_is_false`:
1. In `SheetXCollectionBaker.TryBuildCollections`, temporarily replace the inline `AutoLoad` expression with `AutoLoad = definition.autoLoad,`.
2. Run the suite into `Temp/t-depth-4a-red1.xml`. Expected: that test FAILS with `Expected: 1 But was: 0` — the group was skipped by the `autoLoadOnly` filter.
3. Restore the inline `AutoLoad` expression from Task 3 verbatim.

For `returning_to_separate_asset_reuses_the_original_asset_and_guid`:
1. In `TryLoadOrCreate` (`SheetXCollectionBaker.cs:476`), temporarily change `asset = AssetDatabase.LoadAssetAtPath<SheetXConfigCollectionBase>(path);` to `asset = null;`, which forces a fresh asset instead of reusing the existing one.
2. Run the suite into `Temp/t-depth-4a-red2.xml`. Expected: that test FAILS on the GUID assertion, or on the "exists but cannot load" guard at `:487`.
3. Restore the original line.

For `configuration_and_inline_group_do_not_clobber_each_other`: if it does not already fail, drive it red by temporarily moving `PopulateObject(configuration.Json, global)` (`:237-238`) to *after* the `foreach (var collection in collections)` row loop, run into `Temp/t-depth-4a-red3.xml`, confirm the failure, then restore.

Record each red run's failure message in the task report. A test that cannot be driven red by removing the code it covers is not testing that code — say so in the report rather than keeping it.

- [ ] **Step 2b: Confirm all three are green again**

Run the headless suite into `Temp/t-depth-4a-green.xml`, then parse.
Expected: all three new tests pass with every temporary revert undone.

- [ ] **Step 3: Guarantee Configuration is applied before inline rows**

If the ordering test failed, confirm in `TryLoadData` that `PopulateObject(configuration.Json, global)` (`:237-238`) runs **before** the `foreach (var collection in collections)` row loop (`:240`), and move it above the loop if not. Add this comment directly above it so a later refactor does not reorder them:

```csharp
						// Configuration must be applied before inline rows: both write into Global, and
						// whichever runs last wins for any key they share.
						if (configuration != null)
							JsonConvert.PopulateObject(configuration.Json, global);
```

- [ ] **Step 4: Run the build and the tests**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-4b.xml`, then parse.
Expected: `total="360" passed="360" failed="0"`

- [ ] **Step 5: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs Assets/RCore.SheetX/Tests/CollectionBakeTests.cs
git commit -m "test(sheetx): cover inline auto-load, Configuration ordering and asset reuse"
```

---

## Task 5: Detect a depth change from the generated source

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSettings.cs`
- Create: `Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs`

**Interfaces:**
- Consumes: `SheetXCollectionDepth` (Task 1), `SheetXCollectionGenerator.DepthOf` (Task 2).
- Produces: `internal static List<string> DetectDepthChanges(SheetXSettings settings)` — names of collections whose stored depth disagrees with the declaration in the generated `.cs` already on disk, ordinal-sorted. A collection with no generated file yet is not a change. Task 6 and Task 7 call it.

Settings store only the current depth, so "changed" has to be derived. The generated file is already the record of what was last exported; adding a `lastExportedDepth` field would create a second source of truth that can drift from it.

- [ ] **Step 1: Write the failing test**

Create `Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	public class CollectionDepthChangeTests
	{
		private const string TestRoot = "Assets/SheetXDepthTemp";
		private const string CodeFolder = TestRoot + "/Code";

		[SetUp]
		public void SetUp()
		{
			AssetDatabase.DeleteAsset(TestRoot);
			if (!AssetDatabase.IsValidFolder(TestRoot))
				AssetDatabase.CreateFolder("Assets", "SheetXDepthTemp");
			if (!AssetDatabase.IsValidFolder(CodeFolder))
				AssetDatabase.CreateFolder(TestRoot, "Code");
		}

		[TearDown]
		public void TearDown()
		{
			AssetDatabase.DeleteAsset(TestRoot);
		}

		[Test]
		public void depth_change_is_detected_from_the_generated_declaration_on_disk()
		{
			var settings = CreateSettings(SheetXCollectionDepth.Inline);
			try
			{
				// The file on disk still says SeparateAsset; settings now say Inline.
				WriteGenerated("public partial class PlayerConfigCollection : SheetXConfigCollectionBase");

				Assert.That(SheetXCollectionSettings.DetectDepthChanges(settings),
					Is.EqualTo(new[] { "Player" }));
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void matching_depth_and_missing_file_are_not_changes()
		{
			var settings = CreateSettings(SheetXCollectionDepth.Inline);
			try
			{
				// No generated file yet: a first export is not a depth change.
				Assert.That(SheetXCollectionSettings.DetectDepthChanges(settings), Is.Empty);

				WriteGenerated("[Serializable]\r\npublic partial class PlayerConfigCollection");
				Assert.That(SheetXCollectionSettings.DetectDepthChanges(settings), Is.Empty);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void reverting_to_separate_asset_is_also_a_change()
		{
			var settings = CreateSettings(SheetXCollectionDepth.SeparateAsset);
			try
			{
				WriteGenerated("[Serializable]\r\npublic partial class PlayerConfigCollection");

				Assert.That(SheetXCollectionSettings.DetectDepthChanges(settings),
					Is.EqualTo(new[] { "Player" }));
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		private static void WriteGenerated(string declaration)
		{
			File.WriteAllText(
				CodeFolder + "/PlayerConfigCollection.cs",
				"/***\r\n * This script is automatically generated by SheetX.\r\n ***/\r\n" + declaration + "\r\n{\r\n}\r\n");
		}

		private static SheetXSettings CreateSettings(SheetXCollectionDepth depth)
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.enableCollections = true;
			settings.collectionCodeFolder = CodeFolder;
			settings.collections.Add(new SheetXCollectionDefinition { name = "Player", depth = depth });
			return settings;
		}
	}
}
```

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: FAIL with `CS0117: 'SheetXCollectionSettings' does not contain a definition for 'DetectDepthChanges'`.

- [ ] **Step 3: Implement `DetectDepthChanges`**

Add to `SheetXCollectionSettings.cs`, inside the `#region Validation` block:

```csharp
		/// <summary>
		/// Names the collections whose stored depth disagrees with the generated source already on
		/// disk. The generated file is the record of what was last exported, so there is no
		/// separate "last exported depth" field to drift from it. A collection with no generated
		/// file yet is a first export, not a change.
		/// </summary>
		internal static List<string> DetectDepthChanges(SheetXSettings settings)
		{
			var changed = new List<string>();
			if (settings == null || !settings.enableCollections)
				return changed;

			string folder = NormalizePath(settings.ResolveCollectionCodeFolder());
			foreach (var definition in settings.collections ?? new List<SheetXCollectionDefinition>())
			{
				if (definition == null || definition.builtInGlobal)
					continue;

				string typeName = SheetXCollectionNaming.CollectionTypeName(definition.name);
				string path = string.IsNullOrEmpty(folder) ? typeName + ".cs" : folder + "/" + typeName + ".cs";
				if (!File.Exists(path))
					continue;

				bool onDiskIsAsset = File.ReadAllText(path)
					.IndexOf(typeName + " : SheetXConfigCollectionBase", StringComparison.Ordinal) >= 0;
				var onDisk = onDiskIsAsset
					? SheetXCollectionDepth.SeparateAsset
					: SheetXCollectionDepth.Inline;
				if (onDisk != SheetXCollectionGenerator.DepthOf(settings, definition.name))
					changed.Add(definition.name);
			}
			changed.Sort(StringComparer.Ordinal);
			return changed;
		}
```

Add `using System.IO;` to the file's using block if it is not already present.

- [ ] **Step 4: Run the build and the tests**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-5.xml`, then parse.
Expected: `total="363" passed="363" failed="0"`

- [ ] **Step 5: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSettings.cs Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs.meta
git commit -m "feat(sheetx): detect a collection depth change from generated source"
```

---

## Task 6: Durable migration snapshot across the domain reload

**Files:**
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXMigrationSnapshot.cs`
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs:310-330`
- Test: `Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs`

**Interfaces:**
- Consumes: `DetectDepthChanges` (Task 5).
- Produces:
  - `internal static void SheetXMigrationSnapshot.Capture(SheetXSettings settings, IReadOnlyCollection<string> changedCollections, IReadOnlyDictionary<string, string> previousSources)`
  - `internal static bool SheetXMigrationSnapshot.TryRestore(out string error)` — rewrites the previous sources, then clears the snapshot. Restoring asset data is left to the bake that follows recompilation, because writing old data into a new type is what corrupts it.
  - `internal static bool SheetXMigrationSnapshot.Exists { get; }`
  - `internal static void SheetXMigrationSnapshot.Clear()`

The existing rollback cannot reach this. `SheetXCollectionExportSession.cs:318` holds snapshots in a local consumed within one call, and the only state crossing the reload is the pending-bake entry in `SessionState` (`SheetXCollectionBaker.cs:134,136`), which carries no sources. The snapshot therefore lives in a file under `Library/`, outside `Assets/` and outside version control, so it also survives an editor restart mid-migration.

- [ ] **Step 1: Write the failing test**

Append to `Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs`:

```csharp
		[Test]
		public void migration_snapshot_restores_the_previous_generated_sources()
		{
			var settings = CreateSettings(SheetXCollectionDepth.Inline);
			try
			{
				string path = CodeFolder + "/PlayerConfigCollection.cs";
				const string previous =
					"public partial class PlayerConfigCollection : SheetXConfigCollectionBase\r\n{\r\n}\r\n";
				File.WriteAllText(path, previous);

				SheetXMigrationSnapshot.Capture(
					settings,
					new[] { "Player" },
					new Dictionary<string, string> { [path] = previous });

				// The migration proceeds and the new source lands, then the bake fails after the reload.
				File.WriteAllText(path, "[Serializable]\r\npublic partial class PlayerConfigCollection\r\n{\r\n}\r\n");
				Assert.That(SheetXMigrationSnapshot.Exists, Is.True);

				Assert.That(SheetXMigrationSnapshot.TryRestore(out string error), Is.True, error);

				// Sources go back first; the types they declare recompile before any asset is touched.
				Assert.That(File.ReadAllText(path), Is.EqualTo(previous));
				Assert.That(SheetXMigrationSnapshot.Exists, Is.False);
			}
			finally
			{
				SheetXMigrationSnapshot.Clear();
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void restore_without_a_snapshot_reports_instead_of_throwing()
		{
			SheetXMigrationSnapshot.Clear();

			Assert.That(SheetXMigrationSnapshot.TryRestore(out string error), Is.False);
			Assert.That(error, Does.Contain("No migration snapshot"));
		}
```

Add `using System.Collections.Generic;` to the test file's using block.

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: FAIL with `CS0103: The name 'SheetXMigrationSnapshot' does not exist in the current context`.

- [ ] **Step 3: Implement the snapshot store**

Create `Assets/RCore.SheetX/Editor/Collection/SheetXMigrationSnapshot.cs`:

```csharp
/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Preserves what a collection depth change replaces, so a bake that fails on the far side of
	/// the domain reload can be undone. The per-bake rollback in SheetXCollectionBaker cannot reach
	/// that far: its snapshots live in locals, and the only state crossing the reload is the
	/// pending-bake entry, which carries no sources. This store is a file under Library/ so it also
	/// survives an editor restart mid-migration.
	/// </summary>
	internal static class SheetXMigrationSnapshot
	{
		private sealed class Entry
		{
			public string SettingsAssetPath;
			public List<string> ChangedCollections = new List<string>();
			public Dictionary<string, string> PreviousSources = new Dictionary<string, string>();
		}

		private static string StorePath =>
			Path.Combine(Directory.GetCurrentDirectory(), "Library", "SheetX", "migration-snapshot.json");

		internal static bool Exists => File.Exists(StorePath);

		/// <summary>
		/// Records the sources a depth change is about to overwrite. Call before writing any
		/// generated file; a no-op when nothing changed depth.
		/// </summary>
		internal static void Capture(
			SheetXSettings settings,
			IReadOnlyCollection<string> changedCollections,
			IReadOnlyDictionary<string, string> previousSources)
		{
			if (changedCollections == null || changedCollections.Count == 0)
				return;

			var entry = new Entry
			{
				SettingsAssetPath = settings == null ? "" : AssetDatabase.GetAssetPath(settings),
				ChangedCollections = changedCollections.ToList(),
				PreviousSources = previousSources?.ToDictionary(pair => pair.Key, pair => pair.Value)
					?? new Dictionary<string, string>(),
			};
			Directory.CreateDirectory(Path.GetDirectoryName(StorePath) ?? "");
			File.WriteAllText(StorePath, JsonConvert.SerializeObject(entry, Formatting.Indented));
		}

		/// <summary>
		/// Puts the previous generated sources back and clears the snapshot. Only sources: asset
		/// data is restored by the bake that follows recompilation, because writing old data into a
		/// type that no longer matches is what corrupts it.
		/// </summary>
		internal static bool TryRestore(out string error)
		{
			error = null;
			if (!Exists)
			{
				error = "No migration snapshot to restore.";
				return false;
			}

			Entry entry;
			try
			{
				entry = JsonConvert.DeserializeObject<Entry>(File.ReadAllText(StorePath));
			}
			catch (Exception ex)
			{
				error = $"Migration snapshot could not be read: {ex.Message}";
				return false;
			}
			if (entry == null)
			{
				error = "Migration snapshot was empty.";
				return false;
			}

			try
			{
				foreach (var source in entry.PreviousSources)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(source.Key) ?? "");
					File.WriteAllText(source.Key, source.Value);
				}
			}
			catch (Exception ex)
			{
				error = $"Migration snapshot could not be restored: {ex.Message}";
				return false;
			}

			Clear();
			return true;
		}

		internal static void Clear()
		{
			if (Exists)
				File.Delete(StorePath);
		}
	}
}
```

- [ ] **Step 4: Capture the snapshot before sources are written**

In `SheetXCollectionExportSession.cs`, immediately before `CaptureSnapshots` is called (`:315-318`):

```csharp
			RequiresScriptReload = SourcesChanged(sources);
			var depthChanges = SheetXCollectionSettings.DetectDepthChanges(m_settings);
			if (depthChanges.Count > 0)
			{
				// A depth change outlives this call: the bake happens after a domain reload, where
				// the local snapshots below no longer exist. Persist what we are about to replace.
				string codeFolder = SheetXCollectionSettings.NormalizePath(
					m_settings.ResolveCollectionCodeFolder());
				var previousSources = sources.Keys
					.Select(name => codeFolder + "/" + name)
					.Where(File.Exists)
					.ToDictionary(path => path, File.ReadAllText);
				SheetXMigrationSnapshot.Capture(m_settings, depthChanges, previousSources);
			}
			List<FileSnapshot> snapshots;
```

- [ ] **Step 5: Run the build and the tests**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-6.xml`, then parse.
Expected: `total="365" passed="365" failed="0"`

- [ ] **Step 6: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/Collection/SheetXMigrationSnapshot.cs Assets/RCore.SheetX/Editor/Collection/SheetXMigrationSnapshot.cs.meta Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs
git commit -m "feat(sheetx): persist a migration snapshot across the domain reload"
```

---

## Task 7: Depth dropdown in the Collections window

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionsWindow.cs:99-107, 109-135, 137-183`

**Interfaces:**
- Consumes: `SheetXCollectionDepth` (Task 1).
- Produces: `internal static bool SheetXCollectionsWindow.ShowsAutoLoadAsOn(SheetXCollectionDefinition collection)` and `internal static bool SheetXCollectionsWindow.AllowsAutoLoadEdit(SheetXCollectionDefinition collection)` — the two decisions the row drawing makes, extracted so they can be tested without an `IMGUI` pass.

`IMGUI` layout itself has no test seam, but the two booleans behind it do, and they carry the rule that matters: an inline collection displays `Auto Load` as on and non-editable while its stored flag is left untouched. Those are extracted and tested here; the layout is verified by hand in Step 4.

- [ ] **Step 1: Write the failing test**

Create `Assets/RCore.SheetX/Tests/CollectionsWindowDepthTests.cs`:

```csharp
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class CollectionsWindowDepthTests
	{
		[Test]
		public void inline_collection_shows_auto_load_on_and_locked()
		{
			var collection = new SheetXCollectionDefinition
			{
				name = "Player",
				depth = SheetXCollectionDepth.Inline,
				autoLoad = false,
			};

			// The baker resolves an inline group's AutoLoad from Global, so a checkbox reading
			// false while its rows bake anyway would be a lie.
			Assert.That(SheetXCollectionsWindow.ShowsAutoLoadAsOn(collection), Is.True);
			Assert.That(SheetXCollectionsWindow.AllowsAutoLoadEdit(collection), Is.False);

			// The stored value is preserved so switching back restores the developer's choice.
			Assert.That(collection.autoLoad, Is.False);
		}

		[Test]
		public void separate_asset_collection_shows_and_edits_its_own_auto_load()
		{
			var collection = new SheetXCollectionDefinition
			{
				name = "Shop",
				depth = SheetXCollectionDepth.SeparateAsset,
				autoLoad = false,
			};

			Assert.That(SheetXCollectionsWindow.ShowsAutoLoadAsOn(collection), Is.False);
			Assert.That(SheetXCollectionsWindow.AllowsAutoLoadEdit(collection), Is.True);

			collection.autoLoad = true;
			Assert.That(SheetXCollectionsWindow.ShowsAutoLoadAsOn(collection), Is.True);
		}
	}
}
```

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: FAIL with `CS0117: 'SheetXCollectionsWindow' does not contain a definition for 'ShowsAutoLoadAsOn'`.

- [ ] **Step 3: Add the Storage column to the header**

In `DrawCollectionHeader` (`:99-107`), insert between the `Generated Class` and `Auto` labels:

```csharp
			EditorGUILayout.LabelField(new GUIContent("Storage", "Inline: serialized inside GlobalConfigCollection.asset. Separate Asset: its own .asset file, referenced from Global."), EditorStyles.boldLabel, GUILayout.Width(110));
```

- [ ] **Step 2: Draw the dropdown and gate Auto Load**

In `DrawGlobalCollectionRow` (`:109-135`), after the `Generated Class` label, keep the column aligned — Global has no depth:

```csharp
			GUI.enabled = false;
			EditorGUILayout.LabelField("Root", GUILayout.Width(110));
			GUI.enabled = true;
```

In `DrawCustomCollectionRow` (`:137-183`), after the `Generated Class` label:

```csharp
			EditorGUI.BeginChangeCheck();
			collection.depth = (SheetXCollectionDepth)EditorGUILayout.EnumPopup(
				collection.depth, GUILayout.Width(110));
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_settings);
```

Add the two extracted decisions next to it, then replace that method's `Auto Load` toggle block (`:157-162`) so the drawing reads through them:

```csharp
		/// <summary>
		/// Whether the Auto Load checkbox renders as on. An inline group's AutoLoad is resolved from
		/// Global by the baker, so showing its stored false while its rows bake anyway would be a lie.
		/// </summary>
		internal static bool ShowsAutoLoadAsOn(SheetXCollectionDefinition collection)
			=> collection.depth == SheetXCollectionDepth.Inline || collection.autoLoad;

		/// <summary>
		/// Whether the Auto Load checkbox accepts edits. Inline collections follow Global, so the
		/// control is read-only — but the stored value is preserved for a switch back.
		/// </summary>
		internal static bool AllowsAutoLoadEdit(SheetXCollectionDefinition collection)
			=> collection.depth != SheetXCollectionDepth.Inline;
```

```csharp
			bool allowsEdit = AllowsAutoLoadEdit(collection);
			EditorGUI.BeginChangeCheck();
			GUI.enabled = allowsEdit;
			bool shown = EditorGUILayout.Toggle(
				new GUIContent("", allowsEdit
					? "Automatically bake JSON into ScriptableObject asset after export"
					: "Loaded with Global: an inline group is serialized inside GlobalConfigCollection.asset."),
				ShowsAutoLoadAsOn(collection), GUILayout.Width(35));
			GUI.enabled = true;
			if (EditorGUI.EndChangeCheck() && allowsEdit)
			{
				// Only an editable row writes back, so an inline collection keeps the value it had.
				collection.autoLoad = shown;
				EditorUtility.SetDirty(m_settings);
			}
```

- [ ] **Step 4: Verify by hand in the Editor**

Open the SheetX Collections window. Confirm:
1. Global's row shows a disabled `Root` label in the Storage column, no dropdown.
2. A custom collection shows a `Separate Asset` / `Inline` dropdown, defaulting to `Separate Asset`.
3. Switching it to `Inline` greys out `Auto Load` and shows it checked, with the "Loaded with Global" tooltip.
4. Switching back to `Separate Asset` re-enables `Auto Load` at its previous stored value.
5. Nothing regenerates on change — the settings asset is dirtied and nothing else happens until the next export.

- [ ] **Step 5: Run the build and the full suite**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-7.xml`, then parse.
Expected: `total="367" passed="367" failed="0"`

- [ ] **Step 6: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/Collection/SheetXCollectionsWindow.cs Assets/RCore.SheetX/Tests/CollectionsWindowDepthTests.cs Assets/RCore.SheetX/Tests/CollectionsWindowDepthTests.cs.meta
git commit -m "feat(sheetx): add the collection storage dropdown to the Collections window"
```

---

## Task 8: Depth-change confirmation and restore offer

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs`

**Interfaces:**
- Consumes: `DetectDepthChanges` (Task 5), `SheetXMigrationSnapshot` (Task 6).
- Produces: `internal static Func<string, bool> SheetXCollectionExportSession.ConfirmDepthChange` — an injectable prompt, defaulting to `EditorUtility.DisplayDialog`. Tests and headless runs replace it so nothing blocks on a dialog.

- [ ] **Step 1: Write the failing test**

Append to `Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs`:

```csharp
		[Test]
		public void depth_change_prompt_is_injectable_and_names_the_collection()
		{
			var captured = new List<string>();
			var original = SheetXCollectionExportSession.ConfirmDepthChange;
			try
			{
				SheetXCollectionExportSession.ConfirmDepthChange = message =>
				{
					captured.Add(message);
					return false;
				};

				// A headless or test run must never block on EditorUtility.DisplayDialog.
				Assert.That(SheetXCollectionExportSession.ConfirmDepthChange("Player"), Is.False);
				Assert.That(captured, Has.Count.EqualTo(1));
				Assert.That(captured[0], Does.Contain("Player"));
			}
			finally
			{
				SheetXCollectionExportSession.ConfirmDepthChange = original;
			}
		}

		[Test]
		public void default_depth_change_prompt_is_wired_up()
		{
			Assert.That(SheetXCollectionExportSession.ConfirmDepthChange, Is.Not.Null);
		}
```

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: FAIL with `CS0117: 'SheetXCollectionExportSession' does not contain a definition for 'ConfirmDepthChange'`.

- [ ] **Step 3: Add the injectable prompt and wire it into the flush**

In `SheetXCollectionExportSession.cs`, add near the top of the class:

```csharp
		/// <summary>
		/// Asks the developer to confirm one collection's depth change. Injectable so headless and
		/// test runs neither block on a dialog nor depend on one. Receives the message, returns
		/// whether to proceed.
		/// </summary>
		internal static Func<string, bool> ConfirmDepthChange = collectionName =>
			EditorUtility.DisplayDialog(
				"Change collection storage",
				$"'{collectionName}' is changing how it is stored.\n\n"
				+ "Its data will be re-baked from the spreadsheet. Any existing asset is kept, not deleted, "
				+ "so switching back reuses it.\n\n"
				+ "A MonoBehaviour field of this collection's type still compiles after the change but "
				+ "silently becomes an empty copy — find and fix those by hand.",
				"Change", "Cancel");
```

Replace the `depthChanges` block added in Task 6 with the confirming version:

```csharp
			RequiresScriptReload = SourcesChanged(sources);
			var depthChanges = SheetXCollectionSettings.DetectDepthChanges(m_settings);
			if (depthChanges.Count > 0)
			{
				foreach (string changed in depthChanges)
				{
					if (ConfirmDepthChange(changed))
						continue;
					error = $"Export cancelled: '{changed}' storage change was not confirmed.";
					return false;
				}
				// A depth change outlives this call: the bake happens after a domain reload, where
				// the local snapshots below no longer exist. Persist what we are about to replace.
				string codeFolder = SheetXCollectionSettings.NormalizePath(
					m_settings.ResolveCollectionCodeFolder());
				var previousSources = sources.Keys
					.Select(name => codeFolder + "/" + name)
					.Where(File.Exists)
					.ToDictionary(path => path, File.ReadAllText);
				SheetXMigrationSnapshot.Capture(m_settings, depthChanges, previousSources);
			}
			List<FileSnapshot> snapshots;
```

- [ ] **Step 4: Report an incomplete migration honestly**

In `SheetXCollectionBaker.OnScriptsReloaded` (`:122-130`), extend the failure branch so a bake that fails after a depth change points at the restore path rather than claiming the export finished:

```csharp
					if (error.IndexOf("was not found", StringComparison.Ordinal) >= 0)
					{
						Debug.LogError($"SheetX: Pending bake: compilation failed. {error}");
						remaining.Add(entry);
					}
					else if (SheetXMigrationSnapshot.Exists)
					{
						// The generated sources were replaced by a depth change and the bake then
						// failed. Writing the sources was not success, so say so and name the way back.
						Debug.LogError(
							$"{error}\nSheetX: the collection storage change did not complete. "
							+ "Call SheetXMigrationSnapshot.TryRestore to put the previous generated "
							+ "sources back, or fix the error above and export again.");
					}
					else
					{
						Debug.LogError(error);
					}
```

Clear the snapshot once a bake succeeds. In `TryFinishPendingBake`'s success path — the `continue` at `:120` — precede it with:

```csharp
					if (TryFinishPendingBake(
						settings, entry.AutoLoadAfterExport, PendingBindings(entry), out string error))
					{
						SheetXMigrationSnapshot.Clear();
						continue;
					}
```

- [ ] **Step 5: Run the build and the tests**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-8.xml`, then parse.
Expected: `total="369" passed="369" failed="0"`

- [ ] **Step 6: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs Assets/RCore.SheetX/Tests/CollectionDepthChangeTests.cs
git commit -m "feat(sheetx): confirm collection depth changes and report incomplete migrations"
```

---

## Task 9: Document the contract and cut the release

**Files:**
- Modify: `Assets/RCore.SheetX/Document/Document.md`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`
- Modify: `CHANGELOG.md`
- Modify: `Assets/RCore.SheetX/package.json:3`

**Interfaces:**
- Consumes: everything above.
- Produces: `1.9.0`.

This is a MINOR bump under `docs/contributing/SEMVER_POLICY.md`: `SeparateAsset` is the default, so no existing project's generated output or asset layout changes without the developer choosing it.

- [ ] **Step 1: Document the two storage modes**

In `Assets/RCore.SheetX/Document/Document.md`, in the Data Config Collections section, add a subsection covering:
- Each collection chooses `Separate Asset` (default) or `Inline` in the Collections window.
- The read path is the same either way: `global.player.Characters`.
- `Inline` has no asset, so it cannot be dragged into a Prefab field and has no `IsLoaded`; it is loaded exactly when Global is.
- Switching to `Inline` keeps the old asset on disk. It stops being baked, and the Inspector will show "associated script can not be loaded" for it. Switching back reuses that same file and GUID, so references that were left alone resolve again.
- A `[SerializeField]` of the collection's type keeps compiling after a switch to `Inline` and silently becomes an empty inline copy. These must be found and fixed by hand.

- [ ] **Step 2: Bump the package version**

In `Assets/RCore.SheetX/package.json:3`, change `"version": "1.8.0"` to `"version": "1.9.0"`.

- [ ] **Step 3: Write both changelogs**

In `Assets/RCore.SheetX/CHANGELOG.md`, insert between `## [Unreleased]` and `## [1.8.0] - 2026-09-09`:

```markdown
## [1.9.0] - 2026-09-15

### Added

- Per-collection storage mode. Each collection now chooses between `Separate Asset` (its own `.asset`, referenced from Global — the previous and still default behaviour) and `Inline` (serialized inside `GlobalConfigCollection.asset`, no separate file). Game code reads the same path in both modes: `global.player.Characters`.
- A collection changing storage mode now asks for confirmation before its generated source is replaced, and the sources it replaces are saved outside `Assets/` so a bake that fails after the domain reload can be undone.

### Changed

- An `Inline` collection has no asset, so it has no `IsLoaded` and cannot be assigned to a `ScriptableObject` field. It is loaded exactly when Global is, and its `Auto Load` follows Global's.
- Switching a collection to `Inline` leaves its previous `.asset` in place rather than deleting it. The asset stops being baked, but switching back reuses the same file and GUID so existing references resolve again. **A `[SerializeField]` of that collection's type still compiles after the switch and silently becomes an empty inline copy — those fields have to be found and fixed by hand.**
```

In the root `CHANGELOG.md`, add a matching entry as the first bullet under the current `### Added` heading, in the dense single-paragraph house style used by the surrounding SheetX entries, naming `1.9.0`, both modes, the stable read path, and the `[SerializeField]` caveat.

- [ ] **Step 4: Full verification**

Run: `dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly`
Expected: `Build succeeded. 5 Warning(s) 0 Error(s)`

Run the headless suite into `Temp/t-depth-9.xml`, then parse.
Expected: `total="369" passed="369" failed="0"`

Run: `git diff --check`
Expected: no whitespace errors in any `Assets/RCore.SheetX` file.

Confirm the new `.cs` files use tabs and CRLF, and that every new `.cs` has a tracked `.meta`:
```bash
git status --short Assets/RCore.SheetX/
```
Expected: each new `.cs` appears alongside its `.cs.meta`. An untracked `.meta` means every fresh clone regenerates a different GUID.

- [ ] **Step 5: Commit (only if the user asked for commits)**

```bash
git add Assets/RCore.SheetX/Document/Document.md Assets/RCore.SheetX/CHANGELOG.md CHANGELOG.md Assets/RCore.SheetX/package.json
git commit -m "chore(sheetx): cut 1.9.0"
```

---

## Self-Review

**Spec coverage.** Every spec section maps to a task: model → 1; generated code and the `using System;` / one-file-per-collection rules → 2; the six baker sites, the `:642` gate and the nested write → 3; inline `AutoLoad`, Configuration ordering and asset reuse → 4; depth-change detection → 5; the durable snapshot → 6; UI → 7; confirmation and honest reporting → 8; docs and release → 9. Spec test cases 1-12 map to Task 2 (1), Task 3 (2, 3, 8, 10), Task 4 (9, 11, 12), Task 5 (6), Task 6 (7). Spec case 4 (a mid-bake failure under `Inline` restoring Global) is covered by the existing `TestBeforeSave` rollback tests in `CollectionBakeTests`, which exercise the same `Rollback` path that now also carries inline writes — Global is already in `snapshots` at `:484`, so no new mechanism is involved. Spec case 5 (the leftover asset) is folded into Task 4's round-trip test, which is the stronger assertion.

**Placeholder scan.** No `TBD`, no "add error handling", no "similar to Task N". Every code step carries the code. Task 7 is the one task with no automated test, and it says so explicitly with a hand-verification checklist rather than a vague "test in the Editor".

**Type consistency.** `SheetXCollectionDepth` is spelled identically in every task. `DepthOf(SheetXSettings, string)` is introduced in Task 2 and called in Tasks 3 and 5. `DetectDepthChanges(SheetXSettings)` is introduced in Task 5 and called in Tasks 6 and 8. `ApplyRows(object, Collection, string, out string)` is defined once in Task 3 and its call site updated in the same task. `SheetXMigrationSnapshot.Capture/TryRestore/Exists/Clear` are defined in Task 6 and used in Task 8. `Collection.Depth` is added in Task 3 and read in Tasks 3 and 4.

**Known risk to watch during execution.** Task 4 Step 2 predicts that two of its three tests already pass. If `configuration_and_inline_group_do_not_clobber_each_other` also passes unchanged, Task 4 adds no production code — that is a legitimate outcome for a characterization task, and Step 2 says to record it rather than manufacture a change.
