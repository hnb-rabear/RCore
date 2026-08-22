using System.Reflection;
using NUnit.Framework;
using RCore.RAssetFilter.Editor;
using UnityEditor;

namespace RCore.RAssetFilter.Tests
{
	public class RAssetFilterSettingsTests
	{
		private const string NEW_KEY = "RCore.RAssetFilter.Settings";
		private const string OLD_KEY = "RCore.AssetCleaner.Settings";

		private bool m_hadNewKey;
		private string m_originalNewValue;
		private bool m_hadOldKey;
		private string m_originalOldValue;

		[SetUp]
		public void SetUp()
		{
			Assert.That(AssetDatabase.FindAssets("t:RAssetFilterSettings"), Is.Empty,
				"Settings migration test requires no RAssetFilterSettings asset in this project.");
			m_hadNewKey = EditorPrefs.HasKey(NEW_KEY);
			m_originalNewValue = m_hadNewKey ? EditorPrefs.GetString(NEW_KEY) : null;
			m_hadOldKey = EditorPrefs.HasKey(OLD_KEY);
			m_originalOldValue = m_hadOldKey ? EditorPrefs.GetString(OLD_KEY) : null;
			EditorPrefs.DeleteKey(NEW_KEY);
			EditorPrefs.DeleteKey(OLD_KEY);
			ResetSingleton();
		}

		[TearDown]
		public void TearDown()
		{
			ResetSingleton();
			RestoreKey(NEW_KEY, m_hadNewKey, m_originalNewValue);
			RestoreKey(OLD_KEY, m_hadOldKey, m_originalOldValue);
		}

		[Test]
		public void instance_migrates_old_settings_key_without_deleting_it()
		{
			EditorPrefs.SetString(OLD_KEY, "{\"showSize\":false,\"deepSearch\":true}");

			var settings = RAssetFilterSettings.Instance;

			Assert.That(settings.showSize, Is.False);
			Assert.That(settings.deepSearch, Is.True);
			Assert.That(EditorPrefs.HasKey(NEW_KEY), Is.True);
			Assert.That(EditorPrefs.GetString(NEW_KEY), Does.Contain("\"showSize\":false"));
			Assert.That(EditorPrefs.GetString(OLD_KEY), Is.EqualTo("{\"showSize\":false,\"deepSearch\":true}"));
		}

		[Test]
		public void instance_prefers_new_key_and_invalid_json_keeps_defaults()
		{
			EditorPrefs.SetString(OLD_KEY, "{\"showSize\":false}");
			EditorPrefs.SetString(NEW_KEY, "not json");

			var settings = RAssetFilterSettings.Instance;

			Assert.That(settings.showSize, Is.True);
			Assert.That(EditorPrefs.GetString(OLD_KEY), Is.EqualTo("{\"showSize\":false}"));
		}

		private static void ResetSingleton()
		{
			var type = typeof(RAssetFilterSettings);
			type.GetField("m_instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
			type.GetField("m_instanceIsAsset", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, false);
		}

		private static void RestoreKey(string pKey, bool pHadKey, string pValue)
		{
			if (pHadKey)
				EditorPrefs.SetString(pKey, pValue);
			else
				EditorPrefs.DeleteKey(pKey);
		}
	}
}
