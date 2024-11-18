using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Data.JObject
{
	public class JObjectCollectionSO : ScriptableObject
	{
		public SessionData sessionData;
		public SessionDataHandler sessionDataHandler;

		[NonSerialized] public List<JObjectCollection> collections = new List<JObjectCollection>();
		[NonSerialized] public List<IJObjectHandler> handlers = new List<IJObjectHandler>();

		public virtual void Load()
		{
			(sessionData, sessionDataHandler) = CreateModule<SessionData, SessionDataHandler, JObjectCollectionSO>("UserSession");
		}

		protected TCollection CreateCollection<TCollection>(string key, TCollection defaultVal = null)
			where TCollection : JObjectCollection, new()
		{
			if (string.IsNullOrEmpty(key))
				key = typeof(TCollection).Name;
			var newCollection = JObjectDB.CreateCollection(key, defaultVal);
			if (newCollection != null)
				collections.Add(newCollection);
			return newCollection;
		}

		protected THandler CreateController<THandler, TManager>()
			where THandler : JObjectHandler<TManager>
			where TManager : JObjectCollectionSO
		{
			var newController = Activator.CreateInstance<THandler>();
			newController.dbManager = this as TManager;

			handlers.Add(newController);
			return newController;
		}

		protected (TCollection, THandler) CreateModule<TCollection, THandler, TManager>(string key, TCollection defaultVal = null)
			where TCollection : JObjectCollection, new()
			where THandler : JObjectHandler<TManager>
			where TManager : JObjectCollectionSO
		{
			var collection = CreateCollection(key, defaultVal);
			var controller = CreateController<THandler, TManager>();
			return (collection, controller);
		}
	}
	
	#if UNITY_EDITOR
	[CustomEditor(typeof(JObjectCollectionSO), true)]
	public class JObjectCollectionSOEditor : UnityEditor.Editor
	{
		private JObjectCollectionSO m_jObjectCollection;

		private void OnEnable()
		{
			m_jObjectCollection = target as JObjectCollectionSO;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			if (GUILayout.Button("Load"))
				m_jObjectCollection.Load();
		}
	}
	#endif
}