/**
 * Author HNB-RaBear - 2024
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using RCore.Inspector;
using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// A root ScriptableObject that aggregates and manages a collection of `JObjectModel` instances.
	/// This class acts as the central hub for the game's entire data state, orchestrating the lifecycle events
	/// (Load, Save, Update, Pause, etc.) for all registered data models.
	/// </summary>
	public partial class JObjectModelCollection : ScriptableObject
	{
		[Tooltip("The primary model for session-related data, such as last login time.")]
		[CreateScriptableObject, AutoFill] public SessionModel session;
		
		/// <summary>
		/// An internal list of all data models managed by this collection.
		/// </summary>
		protected List<IJObjectModel> m_models = new();
		private Dictionary<Type, object> m_resolveCache = new();
		private static readonly object s_notFound = new();
		private static readonly Dictionary<Type, FieldInfo[]> s_injectFieldCache = new();
		
		/// <summary>
		/// Initializes the data system. This method is the primary entry point, responsible for
		/// creating and setting up all the game's data models via the <see cref="CreateModel{TData}"/> method.
		/// </summary>
		public virtual void Load()
		{
			m_models = new List<IJObjectModel>();
			m_resolveCache.Clear();

			CreateModel(session, "SessionData");
		}
		
		/// <summary>
		/// Saves all managed data models. It orchestrates the save process by first calling `OnPreSave`
		/// on all models to allow for final calculations, then calls `Save` on each one to persist the data.
		/// </summary>
		public virtual void Save()
		{
			if (m_models == null)
				return;

			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			foreach (var model in m_models)
				model.OnPreSave(utcNowTimestamp);
			
			foreach (var model in m_models)
				model.Save();
			
			RPlayerPrefContainer.SaveChanges();
			PlayerPrefs.Save();
		}

		/// <summary>
		/// Imports game data from a JSON string, overwriting data for matching collections.
		/// After importing, it triggers `PostLoad` logic to re-calculate states.
		/// </summary>
		/// <param name="jsonData">A JSON string representing the entire data set, typically from a backup.</param>
		public virtual void Import(string jsonData)
		{
			if (m_models == null) return;

			var keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			if (keyValuePairs == null) return;
			
			foreach (var model in m_models)
				if (keyValuePairs.TryGetValue(model.Data.key, out string value))
					model.Data.Load(value);

			// m_resolveCache is intentionally NOT cleared here: InjectDependencies() has already
			// run once after Load() and field references are set on model instances directly.
			PostLoad();
		}

		/// <summary>
		/// Imports game data from a dictionary of objects. This is useful for integrating with systems
		/// that provide data in a pre-parsed format.
		/// </summary>
		/// <param name="data">A dictionary where the key is the collection name and the value is the data object.</param>
		public virtual void Import(Dictionary<string, object> data)
		{
			if (m_models == null) return;

			foreach (var model in m_models)
			{
				if (data.TryGetValue(model.Data.key, out var valueObject))
				{
					try
					{
						// Re-serialize the object to JSON and then load it to ensure proper type conversion.
						var valueStr = JsonConvert.SerializeObject(valueObject);
						model.Data.Load(valueStr);
					}
					catch (Exception ex)
					{
						Debug.LogError($"Error deserializing data for key: {model.Data.key} - {ex.Message}");
					}
				}
			}

			// m_resolveCache is intentionally NOT cleared here: InjectDependencies() has already
			// run once after Load() and field references are set on model instances directly.
			PostLoad();
		}

		/// <summary>
		/// Exports all managed data into a dictionary.
		/// </summary>
		/// <returns>A dictionary where the key is the data's unique key and the value is the JObjectData instance.</returns>
		public Dictionary<string, object> GetData()
		{
			var data = new Dictionary<string, object>();
			foreach (var model in m_models)
				data.Add(model.Key, model.Data);
			return data;
		}

		/// <summary>
		/// Propagates the frame update event to all registered models.
		/// </summary>
		public virtual void OnUpdate(float deltaTime)
		{
			if(m_models != null)
				foreach (var model in m_models)
					model.OnUpdate(deltaTime);
		}
		
		/// <summary>
		/// Propagates the application pause/resume event to all registered models.
		/// </summary>
		public virtual void OnPause(bool pause)
		{
			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			int offlineSeconds = 0;
			if (!pause)
				offlineSeconds = session.GetOfflineSeconds();
			foreach (var handler in m_models)
				handler.OnPause(pause, utcNowTimestamp, offlineSeconds);
		}
		
		/// <summary>
		/// Propagates the post-load event to all registered models, allowing them to calculate offline progress.
		/// <para>Note: <see cref="InjectDependencies"/> must be called before this method.</para>
		/// </summary>
		public virtual void PostLoad()
		{
			int offlineSeconds = session.GetOfflineSeconds();
			var utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			foreach (var handler in m_models)
				handler.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}

		/// <summary>
		/// Propagates the remote config fetched event to all registered models.
		/// This should be called by your remote config system after it successfully fetches new values.
		/// </summary>
		public void OnRemoteConfigFetched()
		{
			foreach (var handler in m_models)
				handler.OnRemoteConfigFetched();
		}
		
		/// <summary>
		/// A factory method that links a JObjectModel ScriptableObject with its JObjectData.
		/// It handles the creation of the data via JObjectDB, initializes the model, and registers it for lifecycle events.
		/// </summary>
		/// <param name="ref">A reference to the JObjectModel field (the ScriptableObject instance).</param>
		/// <param name="key">The unique key to use for saving and loading the data.</param>
		/// <param name="defaultVal">An optional default data object to use if no saved data exists.</param>
		protected void CreateModel<TData>(JObjectModel<TData> @ref, string key, TData defaultVal = null) where TData : JObjectData, new()
		{
			if (string.IsNullOrEmpty(key))
				key = typeof(TData).Name;
				
			@ref.data = JObjectDB.CreateCollection(key, defaultVal);
			@ref.key = key;
			@ref.Init();
			@ref.data.key = key;
			m_models.Add(@ref);
		}
		
		/// <summary>
		/// An overload of CreateModel that uses the `key` field pre-defined in the JObjectModel ScriptableObject.
		/// </summary>
		protected void CreateModel<TData>(JObjectModel<TData> @ref, TData defaultVal = null) where TData : JObjectData, new()
		{
			if (string.IsNullOrEmpty(@ref.key))
				@ref.key = typeof(TData).Name;

			@ref.data = JObjectDB.CreateCollection(@ref.key, defaultVal);
			@ref.Init();
			@ref.data.key = @ref.key;
			m_models.Add(@ref);
		}
		
		/// <summary>
		/// Resolves a registered model or its underlying data object by type or interface.
		/// Searches models first, then their <see cref="IJObjectModel.Data"/> objects.
		/// </summary>
		/// <typeparam name="T">The type or interface to resolve.</typeparam>
		/// <returns>The first matching instance, or <c>null</c> if not found.</returns>
		public T Get<T>() where T : class
		{
			if (m_models == null) return null;
			return Resolve(typeof(T)) as T;
		}
		
		/// <summary>
		/// Scans all registered models for fields marked with <see cref="InjectAttribute"/> and
		/// resolves them from the registered model collection.
		/// Call this after all <see cref="CreateModel{TData}"/> calls are complete.
		/// </summary>
		public void InjectDependencies()
		{
			if (m_models == null) return;
			foreach (var model in m_models)
			{
				var fields = GetInjectFields(model.GetType());

				foreach (var field in fields)
				{
					var resolved = Resolve(field.FieldType);

					if (resolved != null)
						field.SetValue(model, resolved);
					else
						throw new InvalidOperationException(
							$"[Inject] Cannot resolve '{field.FieldType.Name}' for '{model.GetType().Name}.{field.Name}'. " +
							$"Register it via CreateModel() in Load() before PostLoad() is called. " +
							$"Currently registered: {string.Join(", ", m_models.Select(m => m.GetType().Name))}"
						);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static FieldInfo[] GetInjectFields(Type type)
		{
			if (!s_injectFieldCache.TryGetValue(type, out var fields))
			{
				fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(f => f.GetCustomAttribute<InjectAttribute>() != null)
					.ToArray();
				s_injectFieldCache[type] = fields;
			}
			return fields;
		}
		
		/// <summary>
		/// Core resolution logic shared by <see cref="Get{T}"/> and <see cref="InjectDependencies"/>.
		/// Searches registered models first, then their <see cref="IJObjectModel.Data"/> objects.
		/// Results are cached, including misses.
		/// </summary>
		private object Resolve(Type type)
		{
			if (m_resolveCache.TryGetValue(type, out var cached))
				return cached == s_notFound ? null : cached;

			foreach (var candidate in m_models)
			{
				if (type.IsInstanceOfType(candidate))
				{
					m_resolveCache[type] = candidate;
					return candidate;
				}
			}

			foreach (var candidate in m_models)
			{
				if (type.IsInstanceOfType(candidate.Data))
				{
					m_resolveCache[type] = candidate.Data;
					return candidate.Data;
				}
			}

			m_resolveCache[type] = s_notFound;
			return null;
		}
	}

	//===============================================================

#if UNITY_EDITOR
	/// <summary>
	/// A custom editor for the `JObjectModelCollection`, providing a convenient UI
	/// for managing the entire game data system from the Unity Inspector.
	/// </summary>
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
			// Buttons for managing this specific data collection instance.
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
						JObjectDB.Backup(path);
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