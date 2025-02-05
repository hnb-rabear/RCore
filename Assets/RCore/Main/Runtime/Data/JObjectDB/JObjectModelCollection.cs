using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#if UNITY_EDITOR
using RCore.Editor;
#endif
using RCore.Inspector;
using UnityEditor;
using UnityEngine;

namespace RCore.Data.JObject
{
	public class JObjectModelCollection : ScriptableObject
	{
		[AutoFill] public SessionModel session;
		[AutoFill] public IdentityModel identity;

		private List<IJObjectModel> m_models = new();

		public virtual void Load()
		{
			m_models = new List<IJObjectModel>();

			CreateModel(session, "SessionData");
			CreateModel(identity, "Identity");
		}

		public virtual void Save()
		{
			if (m_models == null)
				return;

			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			foreach (var handler in m_models)
				handler.OnPreSave(utcNowTimestamp);
			foreach (var handler in m_models)
				handler.Save();
			PlayerPrefs.Save();
		}

		public virtual void Import(string jsonData)
		{
			if (m_models == null)
				return;

			var keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			foreach (var controller in m_models)
				if (keyValuePairs.TryGetValue(controller.Data.key, out string value))
					controller.Data.Load(value);
		}

		public void OnUpdate(float deltaTime)
		{
			foreach (var controller in m_models)
				controller.OnUpdate(deltaTime);
		}

		public void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			foreach (var handler in m_models)
				handler.OnPause(pause, utcNowTimestamp, offlineSeconds);
		}

		public void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			foreach (var handler in m_models)
				handler.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}

		protected void CreateModel<TData>(JObjectModel<TData> @ref, string key, TData defaultVal = null) where TData : JObjectData, new()
		{
			if (string.IsNullOrEmpty(key))
				key = typeof(TData).Name;
			@ref.data = JObjectDB.CreateCollection(key, defaultVal);
			@ref.Init();
			m_models.Add(@ref);
		}
		
		public CloudSave CreateCloudSave()
		{
			var cloudData = new CloudSave
			{
				session = this.session.data.SessionsTotal,
				playTime = this.session.data.activeTime,
				lastActive = this.session.data.lastActive,
				deviceId = this.identity.data.deviceId,
				gpgsId = this.identity.data.gpgsId,
				level = this.identity.data.level,
				data = JObjectDB.ToJson()
			};
			return cloudData;
		}
	}
	
	//===============================================================
	
	[Serializable]
	public class CloudSave
	{
		public string deviceId;
		public string gpgsId;
		public float playTime;
		public int session;
		public int lastActive;
		public int level;
		public string data;
	}

	//===============================================================

#if UNITY_EDITOR
	[CustomEditor(typeof(JObjectModelCollection), true)]
#if ODIN_INSPECTOR
	public class JObjectModelCollectionEditor : Sirenix.OdinInspector.Editor.OdinEditor
	{
		private JObjectModelCollection m_collection;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_collection = target as JObjectModelCollection;
		}
#else
	public class JObjectModelCollectionEditor : UnityEditor.Editor
	{
		private JObjectModelCollection m_collection;

		private void OnEnable()
		{
			m_collection = target as JObjectModelCollection;
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