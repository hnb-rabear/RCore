using RCore.Data.JObject;
using System;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	[CustomEditor(typeof(JObjectDBManager<>), true)]
	public class JObjectDBManagerEditor : UnityEditor.Editor
	{
		private SerializedProperty m_jObjectsCollection;

		private void OnEnable()
		{
			m_jObjectsCollection = serializedObject.FindProperty("m_jObjectsCollection");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space(5);
			
			if (m_jObjectsCollection.objectReferenceValue == null)
				EditorGUILayout.HelpBox("Create a ScriptableObject derived from JObjectsCollection and assign it to the m_jObjectsCollection field.", MessageType.Error);

			EditorHelper.BoxVertical("JObject DB", () =>
			{
				if (JObjectDB.collections.Count > 0 && GUILayout.Button("Save"))
					JObjectDB.Save();

				if (GUILayout.Button("Backup"))
				{
					var time = DateTime.Now;
					string fileName = $"GameData_{time.Year % 100}{time.Month:00}{time.Day:00}_{time.Hour:00}h{time.Minute:00}";
					string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "txt");

					if (!string.IsNullOrEmpty(path))
						JObjectDB.Backup(path);
				}

				if (GUILayout.Button("Copy All"))
					JObjectDB.CopyAllData();

				if (!Application.isPlaying)
				{
					if (GUILayout.Button("Delete All") && EditorUtility.DisplayDialog("Confirm your action", "Delete All Data", "Delete", "Cancel"))
						JObjectDB.DeleteAll();

					if (GUILayout.Button("Restore"))
					{
						string filePath = EditorUtility.OpenFilePanel("Select Data File", Application.dataPath, "json,txt");
						if (!string.IsNullOrEmpty(filePath))
							JObjectDB.Restore(filePath);
					}
				}
			}, isBox: true);
		}
	}
}