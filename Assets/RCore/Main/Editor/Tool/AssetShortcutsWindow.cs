using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
	/// <summary>
	/// Editor window to manage shortcuts to frequently used assets.
	/// </summary>
	public class AssetShortcutsWindow : EditorWindow
	{
		[Serializable]
		private class Category
		{
			public string name;
			public List<string> guids = new();
		}

		private List<Category> m_categories;
		private Vector2 m_scrollPos;
		private string m_newCategoryName = "";
		private int m_editingCategory = -1;

		private const string FILE_PATH = "Assets/Editor/AssetShortcuts.json";

		public static void ShowWindow()
		{
			GetWindow<AssetShortcutsWindow>("Asset Shortcuts");
		}

		private void OnEnable()
		{
			LoadData();
		}

		private void OnDisable()
		{
			SaveData();
		}

		private void LoadData()
		{
			if (File.Exists(FILE_PATH))
			{
				string jsonData = File.ReadAllText(FILE_PATH);
				m_categories = JsonConvert.DeserializeObject<List<Category>>(jsonData);
			}
			m_categories ??= new List<Category>();
		}

		private void SaveData()
		{
			string jsonData = JsonConvert.SerializeObject(m_categories, Formatting.Indented);
			File.WriteAllText(FILE_PATH, jsonData);
		}

		private void OnGUI()
		{
			GUILayout.Label("Categories", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			m_newCategoryName = EditorGUILayout.TextField(m_newCategoryName);
			if (GUILayout.Button("Add Category") && !string.IsNullOrEmpty(m_newCategoryName))
			{
				m_categories.Add(new Category()
				{
					name = m_newCategoryName,
				});
				m_newCategoryName = "";
				SaveData();
			}
			GUILayout.EndHorizontal();

			m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

			for (int i = 0; i < m_categories.Count; i++)
			{
				GUILayout.BeginVertical("box");
				
				var category = m_categories[i];
				GUILayout.BeginHorizontal();
				if (m_editingCategory == i)
				{
					var newName = EditorGUILayout.TextField(category.name);
					if (newName != category.name && !string.IsNullOrEmpty(newName)) // Edit Category Name
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
					if (EditorHelper.Button("Stop Edit", 60))
						m_editingCategory = -1;
				}
				if (EditorHelper.ButtonColor("Delete", Color.red, 70))
				{
					m_categories.Remove(category);
					SaveData();
					break;
				}
				GUILayout.EndHorizontal();

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

				//TODO: allow list categories can be reorder
				for (int ii = 0; ii < category.guids.Count; ii++)
				{
					string guid = category.guids[ii];
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var assetObj = AssetDatabase.LoadAssetAtPath<Object>(path);
					if (assetObj)
					{
						GUILayout.BeginHorizontal();
						EditorGUILayout.ObjectField(assetObj, typeof(Object), false);
						if (assetObj is SceneAsset && EditorHelper.Button("Open", 50))
						{
							if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
								EditorSceneManager.OpenScene(path);
						}
						if (PrefabUtility.GetPrefabAssetType(assetObj) == PrefabAssetType.Regular && EditorHelper.Button("Open", 50))
							AssetDatabase.OpenAsset(assetObj);
						if (EditorHelper.ButtonColor("Delete", Color.red, 70))
						{
							category.guids.Remove(guid);
							ii--;
						}
						GUILayout.EndHorizontal();
					}
				}
				EditorHelper.Separator();
				GUILayout.EndVertical();
			}
			EditorGUILayout.EndScrollView();
		}
	}
}