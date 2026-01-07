using RCore.Editor;
using RCore.Service;
using UnityEditor;
using System;

namespace RCore.Services.IAP.Editor
{
	/// <summary>
	/// Automatically handles IAP directives based on available classes and SDKs.
	/// </summary>
	[InitializeOnLoad]
	public static class IAPValidator
	{
		private const string MENU_ITEM = "Directives/Toggle IAP Directives Validator";
		private static REditorPrefBool m_Active;

		static IAPValidator()
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
			Validate_UNITY_IAP();
		}

		private static void Validate_UNITY_IAP()
		{
			var rcore = IsClassAvailable("RCore.Service.IAPManager");
			var iapType = Type.GetType("UnityEngine.Purchasing.ConfigurationBuilder, UnityEngine.Purchasing");
			if (iapType != null && rcore)
				EditorHelper.AddDirective("UNITY_IAP");
			else
				EditorHelper.RemoveDirective("UNITY_IAP");
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
	    [MenuItem(RMenu.GAMEOBJECT_R_CREATE + "IAPManager", priority = RMenu.GROUP_2 + 1)]
	    public static void CreateIAPManager()
	    {
		    var gameObject = new UnityEngine.GameObject("IAPManager");
		    gameObject.AddComponent<RCore.Service.IAPManager>();
	    }
	}
}