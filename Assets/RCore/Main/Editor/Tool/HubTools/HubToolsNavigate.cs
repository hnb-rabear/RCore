using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class ScenesNavigatorTool : RCoreHubTool
	{
		public override string Name => "Scenes Navigator";
		public override string Category => "Navigate";
		public override string Description => "List Scenes from Build Settings and the entire project for quick access.";
		public override bool IsQuickAction => false;

		private List<string> m_scenes;
		private REditorPrefString m_search;

		public override void Initialize()
		{
			m_search = new REditorPrefString(typeof(ScenesNavigatorTool).FullName);
			m_scenes = GetAllScenesInProject();
		}

		public override void DrawFocusMode()
		{
			var buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
			var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Scenes in Build", labelStyle);
			var scenes = EditorBuildSettings.scenes;
			foreach (var scene in scenes)
			{
				string sceneName = Path.GetFileNameWithoutExtension(scene.path);
				if (GUILayout.Button(sceneName, buttonStyle))
					OpenScene(scene.path);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("All Scenes in project", labelStyle);
			m_search.Value = EditorHelper.TextField(m_search.Value, "Search", 60);
			GUILayout.Space(5);
			for (int i = 0; i < m_scenes.Count; i++)
			{
				string scene = m_scenes[i];
				if ((m_search.Value == "" || scene.ToLower().Contains(m_search.Value.ToLower())) && GUILayout.Button($"{i}\t {scene}", buttonStyle))
					OpenScene(scene);
			}
			EditorGUILayout.EndVertical();
		}

		private void OpenScene(string scenePath)
		{
			if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
		}

		private List<string> GetAllScenesInProject()
		{
			var scenes = new List<string>();
			string[] guids = AssetDatabase.FindAssets("t:Scene");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				scenes.Add(path);
			}
			return scenes;
		}
	}

	public class AssetShortcutsTool : RCoreHubTool
	{
		public override string Name => "Asset Shortcuts";
		public override string Category => "Navigate";
		public override string Description => "Create categories, drag & drop to manage and quickly access frequently used assets.";
		public override bool IsQuickAction => false;

		[System.Serializable]
		private class CategoryData
		{
			public string name;
			public List<string> guids = new();
		}

		private List<CategoryData> m_categories;
		private string m_newCategoryName = "";
		private int m_editingCategory = -1;
		private const string FILE_PATH = "Assets/Editor/AssetShortcuts.json";

		public override void Initialize()
		{
			if (File.Exists(FILE_PATH))
			{
				string jsonData = File.ReadAllText(FILE_PATH);
				m_categories = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CategoryData>>(jsonData);
			}
			m_categories ??= new List<CategoryData>();
		}

		private void SaveData()
		{
			string dir = Path.GetDirectoryName(FILE_PATH);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(m_categories, Newtonsoft.Json.Formatting.Indented);
			File.WriteAllText(FILE_PATH, jsonData);
		}

		public override void DrawFocusMode()
		{
			GUILayout.Label("Categories", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			m_newCategoryName = EditorGUILayout.TextField(m_newCategoryName);
			if (GUILayout.Button("Add Category", GUILayout.Width(100)) && !string.IsNullOrEmpty(m_newCategoryName))
			{
				m_categories.Add(new CategoryData() { name = m_newCategoryName });
				m_newCategoryName = "";
				SaveData();
			}
			GUILayout.EndHorizontal();

			for (int i = 0; i < m_categories.Count; i++)
			{
				GUILayout.BeginVertical("box");
				var category = m_categories[i];
				GUILayout.BeginHorizontal();
				
				if (m_editingCategory == i)
				{
					var newName = EditorGUILayout.TextField(category.name);
					if (newName != category.name && !string.IsNullOrEmpty(newName))
						category.name = newName;
				}
				else
					GUILayout.Label(category.name, EditorStyles.boldLabel);

				if (m_editingCategory != i)
				{
					if (EditorHelper.Button("Edit", 40))
						m_editingCategory = i;
				}
				else
				{
					if (EditorHelper.Button("Save", 50))
					{
						m_editingCategory = -1;
						SaveData();
					}
				}

				if (EditorHelper.ButtonColor("Delete", Color.red, 60))
				{
					if (EditorUtility.DisplayDialog("Delete", "Delete this category?", "Yes", "No"))
					{
						m_categories.Remove(category);
						SaveData();
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
						break;
					}
				}
				GUILayout.EndHorizontal();

				// Drop Area
				var dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
				GUI.Box(dropArea, "Drag & Drop Assets Here");

				if (dropArea.Contains(Event.current.mousePosition))
				{
					if (Event.current.type == EventType.DragUpdated)
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
						Event.current.Use();
					}
					else if (Event.current.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();
						foreach (var obj in DragAndDrop.objectReferences)
						{
							var path = AssetDatabase.GetAssetPath(obj);
							var guid = AssetDatabase.AssetPathToGUID(path);
							if (!string.IsNullOrEmpty(guid) && !category.guids.Contains(guid))
							{
								category.guids.Add(guid);
								SaveData();
							}
						}
						Event.current.Use();
					}
				}

				for (int ii = 0; ii < category.guids.Count; ii++)
				{
					string guid = category.guids[ii];
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var assetObj = AssetDatabase.LoadAssetAtPath<Object>(path);
					if (assetObj)
					{
						GUILayout.BeginHorizontal();
						EditorGUILayout.ObjectField(assetObj, typeof(Object), false);
						if (assetObj is UnityEditor.SceneAsset && EditorHelper.Button("Open", 50))
						{
							if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
								UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
						}
						else if (PrefabUtility.GetPrefabAssetType(assetObj) == PrefabAssetType.Regular && EditorHelper.Button("Open", 50))
							AssetDatabase.OpenAsset(assetObj);

						if (EditorHelper.ButtonColor("X", Color.red, 30))
						{
							category.guids.Remove(guid);
							SaveData();
							ii--;
						}
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndVertical();
				GUILayout.Space(5);
			}
		}
	}

	public class FindByGuidTool : RCoreHubTool
	{
		public override string Name => "Find By GUID";
		public override string Category => "Navigate";
		public override string Description => "Enter a GUID string to instantly locate the corresponding Asset file in the project.";
		public override bool IsQuickAction => true;

		private string m_Guid;
		private Object m_FoundObject;

		public override void DrawCard()
		{
			m_Guid = EditorHelper.TextField(m_Guid, "GUID");
			EditorHelper.ObjectField<Object>(m_FoundObject, "Object");
			
			if (!string.IsNullOrEmpty(m_Guid) && EditorHelper.ButtonColor("Find Object", Color.cyan))
			{
				string path = AssetDatabase.GUIDToAssetPath(m_Guid);
				if (!string.IsNullOrEmpty(path))
					m_FoundObject = AssetDatabase.LoadAssetAtPath<Object>(path);
			}
		}
	}

	public class ReplaceGameObjectsTool : RCoreHubTool
	{
		public override string Name => "Replace GameObjects";
		public override string Category => "Navigate";
		public override string Description => "Batch-replace selected GameObjects with a random Prefab from the provided array.";
		public override bool IsQuickAction => false;

		private List<GameObject> m_ReplaceableGameObjects = new List<GameObject>();
		private List<GameObject> m_Prefabs = new List<GameObject>();

		public override void DrawFocusMode()
		{
			if (m_ReplaceableGameObjects == null || m_ReplaceableGameObjects.Count == 0)
				EditorGUILayout.HelpBox("Use Drag & Drop to populate the list of Replaceable Objects from Scene", MessageType.Info);

			EditorHelper.ListObjects("Replaceable Objects (Targets)", ref m_ReplaceableGameObjects, null, false);
			EditorHelper.ListObjects("Prefabs (Sources)", ref m_Prefabs, null, false);

			GUILayout.Space(10);
			if (GUILayout.Button("Execute Replace", GUILayout.Height(40)))
			{
				if (m_Prefabs.Count > 0 && m_ReplaceableGameObjects.Count > 0)
					EditorHelper.ReplaceGameObjectsInScene(ref m_ReplaceableGameObjects, m_Prefabs);
				else
					EditorUtility.DisplayDialog("Error", "Need both targets and prefabs", "OK");
			}
		}
	}
}
