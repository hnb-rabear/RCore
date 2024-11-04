using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using RCore.Editor;
#endif

namespace RCore
{
	[CreateAssetMenu(fileName = "Configuration", menuName = "RCore/Configuration")]
	public class Configuration : ScriptableObject
	{
		[Serializable]
		public class Env
		{
			public string name;
			public List<Directive> directives = new();
			public void AddDirective(string pName, bool defaultActive)
			{
				if (string.IsNullOrEmpty(pName))
					return;
				var find = GetDirective(pName);
				if (find == null)
					directives.Add(new Directive(pName, defaultActive));
			}
			public Env AddDirective(params string[] pNames)
			{
				foreach (string pName in pNames)
				{
					if (string.IsNullOrEmpty(pName))
						continue;
					bool exist = directives.Any(directive => directive.name == pName);
					if (!exist)
						directives.Add(new Directive(pName, false));
				}
				return this;
			}
			public Directive GetDirective(string pName)
			{
				return string.IsNullOrEmpty(pName) ? null : directives.FirstOrDefault(directive => directive.name == pName);

			}
		}

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
						EditorHelper.CreateScriptableAsset<Configuration>("Assets/Resources/Configuration.asset");
						m_Instance = Resources.Load<Configuration>(nameof(Configuration));
					}
#endif
					m_Instance.Init();
				}
				return m_Instance;
			}
		}

		public Action onSettingsChanged;
		public SerializableDictionary<string, string> customKeys;
		public List<Env> envs = new List<Env>();
		public Env curEnv = new Env();

		[SerializeField] private bool m_enableLog;
		[SerializeField] private bool m_enableDraw;

		private RPlayerPrefBool m_enableLogSystem;
		
		public bool EnableLog
		{
			get => m_enableLog || m_enableLogSystem.Value;
			set
			{
				if (m_enableLog == value)
					return;
				m_enableLog = value;
				Debug.enabled = value;
				onSettingsChanged?.Invoke();
			}
		}
		public bool EnableDraw
		{
			get => m_enableDraw;
			set
			{
				if (m_enableDraw == value)
					return;
				m_enableDraw = value;
				DebugDraw.enabled = value;
				onSettingsChanged?.Invoke();
			}
		}

		private void Init()
		{
			m_enableLogSystem = new RPlayerPrefBool("EnableLogSystem");
			Debug.enabled = m_enableLog;
			DebugDraw.enabled = m_enableDraw;
		}

		private void OnValidate()
		{
			if (envs.Count == 0 || envs[0].name != "do_not_remove")
			{
				envs.Insert(0, new Env()
				{
					name = "do_not_remove",
				});
			}
			envs[0].AddDirective("DOTWEEN",
				"GPGS",
				"IN_APP_REVIEW",
				"IN_APP_UPDATE",
				"APPLOVIN",
				"IRONSOURCE",
				"FIREBASE",
				"FIREBASE_ANALYTICS",
				"FIREBASE_CRASHLYTICS",
				"FIREBASE_REMOTE_CONFIG",
				"FIREBASE_AUTH",
				"FIREBASE_FIRESTORE",
				"FIREBASE_DATABASE",
				"FIREBASE_STORAGE",
				"FIREBASE_MESSAGING"
			);
		}

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