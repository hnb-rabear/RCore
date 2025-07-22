/**
 * Author HNB-RaBear - 2024
 **/

using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// Defines a contract for any data object that can be saved, loaded, deleted,
	/// and serialized to JSON. This interface ensures a consistent API for data handling.
	/// </summary>
	public interface IJObjectData
	{
		/// <summary>
		/// Persists the current state of the object.
		/// </summary>
		/// <param name="minimizeSize">If true, serialization should aim for the smallest possible output size.</param>
		void Save(bool minimizeSize = false);
		
		/// <summary>
		/// Loads the object's state from its persisted source.
		/// </summary>
		/// <returns>True if loading was successful, otherwise false.</returns>
		bool Load();
		
		/// <summary>
		/// Deletes the object's persisted data.
		/// </summary>
		void Delete();
		
		/// <summary>
		/// Serializes the current state of the object to a JSON string.
		/// </summary>
		/// <param name="minimizeSize">If true, serialization should aim for the smallest possible output size.</param>
		/// <returns>A JSON string representation of the object.</returns>
		string ToJson(bool minimizeSize = false);
	}

	/// <summary>
	/// An abstract base class that provides a default implementation of `IJObjectData` using `PlayerPrefs` for storage.
	/// All custom data models intended for use with `JObjectDB` should inherit from this class.
	/// </summary>
	public abstract class JObjectData : IJObjectData
	{
		/// <summary>
		/// The unique key used to store and retrieve this object's data in PlayerPrefs.
		/// This property is ignored during JSON serialization to prevent it from being part of the saved data itself.
		/// </summary>
		[JsonIgnore] public string key { get; set; }
		
		/// <summary>
		/// Saves the object's current state to PlayerPrefs by serializing it to a JSON string.
		/// </summary>
		/// <param name="minimizeSize">
		/// If false (default), uses `JsonUtility` for fast serialization.
		/// If true, uses `Newtonsoft.Json` to create a smaller JSON string by ignoring default and null values, which is useful for network transfer but is slower.
		/// </param>
		public virtual void Save(bool minimizeSize = false)
		{
			if (string.IsNullOrEmpty(key))
			{
				UnityEngine.Debug.LogError($"{GetType().Name}: Cannot save because the 'key' is null or empty.");
				return;
			}
			PlayerPrefs.SetString(key, ToJson(minimizeSize));
		}

		/// <summary>
		/// Loads the object's state from the JSON string stored in PlayerPrefs under its assigned key.
		/// It uses `FromJsonOverwrite` to populate the fields of the existing object instance.
		/// </summary>
		/// <returns>True if a saved value was found and successfully loaded, otherwise false.</returns>
		public virtual bool Load()
		{
			if (!PlayerPrefs.HasKey(key))
				return false;
			
			var json = PlayerPrefs.GetString(key);
			return Load(json);
		}

		/// <summary>
		/// Populates this object's fields from a given JSON string using `JsonUtility.FromJsonOverwrite`.
		/// This updates the current instance in-place rather than creating a new one, which preserves references.
		/// </summary>
		/// <param name="json">The JSON string to load data from.</param>
		/// <returns>True if deserialization was successful, otherwise false.</returns>
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
					Debug.LogError($"Error loading JSON for key '{key}': {ex.Message}");
				}
			}
			return false;
		}

		/// <summary>
		/// Deletes the saved data for this object from PlayerPrefs using its assigned key.
		/// </summary>
		public void Delete() => PlayerPrefs.DeleteKey(key);

		/// <summary>
		/// Serializes the object to a JSON string.
		/// </summary>
		/// <param name="minimizeSize">
		/// If false (default), uses `JsonUtility` for fast, Unity-friendly serialization.
		/// If true, uses `Newtonsoft.Json` to create a smaller JSON string by ignoring default and null values. This is ideal for network transmission or minimizing storage space, at the cost of performance.
		/// </param>
		/// <returns>A JSON string representing the object.</returns>
		public string ToJson(bool minimizeSize = false)
		{
			if (!minimizeSize)
				return JsonUtility.ToJson(this);
			
			var serializerSettings = new JsonSerializerSettings
			{
				// Exclude properties that have their default value (e.g., int 0, bool false, null objects).
				DefaultValueHandling = DefaultValueHandling.Ignore,
				// Exclude properties that are explicitly null.
				NullValueHandling = NullValueHandling.Ignore,
				// Avoid errors with circular references by ignoring them.
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};
			return JsonConvert.SerializeObject(this, serializerSettings);
		}

		/// <summary>
		/// A convenience helper method for child classes to dispatch events through a central `EventDispatcher`.
		/// </summary>
		/// <typeparam name="T">The type of the event, which must inherit from `BaseEvent`.</typeparam>
		/// <param name="e">The event instance to raise.</param>
		/// <param name="pDeBounce">An optional debounce interval in seconds. If greater than 0, the event will only be raised if this interval has passed since the last time an event of the same type was raised.</param>
		protected void DispatchEvent<T>(T e, float pDeBounce = 0) where T : BaseEvent
		{
			if (pDeBounce > 0)
				EventDispatcher.RaiseDeBounce(e, pDeBounce);
			else
				EventDispatcher.Raise(e);
		}
	}
}