using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Service
{
#if UNITY_EDITOR
	public static class RMenu
	{
		public const int GROUP_0 = 0;
		public const int GROUP_2 = 20;
		public const int GROUP_4 = 40;
		public const int GROUP_6 = 60;
		public const int GROUP_8 = 80;
		public const int GROUP_10 = 100;
		public const int GROUP_12 = 120;
		public const int GROUP_14 = 140;
		public const int GROUP_16 = 160;
		public const int GROUP_18 = 180;

		public const string GAMEOBJECT_R = "GameObject/RCore/";
		public const string GAMEOBJECT_R_CREATE = "GameObject/RCore/Create/";
		public const string GAMEOBJECT_R_UI = "GameObject/RCore/UI/";

		public const string R_ASSETS = "Assets/RCore/";
		public const string R_TOOLS = "RCore/Tools/";
		public const string R_EXPLORER = "RCore/Explorer/";

		private const string ALT = "&";
		private const string SHIFT = "#";
		private const string CTRL = "%";
	}
#endif
}