using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Data.JObject
{
	public class JObjectModelCollection : ScriptableObject
	{
		public SessionModel session;
		
		internal List<IJObjectController> handlers; 
		
		public virtual void Load()
		{
			handlers = new List<IJObjectController>();
			
			CreateModule(session, "SessionData");
		}

		public virtual void Save()
		{
			if (handlers == null)
				return;
			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			foreach (var handler in handlers)
				handler.OnPreSave(utcNowTimestamp);
			foreach (var handler in handlers)
				handler.Save();
		}

		protected void CreateModule<TData>(JObjectModel<TData> @ref, string key, TData defaultVal = null) where TData : JObjectData, new()
		{
			if (string.IsNullOrEmpty(key))
				key = typeof(TData).Name;
			@ref.data = JObjectDB.CreateCollection(key, defaultVal);
			handlers.Add(@ref);
		}
	}
}