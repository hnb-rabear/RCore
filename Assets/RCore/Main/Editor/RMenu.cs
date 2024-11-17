/***
 * Author HNB-RaBear - 2019
 **/

using RCore.Audio;
using RCore.Editor.Tool;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public static class RMenu
	{
		// CTRL + ALT + j => Configuration
		// CTRL + ALT + k => Scene Opener
		// CTRL + ALT + n => JObject Database
		// CTRL + ALT + m => KeyValue Database
		// CTRL + ALT + / => Tools Collection
		// SHIFT + 1 => Save Assets

		public const int GROUP_1 = 0;
		public const int GROUP_2 = 20;
		public const int GROUP_3 = 40;
		public const int GROUP_4 = 60;
		public const int GROUP_5 = 80;
		public const int GROUP_6 = 100;

		private const string ALT = "&";
		private const string SHIFT = "#";
		private const string CTRL = "%";

		[MenuItem("RCore/Configuration " + CTRL + "_" + ALT + "_j", priority = GROUP_1 + 1)]
		private static void OpenEnvSetting()
		{
			Selection.activeObject = Configuration.Instance;
		}

		[MenuItem("RCore/Copy name of Build", priority = GROUP_1 + 2)]
		private static void CopyBuildName()
		{
			string buildName = EditorHelper.GetBuildName();
			GUIUtility.systemCopyBuffer = buildName;
			UnityEngine.Debug.Log("Copied name of Build: " + buildName);
		}

		//==========================================================

		[MenuItem("Assets/RCore/Save Assets")]
		[MenuItem("RCore/Asset Database/Save Assets " + SHIFT + "_1", priority = GROUP_2 + 1)]
		private static void SaveAssets()
		{
			var objs = Selection.objects;
			if (objs != null)
				foreach (var obj in objs)
					EditorUtility.SetDirty(obj);
		
			AssetDatabase.SaveAssets();
		}

		[MenuItem("RCore/Quick Scene Opener " + CTRL + "_" + ALT + "_k", priority = GROUP_2 + 5)]
		private static void OpenQuickSceneWindow()
		{
			QuickSceneWindow.ShowWindow();
		}

		//==========================================================

		[MenuItem("RCore/Clear PlayerPrefs", priority = GROUP_4 + 1)]
		private static void ClearPlayerPrefs()
		{
			if (EditorHelper.ConfirmPopup("Clear PlayerPrefs"))
				PlayerPrefs.DeleteAll();
		}

		//==========================================================

		[MenuItem("RCore/Explorer/DataPath Folder", false, GROUP_5 + 81)]
		private static void OpenDataPathFolder()
		{
			string path = Application.dataPath;
			var psi = new ProcessStartInfo(path);
			Process.Start(psi);
		}

		[MenuItem("RCore/Explorer/StreamingAssets Folder", false, GROUP_5 + 82)]
		private static void OpenStreamingAssetsFolder()
		{
			string path = Application.streamingAssetsPath;
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
				AssetDatabase.Refresh();
			}
			var psi = new ProcessStartInfo(path);
			Process.Start(psi);
		}

		[MenuItem("RCore/Explorer/PersistentData Folder", false, GROUP_5 + 83)]
		private static void OpenPersistentDataFolder()
		{
			string path = Application.persistentDataPath;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			var psi = new ProcessStartInfo(path);
			Process.Start(psi);
		}

		[MenuItem("RCore/Explorer/UnityEditor Folder", false, GROUP_5 + 84)]
		private static void OpenUnityEditorFolder()
		{
			string path = EditorApplication.applicationPath.Substring(0, EditorApplication.applicationPath.LastIndexOf("/"));
			var psi = new ProcessStartInfo(path);
			Process.Start(psi);
		}

		//==========================================================

		[MenuItem("RCore/Tools/Tools Collection " + CTRL + "_" + ALT + "_/", priority = GROUP_6 + 1)]
		private static void OpenToolsCollectionWindow()
		{
			ToolsCollectionWindow.ShowWindow();
		}

		[MenuItem("RCore/Tools/Screenshot Taker", priority = GROUP_6 + 2)]
		public static void OpenScreenshotTaker()
		{
			ScreenshotTaker.ShowWindow();
		}

		[MenuItem("RCore/Tools/Find Component Reference", priority = GROUP_6 + 3)]
		public static void OpenFindComponentReferenceWindow()
		{
			FindComponentReferenceWindow.ShowWindow();
		}

		[MenuItem("RCore/Tools/Find Objects", priority = GROUP_6 + 4)]
		public static void OpenObjectsFinderWindow()
		{
			ObjectsFinderWindow.ShowWindow();
		}

		[MenuItem("RCore/Tools/Search And Replace Asset Toolkit", priority = GROUP_6 + 5)]
		public static void OpenSearchAndReplaceAssetWindow()
		{
			SearchAndReplaceAssetWindow.ShowWindow();
		}

		// [MenuItem("RCore/Tools/SheetX/Excel Sheets Exporter", priority = GROUP_6 + 6)]
		// public static void OpenExcelSheetsExporter()
		// {
		// 	ExcelSheetXWindow.ShowWindow();
		// }
		//
		// [MenuItem("RCore/Tools/SheetX/Google Sheets Exporter", priority = GROUP_6 + 6)]
		// public static void OpenGoogleSheetsExporter()
		// {
		// 	GoogleSheetXWindow.ShowWindow();
		// }
		//
		// [MenuItem("RCore/Tools/SheetX/Settings", priority = GROUP_6 + 6)]
		// public static void OpenSheetXSettingsWindow()
		// {
		// 	SheetXSettingsWindow.ShowWindow();
		// }

		//==============================================

		[MenuItem("GameObject/RCore/Group GameObjects", priority = GROUP_1 + 1)]
		private static void GroupGameObjects()
		{
			var objs = Selection.gameObjects;
			if (objs.Length > 1)
			{
				var group = new GameObject();
				for (int i = 0; i < objs.Length; i++)
				{
					objs[i].transform.SetParent(group.transform);
				}
				Selection.activeObject = group;
			}
		}

		[MenuItem("GameObject/RCore/Ungroup GameObjects", priority = GROUP_1 + 2)]
		private static void UngroupGameObjects()
		{
			var objs = Selection.gameObjects;
			if (objs.Length > 1)
			{
				for (int i = 0; i < objs.Length; i++)
					objs[i].transform.SetParent(null);
			}
		}

		//==============================================

		[MenuItem("GameObject/RCore/Module/Create AudioManager", priority = GROUP_2 + 1)]
		public static void AddAudioManager()
		{
			var audioManager = new GameObject("AudioManager");
			audioManager.AddComponent<AudioManager>();
		}

		//==============================================

		[MenuItem("GameObject/RCore/UI/Perfect Image pixels per unit multiplier (W)", priority = GROUP_3 + 1)]
		public static void PerfectRatioImagesByWidth()
		{
			RUtil.PerfectRatioImagesByWidth(Selection.gameObjects);
		}

		[MenuItem("GameObject/RCore/UI/Perfect Image pixels per unit multiplier (H)", priority = GROUP_3 + 2)]
		public static void PerfectRatioImagesByHeight()
		{
			RUtil.PerfectRatioImagesByHeight(Selection.gameObjects);
		}

		[MenuItem("GameObject/RCore/UI/Perfect Image Size", priority = GROUP_3 + 3)]
		public static void SetImagesPerfectRatio()
		{
			foreach (var target in Selection.gameObjects)
			{
				var images = target.GetComponentsInChildren<UnityEngine.UI.Image>(true);
				foreach (var image in images)
					image.PerfectRatio();
			}
		}

		[MenuItem("GameObject/RCore/UI/Replace Text By TextMeshProUGUI")]
		public static void ReplaceTextsByTextTMP()
		{
			EditorHelper.ReplaceTextsByTextTMP(Selection.gameObjects);
		}

		[MenuItem("GameObject/RCore/Reorder SpriteRenderers")]
		public static void ReorderSortingOfSpriteRenderers()
		{
			foreach (var target in Selection.gameObjects)
			{
				ComponentHelper.ReorderSortingOfSpriteRenderers(target.GetComponentsInChildren<SpriteRenderer>(true));
				EditorUtility.SetDirty(target);
			}
		}
		
		//==============================================
		
		[MenuItem("Assets/RCore/Refresh Assets in folder")]
		private static void RefreshAssetsInSelectedFolder()
		{
			EditorHelper.RefreshAssetsInSelectedFolder("t:GameObject t:ScriptableObject");
		}

		[MenuItem("Assets/RCore/Export Selected Folders to Unity Package")]
		private static void ExportSelectedFoldersToUnityPackage()
		{
			EditorHelper.ExportSelectedFoldersToUnityPackage();
		}
	}
}