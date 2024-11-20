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
		public const int GROUP_6 = 100;

		[MenuItem("RCore/Tools/Archive/.env Editor", priority = GROUP_6 + 11)]
		public static void OpenEnvEditor()
		{
			ENVWindow.ShowWindow();
		}

		[MenuItem("RCore/Tools/Archive/Builder", priority = GROUP_6 + 12)]
		public static void OpenBuilder()
		{
			BuilderWindow.ShowWindow();
		}

		[MenuItem("RCore/Tools/Archive/Swap Sprite", priority = GROUP_6 + 13)]
		public static void OpenSwapSpriteWindow()
		{
			SwapSpriteWindow.ShowWindow();
		}
	}
}