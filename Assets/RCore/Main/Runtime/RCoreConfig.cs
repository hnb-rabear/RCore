using RCore.Editor;
using UnityEngine;

namespace RCore
{
	public class RCoreConfig : ScriptableObject
	{
		private static readonly string m_FilePath = $"Resources/{nameof(RCoreConfig)}.asset";
		
		private static RCoreConfig m_Instance;
		public static RCoreConfig Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = Resources.Load<RCoreConfig>(nameof(RCoreConfig));
#if UNITY_EDITOR
				if (m_Instance == null)
					m_Instance = EditorHelper.CreateScriptableAsset<RCoreConfig>($"Assets/{m_FilePath}");
#endif
				return m_Instance;
			}
		}

#if APPLOVINE
		public string maxSdkKey;
		public string maxInterstitial;
		public string maxRewarded;
		public string maxBanner;
#endif
#if IRONSOURCE
		public string isAndroidAppKey;
		public string isIosAppKey;
#endif
	}
}