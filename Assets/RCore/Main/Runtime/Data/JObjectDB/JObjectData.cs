using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RCore.Data.JObject
{
	public interface IJObjectData
	{
		void Save(bool minimizeSize = false);
		bool Load();
		void Delete();
		string ToJson(bool minimizeSize = false);
	}

	public abstract class JObjectData : IJObjectData
	{
		[JsonIgnore] public string key { get; set; }
		/// <summary>
		/// Save data 
		/// </summary>
		/// <param name="minimizeSize">True: Minimize json data by Json.Net. False: serialize by JsonUtility; it is recommended due to its better performance.</param>
		public virtual void Save(bool minimizeSize = false)
		{
			if (string.IsNullOrEmpty(key))
			{
				UnityEngine.Debug.LogError($"{GetType().Name}: key is null");
				return;
			}
			PlayerPrefs.SetString(key, ToJson(minimizeSize));
		}
		public virtual bool Load()
		{
			if (!PlayerPrefs.HasKey(key))
				return false;
			var json = PlayerPrefs.GetString(key);
			return Load(json);
		}
		public bool Load(string json)
		{
			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					JsonUtility.FromJsonOverwrite(json, this);
					return true;
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
			return false;
		}
		public void Delete() => PlayerPrefs.DeleteKey(key);
		/// <summary>
		/// Get Json
		/// </summary>
		/// <param name="minimizeSize">True: Minimize json data by Json.Net. False: serialize by JsonUtility; it is recommended due to its better performance.</param>
		/// <returns></returns>
		public string ToJson(bool minimizeSize = false)
		{
			if (!minimizeSize)
				return JsonUtility.ToJson(this);
			var serializerSettings = new JsonSerializerSettings
			{
				DefaultValueHandling = DefaultValueHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};
			return JsonConvert.SerializeObject(this, serializerSettings);
		}
		protected void DispatchEvent<T>(T e, float pDeBounce = 0) where T : BaseEvent
		{
			if (pDeBounce > 0)
				EventDispatcher.RaiseDeBounce(e, pDeBounce);
			else
				EventDispatcher.Raise(e);
		}
	}
}