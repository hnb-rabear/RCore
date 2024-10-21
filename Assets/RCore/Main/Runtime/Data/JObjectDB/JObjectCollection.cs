using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RCore.Data.JObject
{
	[Serializable]
	public abstract class JObjectCollection
	{
		[JsonIgnore] public string key { get; set; }

		public JObjectCollection() { }
		public JObjectCollection(string pKey)
		{
			key = pKey;
		}
		/// <summary>
		/// Save data 
		/// </summary>
		/// <param name="minimizeSize">True: Minimize json data by Json.Net. False: serialize by JsonUtility; it is recommended due to its better performance.</param>
		public virtual void Save(bool minimizeSize = false)
		{
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
	}
}