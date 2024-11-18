using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Data.JObject
{
	public class JObjectsCollection : ScriptableObject
	{
		public SessionData sessionData;
		public SessionDataHandler sessionDataHandler;

		internal List<JObjectData> datas;
		internal List<IJObjectHandler> handlers;

		public virtual void Load()
		{
			datas = new List<JObjectData>();
			handlers = new List<IJObjectHandler>();

			(sessionData, sessionDataHandler) = CreateModule<SessionData, SessionDataHandler, JObjectsCollection>("SessionData");
		}

		protected TCollection CreateCollection<TCollection>(string key, TCollection defaultVal = null)
			where TCollection : JObjectData, new()
		{
			if (string.IsNullOrEmpty(key))
				key = typeof(TCollection).Name;
			var newCollection = JObjectDB.CreateCollection(key, defaultVal);
			if (newCollection != null)
				datas.Add(newCollection);
			return newCollection;
		}

		protected THandler CreateController<THandler, TJObjectsCollection>()
			where THandler : JObjectHandler<TJObjectsCollection>
			where TJObjectsCollection : JObjectsCollection
		{
			var newController = Activator.CreateInstance<THandler>();
			newController.dbManager = this as TJObjectsCollection;

			handlers.Add(newController);
			return newController;
		}

		protected (TCollection, THandler) CreateModule<TCollection, THandler, TJObjectsCollection>(string key, TCollection defaultVal = null)
			where TCollection : JObjectData, new()
			where THandler : JObjectHandler<TJObjectsCollection>
			where TJObjectsCollection : JObjectsCollection
		{
			var collection = CreateCollection(key, defaultVal);
			var controller = CreateController<THandler, TJObjectsCollection>();
			return (collection, controller);
		}
	}

	//===============================================================

#if UNITY_EDITOR
	[CustomEditor(typeof(JObjectsCollection), true)]
	public class JObjectsCollectionEditor : UnityEditor.Editor
	{
		private JObjectsCollection m_jObjectsCollection;

		private void OnEnable()
		{
			m_jObjectsCollection = target as JObjectsCollection;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Load"))
				m_jObjectsCollection.Load();

			GUILayout.Space(5);

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
#endif
}