using System;
using System.Collections.Generic;
using UnityEngine;
using RCore.Inspector;
using RCore.Common;
using System.Linq;

[CreateAssetMenu(fileName = "DevSetting", menuName = "RCore/Dev Setting")]
public class DevSetting : ScriptableObject
{
#region Internal Class

	private static readonly string[] Directives = {
		"DEVELOPMENT",
		"UNITY_IAP",
		"UNITY_MONETIZATION",
		"ADDRESSABLES",
		"USE_DOTWEEN",
		"GPGS",
		"ACTIVE_FIREBASE",
		"ACTIVE_FIREBASE_AUTH",
		"ACTIVE_FIREBASE_STORAGE",
		"ACTIVE_FIREBASE_DATABASE",
		"ACTIVE_FIREBASE_MESSAGING",
		"ACTIVE_FIREBASE_CRASHLYTICS",
		"ACTIVE_FIREBASE_ANALYTICS",
		"ACTIVE_FIREBASE_FIRESTORE",
		"ACTIVE_FIREBASE_REMOTE",
		"IRONSOURCE",
		"APPLOVINE",
	};

	[Serializable]
	public class Profile
	{
		[ReadOnly] public string name;
		public bool enableLog;
		public bool enableDraw;
		public bool showFPS;
		public List<Directive> defines = new List<Directive>();

		public void ValidateDirectives()
		{
			defines ??= new List<Directive>();
			foreach (string directive in Directives)
				AddDirective(directive, false);
			defines.Sort();
			defines.RemoveAll(x => string.IsNullOrEmpty(x.name));
		}

		private bool ContainDirective(string pName)
		{
			if (string.IsNullOrEmpty(pName))
				return true;

			return defines.Any(t => t.name == pName);
		}

		public void AddDirective(string pDirective, bool pActive = true)
		{
			if (defines.All(t => t.name != pDirective))
				defines.Add(new Directive(pDirective, pActive));
		}

		public void InitDirectives(string[] currentDirectives)
		{
			for (int i = 0; i < currentDirectives.Length; i++)
			{
				AddDirective(currentDirectives[i]);
			}

			for (int i = 0; i < defines.Count; i++)
			{
				if (currentDirectives.Length > 0)
				{
					bool exist = false;
					for (int j = 0; j < currentDirectives.Length; j++)
					{
						if (currentDirectives[j] == defines[i].name)
							exist = true;
					}
					defines[i].enabled = exist;
				}
				else
					defines[i].enabled = false;
			}
		}
	}

	[Serializable]
	public class Directive : IComparable<Directive>
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
		public bool IsFixed()
		{
			return Directives.Contains(name);
		}
		public int CompareTo(Directive other)
		{
			if (IsFixed() && !other.IsFixed())
				return -1;
			if (!IsFixed() && other.IsFixed())
				return 1;
			return String.Compare(name, other.name, StringComparison.Ordinal);
		}
	}

#endregion

	//==================================

	private static DevSetting mInstance;

	public static DevSetting Instance
	{
		get
		{
			if (mInstance == null)
			{
				mInstance = Resources.Load<DevSetting>("DevSetting");
				mInstance.enableLogSystem = new PlayerPrefBool("EnableLogSystem");
#if DEVELOPMENT
                mInstance.enableLogSystem.Value = true;
#endif
			}
			return mInstance;
		}
	}

	public Action onSettingsChanged;
	public Profile profile = new Profile();
	public PlayerPrefBool enableLogSystem;

	public bool EnableLog
	{
		get => profile.enableLog || enableLogSystem.Value;
		set
		{
			profile.enableLog = value;
			onSettingsChanged?.Invoke();
		}
	}

	public bool EnableDraw
	{
		get => profile.enableDraw;
		set
		{
			profile.enableDraw = value;
			onSettingsChanged?.Invoke();
		}
	}

	public bool ShowFPS
	{
		get => profile.showFPS;
		set
		{
			profile.showFPS = value;
			onSettingsChanged?.Invoke();
		}
	}
}