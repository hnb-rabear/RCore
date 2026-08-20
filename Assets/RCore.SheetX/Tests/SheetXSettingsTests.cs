/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.IO;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEditor;

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
	}
}
