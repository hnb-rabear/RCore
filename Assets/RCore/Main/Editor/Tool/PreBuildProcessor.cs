using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets.Settings;

namespace RCore.Editor.Tool
{
	public class PreBuildProcessor : IPreprocessBuildWithReport
	{
		private const string MENU_ADDRESSABLES = "Toggle Auto Build Addressables";
		private const string MENU_PAD = "Toggle Auto Build PAD";

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

			if (m_AutoBuildPlayAssetDelivery.Value)
			{
				// Add your Play Asset Delivery build process here if required.
				//TODO: find a way to build PAD
			}
		}

		[MenuItem(RMenu.R_TOOLS + MENU_ADDRESSABLES)]
		private static void ToggleAutoBuildAddressables()
		{
			m_AutoBuildAddressables.Value = !m_AutoBuildAddressables.Value;
		}

		[MenuItem(RMenu.R_TOOLS + MENU_ADDRESSABLES, true)]
		private static bool ToggleValidateAutoBuildAddressables()
		{
			Menu.SetChecked(RMenu.R_TOOLS + MENU_ADDRESSABLES, m_AutoBuildAddressables.Value);
			return true;
		}
		
		// [MenuItem(RMenu.R_TOOLS + MENU_PAD)]
		// private static void ToggleAutoBuildPAD()
		// {
		// 	m_AutoBuildPlayAssetDelivery.Value = !m_AutoBuildPlayAssetDelivery.Value;
		// }
		//
		// [MenuItem(RMenu.R_TOOLS + MENU_PAD, true)]
		// private static bool ToggleValidateAutoBuildPAD()
		// {
		// 	Menu.SetChecked(RMenu.R_TOOLS + MENU_PAD, m_AutoBuildPlayAssetDelivery.Value);
		// 	return true;
		// }
	}
}