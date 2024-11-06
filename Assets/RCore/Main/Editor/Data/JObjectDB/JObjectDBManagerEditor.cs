using RCore.Data.JObject;
using System;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	[CustomEditor(typeof(JObjectDBManager), true)]
	public class JObjectDBManagerEditor : UnityEditor.Editor
	{
		protected JObjectDBManager m_target;

		private void OnEnable()
		{
			m_target = target as JObjectDBManager;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
				
			GUILayout.BeginVertical("box");
				
			if (GUILayout.Button("Save"))
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
				if (GUILayout.Button("Delete All"))
					JObjectDB.DeleteAll();
				
				if (GUILayout.Button("Restore"))
				{
					string filePath = EditorUtility.OpenFilePanel("Select Data File", Application.dataPath, "json,txt");
					if (!string.IsNullOrEmpty(filePath))
						JObjectDB.Restore(filePath);
				}
			}
				
			GUILayout.EndVertical();
		}
	}
}