using RCore.Common;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace RCore.Editor
{
	public class ComponentReferenceFinderWindow : EditorWindow
	{
		[MenuItem("RCore/Tools/Component Browser")]
		private static void ShowWindow()
		{
			var window = GetWindow<ComponentReferenceFinderWindow>("Component Browser", true);
			window.Show();
		}

		private List<Type> m_types = new List<Type>();
		private List<string> m_typesArray = new List<string>();
		private int m_idx;
		private string m_Filter;
		private string m_PreFilter;
		private Vector2 m_ScrollPosition;

		private void OnGUI()
		{
			m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, false, false);

			if (m_Filter != m_PreFilter)
			{
				m_types = null;
				m_PreFilter = m_Filter;
			}

			if (m_types == null)
				GetAllTypes();

			GUILayout.Label("Filter");
			m_Filter = EditorGUILayout.TextField(m_Filter);
			GUILayout.Label("Select Script");
			m_idx = EditorGUILayout.Popup(m_idx, m_typesArray.ToArray());

			if (GUILayout.Button("Find all prefabs"))
				ShowItemsOfTypeInProjectHierarchy(m_types[m_idx]);

			GUILayout.EndScrollView();
		}

		private void GetAllTypes()
		{
			string filter = m_Filter.ToLower();
			m_types = new List<Type>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var asm in assemblies)
			{
				var types = asm.GetTypes();
				foreach (var type in types)
				{
					if (type.IsSubclassOf(typeof(MonoBehaviour)))
					{
						string scriptName = type.Name;
						if (!string.IsNullOrEmpty(type.Namespace))
							scriptName = $"{type.Namespace}.{type.Name}";
						if (!string.IsNullOrEmpty(filter) && !scriptName.ToLower().Contains(filter))
							continue;
						m_types.Add(type);
					}
				}
			}
			m_types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

			m_typesArray = new List<string>();
			foreach (var type in m_types)
			{
				string scriptName = type.Name;
				if (!string.IsNullOrEmpty(type.Namespace))
					scriptName = $"{type.Namespace}.{type.Name}";
				m_typesArray.Add(scriptName);
			}
		}

		private static void ShowItemsOfTypeInProjectHierarchy(Type type)
		{
			var guids = AssetDatabase.FindAssets("t:Prefab");
			var toSelect = new List<int>();
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);
				foreach (var obj in toCheck)
				{
					var go = obj as GameObject;
					if (go == null)
					{
						continue;
					}

					var comp = go.GetComponent(type);
					if (comp != null)
					{
						toSelect.Add(go.GetInstanceID());
					}
					else
					{
						var comps = go.GetComponentsInChildren(type);
						if (comps.Length > 0)
						{
							toSelect.Add(go.GetInstanceID());
						}
					}
				}
			}

			// clear the current selection
			Selection.instanceIDs = Array.Empty<int>();
			ShowSelectionInProjectHierarchy();

			// show the prefabs we found
			Selection.instanceIDs = toSelect.ToArray();
			ShowSelectionInProjectHierarchy();
		}

		// use internal classes to update the selection in the project hierarchy.
		// it's dumb that we have to do this.
		private static void ShowSelectionInProjectHierarchy()
		{
			var pbType = GetType("UnityEditor.ProjectBrowser");
			var meth = pbType.GetMethod("ShowSelectedObjectsInLastInteractedProjectBrowser",
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Static);
			meth.Invoke(null, null);
		}

		// helper method to find a type of a given name
		private static Type GetType(string name)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var asm in assemblies)
			{
				var type = asm.GetType(name);
				if (type != null)
				{
					return type;
				}
			}

			return null;
		}
	}
}