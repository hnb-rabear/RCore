using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RCore.Editor.Tool
{
	internal class FindComponentReferenceDrawer
	{
		private List<System.Type> m_types = new List<System.Type>();
		private List<string> m_typesArray = new List<string>();
		private int m_idx;
		private string m_filter;
		private string m_preFilter;

		public void DrawOnGUI()
		{
			if (m_filter != m_preFilter)
			{
				m_types = null;
				m_preFilter = m_filter;
			}

			if (m_types == null)
				GetAllTypes();

			GUILayout.Label("Filter");
			m_filter = EditorGUILayout.TextField(m_filter);
			GUILayout.Label("Select Script");
			if (m_typesArray.Count > 0)
				m_idx = EditorGUILayout.Popup(m_idx, m_typesArray.ToArray());

			if (m_types.Count > 0 && GUILayout.Button("Find all prefabs"))
				ShowItemsOfTypeInProjectHierarchy(m_types[m_idx]);
		}

		private void GetAllTypes()
		{
			string filter = m_filter?.ToLower() ?? "";
			m_types = new List<System.Type>();
			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			foreach (var asm in assemblies)
			{
				System.Type[] types;
				try { types = asm.GetTypes(); }
				catch { continue; }
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
			m_types.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));

			m_typesArray = new List<string>();
			foreach (var type in m_types)
			{
				string scriptName = type.Name;
				if (!string.IsNullOrEmpty(type.Namespace))
					scriptName = $"{type.Namespace}.{type.Name}";
				m_typesArray.Add(scriptName);
			}
		}

		private static void ShowItemsOfTypeInProjectHierarchy(System.Type type)
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
						continue;

					var comp = go.GetComponent(type);
					if (comp != null)
					{
						toSelect.Add(go.GetInstanceID());
					}
					else
					{
						var comps = go.GetComponentsInChildren(type);
						if (comps.Length > 0)
							toSelect.Add(go.GetInstanceID());
					}
				}
			}

			Selection.instanceIDs = System.Array.Empty<int>();
			ShowSelectionInProjectHierarchy();
			Selection.instanceIDs = toSelect.ToArray();
			ShowSelectionInProjectHierarchy();
		}

		private static void ShowSelectionInProjectHierarchy()
		{
			var pbType = GetTypeByName("UnityEditor.ProjectBrowser");
			var meth = pbType?.GetMethod("ShowSelectedObjectsInLastInteractedProjectBrowser",
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Static);
			meth?.Invoke(null, null);
		}

		private static System.Type GetTypeByName(string name)
		{
			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			foreach (var asm in assemblies)
			{
				var type = asm.GetType(name);
				if (type != null)
					return type;
			}
			return null;
		}
	}
}
