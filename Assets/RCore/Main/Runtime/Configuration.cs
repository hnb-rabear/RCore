/***
 * Author HNB-RaBear - 2018
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RCore.UI;
using UnityEngine;
#if UNITY_EDITOR
using RCore.Editor;
#endif

namespace RCore
{
	/// <summary>
	/// A ScriptableObject that serves as a central hub for project-wide configurations.
	/// It provides a singleton-like access pattern and is primarily used to manage
	/// build environments through scripting define symbols. It also contains a generic
	/// key-value store for other settings.
	/// </summary>
	[CreateAssetMenu(fileName = "Configuration", menuName = "RCore/Configuration")]
	public class Configuration : ScriptableObject
	{
		/// <summary>
		/// Represents a single build environment, which is a collection of scripting define symbols.
		/// For example, "Development", "Staging", or "Production".
		/// </summary>
		[Serializable]
		public class Env
		{
			public string name;
			public List<Directive> directives = new();
			
			/// <summary>
			/// Adds a new directive to the environment's list if it doesn't already exist.
			/// </summary>
			/// <param name="pName">The name of the directive (e.g., "DEVELOPMENT").</param>
			/// <param name="defaultActive">The default enabled state for the new directive.</param>
			public void AddDirective(string pName, bool defaultActive)
			{
				if (string.IsNullOrEmpty(pName)) return;
				
				var find = GetDirective(pName);
				if (find == null)
					directives.Add(new Directive(pName, defaultActive));
			}
			
			/// <summary>
			/// Adds multiple new directives to the environment's list if they don't already exist.
			/// </summary>
			/// <param name="pNames">An array of directive names.</param>
			/// <returns>This Env instance for method chaining.</returns>
			public Env AddDirective(params string[] pNames)
			{
				foreach (string pName in pNames)
				{
					if (string.IsNullOrEmpty(pName)) continue;
					
					bool exist = directives.Any(directive => directive.name == pName);
					if (!exist)
						directives.Add(new Directive(pName, false));
				}
				return this;
			}
			
			/// <summary>
			/// Retrieves a directive from the list by its name.
			/// </summary>
			/// <param name="pName">The name of the directive to find.</param>
			/// <returns>The Directive object, or null if not found.</returns>
			public Directive GetDirective(string pName)
			{
				return string.IsNullOrEmpty(pName) ? null : directives.FirstOrDefault(directive => directive.name == pName);
			}
		}

		/// <summary>
		/// Represents a single scripting define symbol.
		/// Contains its name, a color for the editor UI, and its enabled state.
		/// </summary>
		[Serializable]
		public class Directive
		{
			public Directive()
			{
				color = Color.white;
			}
			public Directive(string pName, bool pEnable)
			{
				name = pName;
				enabled = pEnable;
				color = Color.white;
			}
			public string name;
			public Color color;
			public bool enabled = true;
		}

		//==================================

		private static Configuration m_Instance;
		
		/// <summary>
		/// Provides global, singleton-like access to the Configuration asset.
		/// It will attempt to load the asset from "Assets/Resources/Configuration.asset".
		/// If it doesn't exist in the editor, it will be created automatically.
		/// </summary>
		public static Configuration Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = Resources.Load<Configuration>(nameof(Configuration));
#if UNITY_EDITOR
					if (m_Instance == null)
					{
						Debug.Log("Configuration asset not found. Creating a new one at Assets/Resources/Configuration.asset");
						EditorHelper.CreateScriptableAsset<Configuration>("Assets/Resources/Configuration.asset");
						m_Instance = Resources.Load<Configuration>(nameof(Configuration));
					}
#endif
				}
				return m_Instance;
			}
		}

		/// <summary>A serializable dictionary for storing general-purpose key-value string pairs.</summary>
		public SerializableDictionary<string, string> keyValues;
		/// <summary>A list of all saved build environments.</summary>
		public List<Env> envs = new List<Env>();
		/// <summary>The currently active environment being used by the editor.</summary>
		public Env curEnv;
		
		/// <summary>Static accessor for the key-value pairs.</summary>
		public static SerializableDictionary<string, string> KeyValues => Instance.keyValues;
		
		/// <summary>
		/// Called in the editor when the script is loaded or a value is changed in the Inspector.
		/// Ensures that a default, non-removable environment exists and is populated with
		/// a master list of common directives.
		/// </summary>
		private void OnValidate()
		{
			// Ensure the "do_not_remove" environment exists at index 0.
			// This environment acts as a master list of known directives.
			if (envs.Count == 0 || envs[0].name != "do_not_remove")
			{
				envs.Insert(0, new Env()
				{
					name = "do_not_remove",
				});
			}
			// Add a predefined set of common directives to the master list.
			envs[0].AddDirective("DEVELOPMENT",
				"UNITY_IAP", "ADDRESSABLES", "DOTWEEN", "GPGS",
				"IN_APP_REVIEW", "IN_APP_UPDATE", "MAX", "IRONSOURCE",
				"FIREBASE", "FIREBASE_ANALYTICS", "FIREBASE_CRASHLYTICS",
				"FIREBASE_REMOTE_CONFIG", "FIREBASE_AUTH", "FIREBASE_FIRESTORE",
				"FIREBASE_DATABASE", "FIREBASE_STORAGE", "FIREBASE_MESSAGING"
			);
		}

		//==================================

		/// <summary>
		/// This method runs automatically before the first scene loads.
		/// It standardizes the application's culture to "en-US" to ensure consistent
		/// date, time, and number formatting across all devices, preventing localization-related bugs.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void RunOnGameStart()
		{
			var culture = CultureInfo.CreateSpecificCulture("en-US");
			var dateTimeFormat = new DateTimeFormatInfo
			{
				ShortDatePattern = "dd-MM-yyyy",
				LongTimePattern = "HH:mm:ss",
				ShortTimePattern = "HH:mm"
			};
			culture.DateTimeFormat = dateTimeFormat;
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
		}
	}
}