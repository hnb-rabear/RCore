/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern.Example
{
    using System.Collections.Generic;
    using UnityEngine; // For Debug.Log in this example

    // This module will be auto-created with a load order of 5.
    [Module(nameof(RemoteConfigModuleExample), LoadOrder = 5)]
    public class RemoteConfigModuleExample : IRemoteConfigModuleExample
    {
        public string ModuleID => "FirebaseRemoteConfig_v1";

        // A simple dictionary to simulate fetched config values for this example
        private Dictionary<string, object> m_mockConfigValues = new Dictionary<string, object>();

        public void Initialize()
        {
            Debug.Log($"[{nameof(RemoteConfigModuleExample)}] Started. ID: {ModuleID}");
            // In a real scenario, you might initiate fetching remote config here.
            // For this example, we'll just populate some mock values.
            m_mockConfigValues["welcome_message"] = "Hello from Firebase Remote Config!";
            m_mockConfigValues["feature_x_enabled"] = true;
            m_mockConfigValues["max_lives"] = 5;
            Debug.Log($"[{nameof(RemoteConfigModuleExample)}] Mock config loaded.");
        }

        public void Tick()
        {
            // Remote config modules usually don't need per-frame updates.
            // Some might have logic to periodically check for updated config.
        }

        public void Shutdown()
        {
            Debug.Log($"[{nameof(RemoteConfigModuleExample)}] Destroyed. ID: {ModuleID}");
            m_mockConfigValues.Clear();
        }

        public string GetString(string key, string defaultValue)
        {
            if (m_mockConfigValues.TryGetValue(key, out object value) && value is string stringValue)
            {
                return stringValue;
            }
            Debug.LogWarning($"[{nameof(RemoteConfigModuleExample)}] Key '{key}' not found or not a string. Returning default.");
            return defaultValue;
        }

        public int GetInt(string key, int defaultValue)
        {
            if (m_mockConfigValues.TryGetValue(key, out object value) && value is int intValue)
            {
                return intValue;
            }
            Debug.LogWarning($"[{nameof(RemoteConfigModuleExample)}] Key '{key}' not found or not an int. Returning default.");
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue)
        {
            if (m_mockConfigValues.TryGetValue(key, out object value) && value is bool boolValue)
            {
                return boolValue;
            }
            Debug.LogWarning($"[{nameof(RemoteConfigModuleExample)}] Key '{key}' not found or not a bool. Returning default.");
            return defaultValue;
        }
    }
}