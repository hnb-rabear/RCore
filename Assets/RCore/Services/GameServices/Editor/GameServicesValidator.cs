using RCore.Editor;
using RCore.Service;
using UnityEditor;
using System;

namespace RCore.Services.GameServices.Editor
{
	/// <summary>
	/// Automatically handles GameServices directives based on available classes and SDKs.
	/// </summary>
	[InitializeOnLoad]
	public static class GameServicesValidator
	{
		private const string MENU_ITEM = "Directives/Toggle GameServices Directives Validator";
		private static REditorPrefBool m_Active;

		static GameServicesValidator()
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
			Validate_GPGS();
			Validate_IN_APP_REVIEW();
			Validate_IN_APP_UPDATE();
		}

		private static void Validate_GPGS()
		{
			var rcore = IsClassAvailable("RCore.Service.GameServices");
			var gpgsType = Type.GetType("GooglePlayGames.PlayGamesPlatform, Google.Play.Games");
			if (gpgsType != null && rcore)
				EditorHelper.AddDirective("GPGS");
			else
				EditorHelper.RemoveDirective("GPGS");
		}

		private static void Validate_IN_APP_REVIEW()
		{
			var rcore = IsClassAvailable("RCore.Service.GameServices");
			var inAppReviewType = Type.GetType("Google.Play.Review.ReviewManager, Google.Play.Review");
			if (inAppReviewType != null && rcore)
				EditorHelper.AddDirective("IN_APP_REVIEW");
			else
				EditorHelper.RemoveDirective("IN_APP_REVIEW");
		}

		private static void Validate_IN_APP_UPDATE()
		{
			var rcore = IsClassAvailable("RCore.Service.GameServices");
			var inAppUpdateType = Type.GetType("Google.Play.AppUpdate.AppUpdateManager, Google.Play.AppUpdate");
			if (inAppUpdateType != null && rcore)
				EditorHelper.AddDirective("IN_APP_UPDATE");
			else
				EditorHelper.RemoveDirective("IN_APP_UPDATE");
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