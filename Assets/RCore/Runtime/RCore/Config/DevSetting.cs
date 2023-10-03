using System;
using System.Collections.Generic;
using UnityEngine;
using RCore.Inspector;
using Debug = UnityEngine.Debug;
using RCore.Common;

[CreateAssetMenu(fileName = "DevSetting", menuName = "RCore/Dev Setting")]
public class DevSetting : ScriptableObject
{
#region Internal Class

	[Serializable]
	public class Profile
	{
		public string name;
		public List<Directive> defines;
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

	private static DevSetting m_Instance;
	public static DevSetting Instance
	{
		get
		{
			if (m_Instance == null)
			{
				m_Instance = Resources.Load<DevSetting>("DevSetting");
				m_Instance.enableLogSystem = new PlayerPrefBool("EnableLogSystem");
			}
			return m_Instance;
		}
	}

	public Action onSettingsChanged;
	public Profile profile = new Profile();
	public PlayerPrefBool enableLogSystem;
	public bool enableLog;
	public bool enableDraw;
	public bool showFPS;

	public bool EnableLog
	{
		get => enableLog || enableLogSystem.Value;
		set
		{
			enableLog = value;
			onSettingsChanged?.Invoke();
		}
	}
	public bool EnableDraw
	{
		get => enableDraw;
		set
		{
			enableDraw = value;
			onSettingsChanged?.Invoke();
		}
	}
	public bool ShowFPS
	{
		get => showFPS;
		set
		{
			showFPS = value;
			onSettingsChanged?.Invoke();
		}
	}
}