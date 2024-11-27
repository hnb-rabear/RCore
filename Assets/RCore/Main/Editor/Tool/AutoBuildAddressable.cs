using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets.Settings;

namespace RCore.Editor.Tool
{
	public class PreBuildProcessor : IPreprocessBuildWithReport
	{
		private static REditorPrefBool m_AutoBuildAddressables;
		private static REditorPrefBool m_AutoBuildPlayAssetDelivery;

		public int callbackOrder => 0;

		static PreBuildProcessor()
		{
			m_AutoBuildAddressables = new REditorPrefBool(nameof(m_AutoBuildAddressables));
			m_AutoBuildPlayAssetDelivery = new REditorPrefBool(nameof(m_AutoBuildPlayAssetDelivery));
		}

		public void OnPreprocessBuild(BuildReport report)
		{
			if (m_AutoBuildAddressables.Value
			    || m_AutoBuildPlayAssetDelivery.Value)
			{
				Debug.Log("Clean Addressables");
				AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
			}
			
			if (m_AutoBuildAddressables.Value)
			{
				Debug.Log("Starting Addressables build...");
				AddressableAssetSettings.BuildPlayerContent();
			}
			
			if (m_AutoBuildPlayAssetDelivery.Value) { }
		}

		[MenuItem(RMenu.R_TOOLS + "Toggle Auto Build Addressables")]
		private static void ToggleAction()
		{
			m_AutoBuildAddressables.Value = !m_AutoBuildAddressables.Value;
		}

		[MenuItem(RMenu.R_TOOLS + "Toggle Auto Build Addressables", true)]
		private static bool ToggleValidate()
		{
			Menu.SetChecked(RMenu.R_TOOLS + "Toggle Auto Build Addressables", m_AutoBuildAddressables.Value);
			return true;
		}
	}
}