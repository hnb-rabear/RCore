using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Data.JObject
{
	public class JObjectDataCollection : ScriptableObject
	{
		public SessionData sessionData;
		public SessionDataHandler sessionDataHandler;

		private List<JObjectData> m_datas;
		private List<IJObjectHandler> m_handlers;

		public virtual void Load()
		{
			m_datas = new List<JObjectData>();
			m_handlers = new List<IJObjectHandler>();

			(sessionData, sessionDataHandler) = CreateModel<SessionData, SessionDataHandler, JObjectDataCollection>("SessionData");
		}
		
		public virtual void Save()
		{
			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			if (m_handlers != null)
				foreach (var handler in m_handlers)
					handler.OnPreSave(utcNowTimestamp);
			if (m_datas != null)
				foreach (var collection in m_datas)
					collection.Save();
		}

		public virtual void Import(string jsonData)
		{
			if (m_datas == null)
				return;

			var keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			foreach (var collection in m_datas)
				if (keyValuePairs.TryGetValue(collection.key, out string value))
					collection.Load(value);
		}

		public void OnUpdate(float deltaTime)
		{
			foreach (var controller in m_handlers)
				controller.OnUpdate(deltaTime);
		}

		public void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			foreach (var handler in m_handlers)
				handler.OnPause(pause, utcNowTimestamp, offlineSeconds);
		}

		public void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			foreach (var handler in m_handlers)
				handler.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}
		
		protected TData CreateJObjectData<TData>(string key, TData defaultVal = null)
			where TData : JObjectData, new()
		{
			if (string.IsNullOrEmpty(key))
				key = typeof(TData).Name;
			var newCollection = JObjectDB.CreateCollection(key, defaultVal);
			if (newCollection != null)
				m_datas.Add(newCollection);
			return newCollection;
		}

		protected THandler CreateJObjectHandler<THandler, TData>()
			where THandler : JObjectHandler<TData>
			where TData : JObjectDataCollection
		{
			var newController = Activator.CreateInstance<THandler>();
			newController.dataCollection = this as TData;

			m_handlers.Add(newController);
			return newController;
		}

		protected (TData, THandler) CreateModel<TData, THandler, TDataCollection>(string key, TData defaultVal = null)
			where TData : JObjectData, new()
			where THandler : JObjectHandler<TDataCollection>
			where TDataCollection : JObjectDataCollection
		{
			var collection = CreateJObjectData(key, defaultVal);
			var controller = CreateJObjectHandler<THandler, TDataCollection>();
			return (collection, controller);
		}
	}

	//===============================================================

#if UNITY_EDITOR
	[CustomEditor(typeof(JObjectDataCollection), true)]
#if ODIN_INSPECTOR
	public class JObjectDataCollectionEditor : Sirenix.OdinInspector.Editor.OdinEditor
	{
		private JObjectDataCollection m_collection;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_collection = target as JObjectDataCollection;
		}
#else
	public class JObjectDataCollectionEditor : UnityEditor.Editor
	{
		private JObjectDataCollection m_collection;

		private void OnEnable()
		{
			m_collection = target as JObjectDataCollection;
		}
#endif

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Load"))
					m_collection.Load();

				if (GUILayout.Button("Save"))
					m_collection.Save();
			}
			EditorGUILayout.EndHorizontal();

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