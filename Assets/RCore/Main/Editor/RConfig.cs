using RCore.Editor;
using UnityEngine;

namespace RCore
{
	public class RConfig : ScriptableObject
	{
		private const string FILE_PATH = "Resources/RConfig.asset";
		
		private static RConfig m_Instance;
		public static RConfig Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = Resources.Load<RConfig>(nameof(RConfig));
#if UNITY_EDITOR
				if (m_Instance == null)
					m_Instance = EditorHelper.CreateScriptableAsset<RConfig>($"Assets/{FILE_PATH}");
#endif
				return m_Instance;
			}
		}
#if UNITY_EDITOR
		public string tinyPngApiKey = "";
#endif
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