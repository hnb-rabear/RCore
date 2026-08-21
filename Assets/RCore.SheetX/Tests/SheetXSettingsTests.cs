/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.IO;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	/// <summary>
	/// Guards the two failure modes that made SheetX settings disappear on a fresh clone:
	/// the asset being resolved out of the gitignored package cache, and in-memory edits
	/// never reaching disk.
	/// </summary>
	public class SheetXSettingsTests
	{
		[Test]
		public void init_resolves_asset_under_assets_not_packages()
		{
			var settings = SheetXSettings.Init();
			Assert.IsNotNull(settings, "Init() must always return a settings instance.");

			string path = AssetDatabase.GetAssetPath(settings);
			Assert.IsNotEmpty(path, "Settings must be a persisted asset, not an in-memory instance.");
			Assert.IsTrue(path.StartsWith("Assets/"),
				$"Settings resolved to '{path}'. Anything under Packages/ lives in Library/PackageCache, " +
				"which is gitignored and rebuilt on every UPM re-resolve, so edits there are lost.");
		}

		[Test]
		public void init_returns_same_asset_on_repeated_calls()
		{
			// Each editor tab calls Init() independently; if they resolved to different assets,
			// whichever saved last would silently overwrite the others.
			Assert.AreSame(SheetXSettings.Init(), SheetXSettings.Init());
		}

		[Test]
		public void save_persists_sheet_paths_across_reload()
		{
			var settings = SheetXSettings.Init();
			string original = settings.jsonOutputFolder;
			const string marker = "Assets/__sheetx_save_probe__";

			try
			{
				settings.jsonOutputFolder = marker;
				settings.SaveToDisk();

				// Read the raw file rather than the cached object — LoadAssetAtPath would hand back
				// the same in-memory instance and pass even if nothing was written.
				string diskText = File.ReadAllText(AssetDatabase.GetAssetPath(settings));

				StringAssert.Contains(marker, diskText,
					"SaveToDisk() did not flush to disk — edits would vanish on the next domain reload.");
			}
			finally
			{
				settings.jsonOutputFolder = original;
				settings.SaveToDisk();
			}
		}

		[Test]
		public void token_cache_lives_outside_the_asset_pipeline()
		{
			string dir = SheetXHelper.GetTokenStoreDirectory().Replace('\\', '/');
			string assets = Application.dataPath.Replace('\\', '/').TrimEnd('/');

			Assert.IsFalse(dir.StartsWith(assets + "/", System.StringComparison.OrdinalIgnoreCase),
				$"Token cache resolved to '{dir}', inside Assets. A cached OAuth token there is committed " +
				"by anyone whose .gitignore is stale, and ships with any zip of Assets.");
			StringAssert.EndsWith("/Library/SheetX", dir);
			Assert.IsTrue(Directory.Exists(dir), "GetTokenStoreDirectory() must create the directory it returns.");
		}

		[Test]
		public void credential_pref_key_does_not_depend_on_bundle_identifier()
		{
			var settings = SheetXSettings.Init();
			string original = settings.ObfGoogleClientId;
			const string probe = "sheetx-probe-client-id";

			try
			{
				settings.ObfGoogleClientId = probe;

				// The bundle identifier is a shipping setting that changes per build flavor. If it were
				// part of the key, a single Player Settings edit would orphan the stored credential.
				string legacyKey = $"{Application.identifier}.SheetX.GoogleClientId";
				Assert.IsFalse(EditorPrefs.HasKey(legacyKey),
					$"Credential was written under '{legacyKey}', which is keyed by the bundle identifier.");
				Assert.AreEqual(probe, settings.ObfGoogleClientId);
			}
			finally
			{
				settings.ObfGoogleClientId = original;
			}
		}

		[Test]
		public void credential_migrates_from_legacy_bundle_identifier_key()
		{
			var settings = SheetXSettings.Init();
			string original = settings.ObfGoogleClientSecret;
			string legacyKey = $"{Application.identifier}.SheetX.GoogleClientSecret";
			const string legacyValue = "sheetx-legacy-secret";

			try
			{
				// Simulate an install that authenticated before the key changed: clear the new-key value
				// and seed only the old one.
				settings.ObfGoogleClientSecret = "";
				EditorPrefs.SetString(legacyKey, legacyValue);

				Assert.AreEqual(legacyValue, settings.ObfGoogleClientSecret,
					"An existing install must not silently lose its credential when the key scheme changes.");
				Assert.IsFalse(EditorPrefs.HasKey(legacyKey),
					"The legacy key must be removed once its value has been copied forward.");
				Assert.AreEqual(legacyValue, settings.ObfGoogleClientSecret,
					"The migrated value must persist under the new key, not just be returned once.");
			}
			finally
			{
				EditorPrefs.DeleteKey(legacyKey);
				settings.ObfGoogleClientSecret = original;
			}
		}

		[Test]
		public void no_legacy_flavor_defines_exist_in_editor_scripts()
		{
			var editorScripts = Directory.GetFiles("Assets/RCore.SheetX/Editor", "*.cs", SearchOption.AllDirectories);
			var legacyDefines = new[] { "SX_LOCALIZATION", "SX_LITE", "SX_NO_LOCALIZATION" };

			foreach (var file in editorScripts)
			{
				string content = File.ReadAllText(file);
				foreach (var define in legacyDefines)
				{
					Assert.IsFalse(content.Contains(define),
						$"File '{file}' still contains legacy define '{define}'. SheetX should be single-flavor.");
				}
			}
		}
	}
}
