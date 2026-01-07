using RCore.Editor;
using RCore.Service;
using UnityEditor;
using System;

namespace RCore.Services.Notifications.Editor
{
	/// <summary>
	/// Automatically handles Notification directives based on available classes and SDKs.
	/// </summary>
	[InitializeOnLoad]
	public static class NotificationValidator
	{
		private const string MENU_ITEM = "Directives/Toggle Notification Directives Validator";
		private static REditorPrefBool m_Active;

		static NotificationValidator()
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
			Validate_UNITY_NOTIFICATION();
		}

		private static void Validate_UNITY_NOTIFICATION()
		{
			var rcore = IsClassAvailable("RCore.Service.NotificationsManager");
			var iapType = Type.GetType("Unity.Notifications.NotificationCenter, Unity.Notifications.Unified");
			if (iapType != null && rcore)
				EditorHelper.AddDirective("UNITY_NOTIFICATION");
			else
				EditorHelper.RemoveDirective("UNITY_NOTIFICATION");
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
	    [MenuItem(RMenu.GAMEOBJECT_R_CREATE + "NotificationsManager", priority = RMenu.GROUP_2 + 1)]
	    public static void CreateNotificationsManager()
	    {
		    var gameObject = new UnityEngine.GameObject("NotificationsManager");
		    gameObject.AddComponent<RCore.Service.NotificationsManager>();
	    }
	}
}