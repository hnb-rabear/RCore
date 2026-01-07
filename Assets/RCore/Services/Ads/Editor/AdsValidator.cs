using RCore.Editor;
using RCore.Service;
using UnityEditor;
using System;

namespace RCore.Services.Ads.Editor
{
	/// <summary>
	/// Automatically managing defining symbols based on the existence of specific types in the project.
	/// </summary>
	[InitializeOnLoad]
	public static class AdsValidator
	{
		private const string MENU_ITEM = "Directives/Toggle Ads Directives Validator";
		private static REditorPrefBool m_Active;

		static AdsValidator()
		{
			m_Active = new REditorPrefBool(MENU_ITEM);
			EditorApplication.update += RunOnEditorStart;
		}

		private static void RunOnEditorStart()
		{
			if (m_Active.Value)
				Validate();
		}

		private static void Validate()
		{
			Validate_ADMOB();
			Validate_APPLOVIN();
		}

		private static void Validate_ADMOB()
		{
			var googleMobileAdsType = Type.GetType("GoogleMobileAds.Api.MobileAds, GoogleMobileAds");
			if (googleMobileAdsType != null)
				EditorHelper.AddDirective("ADMOB");
			else
				EditorHelper.RemoveDirective("ADMOB");
		}

		private static void Validate_APPLOVIN()
		{
			var rcore = IsClassAvailable("RCore.Service.ApplovinProvider");
			var appLovinType = Type.GetType("MaxSdk, MaxSdk.Scripts");
			if (appLovinType != null && rcore)
				EditorHelper.AddDirective("MAX");
			else
				EditorHelper.RemoveDirective("MAX");
		}
		
		private static bool IsClassAvailable(string className)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var type = assembly.GetType(className);
				if (type != null)
					return true;
			}
			return false;
		}

		[MenuItem(RMenu.R_TOOLS + MENU_ITEM)]
		private static void ToggleActive()
		{
			m_Active.Value = !m_Active.Value;
			if (m_Active.Value)
				Validate();
		}

		[MenuItem(RMenu.R_TOOLS + MENU_ITEM, true)]
		private static bool ToggleActiveValidate()
		{
			Menu.SetChecked(RMenu.R_TOOLS + MENU_ITEM, m_Active.Value);
			return true;
		}
	}
}