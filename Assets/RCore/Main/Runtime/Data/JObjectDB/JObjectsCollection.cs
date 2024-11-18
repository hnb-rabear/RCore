using System;
using System.Collections.Generic;
using UnityEditor;
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
	
	#if UNITY_EDITOR
	[CustomEditor(typeof(JObjectsCollection), true)]
	public class JObjectCollectionSOEditor : UnityEditor.Editor
	{
		private JObjectsCollection m_jObjectsCollection;

		private void OnEnable()
		{
			m_jObjectsCollection = target as JObjectsCollection;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			if (GUILayout.Button("Load"))
				m_jObjectsCollection.Load();
		}
	}
	#endif
}