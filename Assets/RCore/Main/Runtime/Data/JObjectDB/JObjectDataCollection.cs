/**
 * Author HNB-RaBear - 2024
 **/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#if UNITY_EDITOR
using System.IO;
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// A central ScriptableObject that acts as the root for managing the entire data state of the application.
	/// It aggregates all individual data collections (`JObjectData`) and their corresponding logic handlers (`IJObjectHandler`),
	/// and orchestrates their lifecycle events like loading, saving, and updating.
	/// </summary>
	public class JObjectDataCollection : ScriptableObject
	{
		// Specific data models and their handlers can be exposed for easy access.
		public SessionData sessionData;
		public SessionDataHandler sessionDataHandler;

		// Internal lists to keep track of all managed data and logic components.
		private List<JObjectData> m_datas;
		private List<IJObjectHandler> m_handlers;

		/// <summary>
		/// Initializes the data collection. This method is the primary entry point for setting up
		/// the game's data architecture. It creates all necessary data objects and their handlers.
		/// </summary>
		public virtual void Load()
		{
			m_datas = new List<JObjectData>();
			m_handlers = new List<IJObjectHandler>();

			// Example of creating a model: SessionData and its handler.
			(sessionData, sessionDataHandler) = CreateModel<SessionData, SessionDataHandler, JObjectDataCollection>("SessionData");
		}

		/// <summary>
		/// Saves all managed data collections. It first calls the `OnPreSave` hook on all handlers
		/// to allow for any final calculations, then persists each data object.
		/// </summary>
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

		/// <summary>
		/// Imports game data from a JSON string, overwriting the data for matching collections.
		/// </summary>
		/// <param name="jsonData">A JSON string typically representing a full data backup.</param>
		public virtual void Import(string jsonData)
		{
			if (m_datas == null)
			{
				Debug.LogError("Cannot import data before the collection has been loaded.");
				return;
			}

			var keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			if (keyValuePairs == null) return;
			
			foreach (var collection in m_datas)
				if (keyValuePairs.TryGetValue(collection.key, out string value))
					collection.Load(value);
		}

		/// <summary>
		/// Propagates the frame update event to all registered handlers.
		/// </summary>
		/// <param name="deltaTime">The time since the last frame.</param>
		public void OnUpdate(float deltaTime)
		{
			if(m_handlers != null)
				foreach (var controller in m_handlers)
					controller.OnUpdate(deltaTime);
		}

		/// <summary>
		/// Propagates the application pause/resume event to all registered handlers.
		/// </summary>
		public void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			if(m_handlers != null)
				foreach (var handler in m_handlers)
					handler.OnPause(pause, utcNowTimestamp, offlineSeconds);
		}

		/// <summary>
		/// Propagates the post-load event to all registered handlers, useful for calculating offline progress.
		/// </summary>
		public void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			if(m_handlers != null)
				foreach (var handler in m_handlers)
					handler.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}

		/// <summary>
		/// A factory method to create and register a data object (`JObjectData`).
		/// </summary>
		/// <typeparam name="TData">The type of the data object to create.</typeparam>
		/// <param name="key">The unique key for the data object.</param>
		/// <param name="defaultVal">An optional default value if no saved data is found.</param>
		/// <returns>The newly created data object.</returns>
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

		/// <summary>
		/// A factory method to create and register a logic handler (`JObjectHandler`).
		/// </summary>
		/// <typeparam name="THandler">The type of the handler to create.</typeparam>
		/// <typeparam name="TData">The type of this data collection, passed to the handler.</typeparam>
		/// <returns>The newly created handler instance.</returns>
		protected THandler CreateJObjectHandler<THandler, TData>()
			where THandler : JObjectHandler<TData>
			where TData : JObjectDataCollection
		{
			var newController = Activator.CreateInstance<THandler>();
			newController.dataCollection = this as TData;

			m_handlers.Add(newController);
			return newController;
		}

		/// <summary>
		/// A high-level factory method that combines the creation of both a data object and its logic handler.
		/// </summary>
		/// <returns>A tuple containing the created data object and its handler.</returns>
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
	/// <summary>
	/// A custom editor for the `JObjectDataCollection` class, providing a convenient UI
	/// for managing the entire game data system from the Unity Inspector.
	/// </summary>
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
			// Buttons for managing the specific instance of the data collection.
			if (GUILayout.Button("Load"))
				m_collection.Load();

			if (GUILayout.Button("Save"))
				m_collection.Save();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			// A dedicated section for global database operations via JObjectDB.
			EditorHelper.BoxVertical("JObject DB", () =>
			{
				if (JObjectDB.collections.Count > 0 && GUILayout.Button("Save All Collections"))
					JObjectDB.Save();

				if (GUILayout.Button("Backup All Data..."))
				{
					var time = DateTime.Now;
					string fileName = $"GameData_{time:yyMMdd_HHmm}";
					string path = EditorUtility.SaveFilePanel("Backup Data", Application.dataPath.Replace("Assets", "Saves"), fileName, "json");

					if (!string.IsNullOrEmpty(path))
						File.WriteAllText(path, JObjectDB.ToJson());
				}

				if (GUILayout.Button("Copy All Data to Clipboard"))
					JObjectDB.CopyAllData();

				if (!Application.isPlaying)
				{
					if (GUILayout.Button("Delete All Data") && EditorUtility.DisplayDialog("Confirm Action", "This will delete all saved data in PlayerPrefs managed by JObjectDB. Are you sure?", "Delete All", "Cancel"))
						JObjectDB.DeleteAll();

					if (GUILayout.Button("Restore Data from File..."))
					{
						string filePath = EditorUtility.OpenFilePanel("Select Data File", Application.dataPath.Replace("Assets", "Saves"), "json,txt");
						if (!string.IsNullOrEmpty(filePath))
							JObjectDB.Restore(filePath);
					}
				}
			}, isBox: true);
		}
	}
#endif
}