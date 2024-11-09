/***
 * Author RaBear - HNB - 2019
 **/

using RCore.Editor.Tool;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public static partial class RMenu
	{
		public const int GROUP_1 = 0;
		public const int GROUP_2 = 20;
		public const int GROUP_3 = 40;
		public const int GROUP_4 = 60;
		public const int GROUP_5 = 80;
		public const int GROUP_6 = 100;

		private const string ALT = "&";
		private const string SHIFT = "#";
		private const string CTRL = "%";

		[MenuItem("RCore/Configuration %_&_j", priority = GROUP_1 + 1)]
		private static void OpenEnvSetting()
		{
			Selection.activeObject = Configuration.Instance;
		}
		
		//==========================================================
		
		[MenuItem("RCore/Asset Database/Save Assets " + SHIFT + "_1", priority = GROUP_2 + 1)]
		private static void SaveAssets()
		{
			var objs = Selection.objects;
			if (objs != null)
				foreach (var obj in objs)
					EditorUtility.SetDirty(obj);

			AssetDatabase.SaveAssets();
		}

		[MenuItem("RCore/Asset Database/Refresh Prefabs in folder", priority = GROUP_2 +  2)]
		private static void RefreshPrefabs()
		{
			RefreshAssets("t:GameObject");
		}

		[MenuItem("RCore/Asset Database/Refresh ScriptableObjects in folder", priority = GROUP_2 +  3)]
		private static void RefreshScriptableObjects()
		{
			RefreshAssets("t:ScriptableObject");
		}

		[MenuItem("RCore/Asset Database/Refresh Assets in folder", priority = GROUP_2 + 4)]
		private static void RefreshAll()
		{
			RefreshAssets("t:GameObject t:ScriptableObject");
		}

		private static void RefreshAssets(string filter)
		{
			string folderPath = EditorHelper.OpenFolderPanel();
			folderPath = EditorHelper.FormatPathToUnityPath(folderPath);
			var assetGUIDs = AssetDatabase.FindAssets(filter, new[] { folderPath });
			foreach (string guid in assetGUIDs)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (asset != null)
					EditorUtility.SetDirty(asset);
			}
			AssetDatabase.SaveAssets();
		}

		//==========================================================

		[MenuItem("RCore/Group Scene Objects " + ALT + "_F1", priority = GROUP_3 + 1)]
		private static void GroupSceneObjects()
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

		[MenuItem("RCore/Ungroup Scene Objects " + ALT + "_F2", priority = GROUP_3 + 2)]
		private static void UngroupSceneObjects()
		{
			var objs = Selection.gameObjects;
			if (objs.Length > 1)
			{
				for (int i = 0; i < objs.Length; i++)
					objs[i].transform.SetParent(null);
			}
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
		
		[MenuItem("RCore/Tools/Tools Collection", priority = GROUP_6 + 1)]
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
		
		//==============================================
		
		[MenuItem("GameObject/RCore/UI/Perfect Image pixels per unit multiplier (W)")]
		public static void PerfectRatioImagesByWidth()
		{
			RUtil.PerfectRatioImagesByWidth(Selection.gameObjects);
		}

		[MenuItem("GameObject/RCore/UI/Perfect Image pixels per unit multiplier (H)")]
		public static void PerfectRatioImagesByHeight()
		{
			RUtil.PerfectRatioImagesByHeight(Selection.gameObjects);
		}

		[MenuItem("GameObject/RCore/UI/Replace Text By TextMeshProUGUI")]
		public static void ReplaceTextsByTextTMP()
		{
			EditorHelper.ReplaceTextsByTextTMP(Selection.gameObjects);
		}

		[MenuItem("GameObject/RCore/Reorder sorting of SpriteRenderers")]
		public static void ReorderSortingOfSpriteRenderers()
		{
			foreach (var target in Selection.gameObjects)
			{
				ComponentHelper.ReorderSortingOfSpriteRenderers(target.GetComponentsInChildren<SpriteRenderer>(true));
				EditorUtility.SetDirty(target);
			}
		}
		
		//==============================================

		[MenuItem("GameObject/RCore/Image/Set perfect ratio")]
		public static void SetImagesPerfectRatio()
		{
			foreach (var target in Selection.gameObjects)
			{
				var images = target.GetComponentsInChildren<UnityEngine.UI.Image>(true);
				foreach (var image in images)
					image.PerfectRatio();
			}
		}
	}
}