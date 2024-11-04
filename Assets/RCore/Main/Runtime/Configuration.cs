using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using RCore.Editor;
#endif

namespace RCore
{
	[CreateAssetMenu(fileName = "Configuration", menuName = "RCore/Configuration")]
	public class Configuration : ScriptableObject
	{
#region Internal Class

		[Serializable]
		public class Profile
		{
			public string name;
			public List<Directive> defines = new List<Directive>();
			public void AddDirective(string pName, bool defaultActive)
			{
				if (string.IsNullOrEmpty(pName))
					return;
				var find = GetDirective(pName);
				if (find == null)
					defines.Add(new Directive(pName, defaultActive));
			}
			public Directive GetDirective(string pName)
			{
				if (string.IsNullOrEmpty(pName))
					return null;

				for (int i = 0; i < defines.Count; i++)
					if (defines[i].name == pName)
						return defines[i];
				return null;
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

#endregion

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
		public Profile profile = new Profile();
		public RPlayerPrefBool enableLogSystem;
		[SerializeField] private bool m_enableLog;
		[SerializeField] private bool m_enableDraw;

		private void Init()
		{
			enableLogSystem = new RPlayerPrefBool("EnableLogSystem");
			RCore.Debug.enabled = m_enableLog;
			RCore.DebugDraw.enabled = m_enableDraw;
		}
		public bool EnableLog
		{
			get => m_enableLog || enableLogSystem.Value;
			set
			{
				if (m_enableLog == value)
					return;
				m_enableLog = value;
				RCore.Debug.enabled = value;
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