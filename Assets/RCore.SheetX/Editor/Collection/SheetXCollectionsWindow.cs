/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Editor utility window for managing Global and custom feature collections.
	/// </summary>
	public class SheetXCollectionsWindow : EditorWindow
	{
		private SheetXSettings m_settings;
		private Vector2 m_scrollPosition;

		/// <summary>
		/// Opens the Collections manager window.
		/// </summary>
		public static void ShowWindow(SheetXSettings settings)
		{
			var window = CreateInstance<SheetXCollectionsWindow>();
			window.titleContent = new GUIContent("Manage Collections");
			window.m_settings = settings ?? SheetXSettings.Init();
			window.minSize = new Vector2(500, 300);
			window.ShowUtility();
		}

		private void OnEnable()
		{
			m_settings ??= SheetXSettings.Init();
		}

		private void OnGUI()
		{
			if (m_settings == null)
				m_settings = SheetXSettings.Init();

			SheetXCollectionSettings.EnsureGlobal(m_settings);

			EditorGUI.BeginChangeCheck();
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);

			DrawToolbar();
			EditorGUILayout.Space(6);
			DrawCollectionsSection();

			GUILayout.EndScrollView();

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_settings);
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent("Load All Collections", "Bake JSON data into all collection ScriptableObject assets now"), GUILayout.Height(24)))
				LoadData(autoLoadOnly: false);
			if (GUILayout.Button(new GUIContent("Add Collection", "Create a new custom feature collection (e.g. Shop, Quest)"), GUILayout.Height(24), GUILayout.Width(140)))
			{
				string name = "Collection";
				int suffix = 2;
				while (m_settings.collections.Exists(
					collection => string.Equals(collection.name, name, StringComparison.Ordinal)))
				{
					name = "Collection" + suffix++;
				}
				m_settings.collections.Add(new SheetXCollectionDefinition
				{
					name = name,
					autoLoad = true,
				});
				EditorUtility.SetDirty(m_settings);
			}
			GUILayout.EndHorizontal();
		}

		private void DrawCollectionsSection()
		{
			GUILayout.BeginVertical("box");
			DrawCollectionHeader();

			var global = m_settings.collections.First(collection => collection.builtInGlobal);
			DrawGlobalCollectionRow(global);
			for (int i = 0; i < m_settings.collections.Count; i++)
			{
				var collection = m_settings.collections[i];
				if (!collection.builtInGlobal)
					DrawCustomCollectionRow(collection);
			}

			GUILayout.EndVertical();
		}

		private static void DrawCollectionHeader()
		{
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Collection Name", "Unique identifier for this collection."), EditorStyles.boldLabel);
			EditorGUILayout.LabelField(new GUIContent("Generated Class", "The C# ScriptableObject class name that will be generated."), EditorStyles.boldLabel, GUILayout.Width(180));
			EditorGUILayout.LabelField(new GUIContent("Auto", "Automatically bake JSON into ScriptableObject asset after export."), EditorStyles.boldLabel, GUILayout.Width(35));
			EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(145));
			GUILayout.EndHorizontal();
		}

		private void DrawGlobalCollectionRow(SheetXCollectionDefinition collection)
		{
			GUILayout.BeginHorizontal();

			// Global name is default and immutable
			GUI.enabled = false;
			EditorGUILayout.TextField(collection.name);
			GUI.enabled = true;

			EditorGUILayout.LabelField(
				SheetXCollectionNaming.CollectionTypeName(collection.name), GUILayout.Width(180));

			EditorGUI.BeginChangeCheck();
			collection.autoLoad = EditorGUILayout.Toggle(
				new GUIContent("", "Automatically bake JSON into GlobalConfigCollection.asset after export"),
				collection.autoLoad, GUILayout.Width(35));
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_settings);

			if (GUILayout.Button(new GUIContent("Load Data", "Bake JSON data into GlobalConfigCollection.asset now"), GUILayout.Width(80)))
				LoadData(SheetXCollectionSettings.GlobalName);

			// Placeholder layout space matching delete button width
			GUILayout.Space(64);

			GUILayout.EndHorizontal();
		}

		private void DrawCustomCollectionRow(SheetXCollectionDefinition collection)
		{
			GUILayout.BeginHorizontal();
			string renamed = EditorGUILayout.TextField(collection.name);
			if (!string.Equals(renamed, collection.name, StringComparison.Ordinal))
			{
				if (!SheetXCollectionSettings.RenameCollection(
					m_settings, collection.name, renamed, out string renameError))
				{
					Debug.LogError(renameError);
				}
				else
				{
					EditorUtility.SetDirty(m_settings);
				}
			}

			EditorGUILayout.LabelField(
				SheetXCollectionNaming.CollectionTypeName(collection.name), GUILayout.Width(180));

			EditorGUI.BeginChangeCheck();
			collection.autoLoad = EditorGUILayout.Toggle(
				new GUIContent("", "Automatically bake JSON into ScriptableObject asset after export"),
				collection.autoLoad, GUILayout.Width(35));
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_settings);

			if (GUILayout.Button(new GUIContent("Load Data", "Bake JSON data into this collection's ScriptableObject asset now"), GUILayout.Width(80)))
				LoadData(collection.name);

			if (GUILayout.Button(new GUIContent("Delete", "Delete this custom collection. Its sheet bindings will move to Global."), GUILayout.Width(60))
				&& EditorUtility.DisplayDialog(
					"Delete Collection", $"Delete '{collection.name}'? Its sheet bindings move to Global.", "Delete", "Cancel"))
			{
				if (!SheetXCollectionSettings.DeleteCollection(
					m_settings, collection.name, out string deleteError))
				{
					Debug.LogError(deleteError);
				}
				else
				{
					EditorUtility.SetDirty(m_settings);
				}
			}

			GUILayout.EndHorizontal();
		}

		private void LoadData(string collectionName)
		{
			if (!SheetXCollectionBaker.TryLoadData(m_settings, collectionName, out string error))
				Debug.LogError(error);
		}

		private void LoadData(bool autoLoadOnly)
		{
			if (!SheetXCollectionBaker.TryLoadData(m_settings, autoLoadOnly, out string error))
				Debug.LogError(error);
		}
	}
}
