using System.Collections.Generic;
using System.IO;
using RCore.Editor.Tool;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace RCore.Editor
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

		[MenuItem(R_TOOLS + "Archive/.env Editor", priority = GROUP_6 + 11)]
		public static void OpenEnvEditor()
		{
			ENVWindow.ShowWindow();
		}

		[MenuItem(R_TOOLS + "Archive/Builder", priority = GROUP_6 + 12)]
		public static void OpenBuilder()
		{
			BuilderWindow.ShowWindow();
		}

		[MenuItem(R_TOOLS + "Archive/Swap Sprite", priority = GROUP_6 + 13)]
		public static void OpenSwapSpriteWindow()
		{
			SwapSpriteWindow.ShowWindow();
		}
	}
}