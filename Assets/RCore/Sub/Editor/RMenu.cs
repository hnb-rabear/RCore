using System.IO;
using RCore.Editor.Tool;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace RCore.Editor
{
	public static partial class RMenu
	{
		public const int GROUP_6 = 100;

		[MenuItem("RCore/Tools/Archive/.env Editor", priority = GROUP_6 + 11)]
		public static void OpenEnvEditor()
		{
			ENVWindow.ShowWindow();
		}

		[MenuItem("RCore/Tools/Archive/Builder", priority = GROUP_6 + 12)]
		public static void OpenBuilder()
		{
			BuilderWindow.ShowWindow();
		}

		[MenuItem("RCore/Tools/Archive/Swap Sprite", priority = GROUP_6 + 13)]
		public static void OpenSwapSpriteWindow()
		{
			SwapSpriteWindow.ShowWindow();
		}

		[MenuItem("Assets/RCore/Export Selected Folder to Unity Package")]
		public static void ExportSelectedFolder()
		{
			// Get the selected folder
			UnityEngine.Object selectedObject = Selection.activeObject;
			string path = AssetDatabase.GetAssetPath(selectedObject);

			// Ensure that the selected object is a folder
			if (!AssetDatabase.IsValidFolder(path))
			{
				Debug.LogError("Please select a valid folder to export.");
				return;
			}
			var directoryPath = Path.GetDirectoryName(path)?.Replace("Assets/", "");
			// Define the path to export the package
			string packagePath = EditorUtility.SaveFilePanel("Export Unity Package", directoryPath, selectedObject.name + ".unitypackage", "unitypackage");

			if (string.IsNullOrEmpty(packagePath))
			{
				Debug.Log("Export cancelled.");
				return;
			}

			// Export the selected folder as a package
			AssetDatabase.ExportPackage(path, packagePath, ExportPackageOptions.Recurse);
			Debug.Log("Package exported successfully to: " + packagePath);
		}
	}
}