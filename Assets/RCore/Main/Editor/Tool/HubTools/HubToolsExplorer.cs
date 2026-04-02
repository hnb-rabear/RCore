using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;

namespace RCore.Editor.Tool
{
	public class HubExplorerTool : RCoreHubTool
	{
		public override string Name => "Project Explorer";
		public override string Category => "Navigate";
		public override string Description => "Quick access to important project and system folders.";
		public override bool IsQuickAction => true;

		public override void DrawCard()
		{
			if (GUILayout.Button("DataPath Folder"))
			{
				string path = Application.dataPath;
				var psi = new ProcessStartInfo(path);
				Process.Start(psi);
			}

			if (GUILayout.Button("StreamingAssets Folder"))
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

			if (GUILayout.Button("PersistentData Folder"))
			{
				string path = Application.persistentDataPath;
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
				var psi = new ProcessStartInfo(path);
				Process.Start(psi);
			}

			if (GUILayout.Button("UnityEditor Folder"))
			{
				string path = EditorApplication.applicationPath.Substring(0, EditorApplication.applicationPath.LastIndexOf("/"));
				var psi = new ProcessStartInfo(path);
				Process.Start(psi);
			}

		}
	}

	public class HubEditorIconDictionaryTool : RCoreHubTool
	{
		public override string Name => "Editor Icon Dictionary";
		public override string Category => "Navigate";
		public override string Description => "Browse and preview built-in Unity Editor icons.";
		public override bool IsQuickAction => true;

		public override void DrawCard()
		{
			if (GUILayout.Button("Open Editor Icon Dictionary", GUILayout.Height(30)))
			{
				EditorIconsWindow.ShowWindow();
			}
		}
	}
}
