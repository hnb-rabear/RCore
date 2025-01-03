using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Service
{
	public static class RMenu
	{
		public const int GROUP_1 = 0;
		public const int GROUP_2 = 20;
		public const int GROUP_3 = 40;
		public const int GROUP_4 = 60;
		public const int GROUP_5 = 80;
		public const int GROUP_6 = 100;

		public const string GAMEOBJECT_R = "GameObject/RCore/";
		public const string GAMEOBJECT_R_CREATE = "GameObject/RCore/Create/";
		public const string GAMEOBJECT_R_UI = "GameObject/RCore/UI/";

		public const string R_ASSETS = "Assets/RCore/";
		public const string R_TOOLS = "RCore/Tools/";
		public const string R_EXPLORER = "RCore/Explorer/";

		private const string ALT = "&";
		private const string SHIFT = "#";
		private const string CTRL = "%";

		[MenuItem(GAMEOBJECT_R_CREATE + "IAPManager", priority = GROUP_2 + 1)]
		public static void CreateIAPManager()
		{
			var gameObject = new GameObject("IAPManager");
			gameObject.AddComponent<IAPManager>();
		}
		
		[MenuItem(GAMEOBJECT_R_CREATE + "NotificationsManager", priority = GROUP_2 + 1)]
		public static void CreateNotificationsManager()
		{
			var gameObject = new GameObject("NotificationsManager");
			gameObject.AddComponent<NotificationsManager>();
		}
	}
}