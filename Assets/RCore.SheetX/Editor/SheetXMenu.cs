using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.SheetX
{
	public static class SheetXMenu
	{
		public const int GROUP_6 = 100;

#if ASSETS_STORE
		[MenuItem("Window/SheetX/Settings")]
#else
		[MenuItem("RCore/Tools/SheetX/Settings", priority = GROUP_6 + 6)]
#endif
		public static void ShowSheetXSettingsWindow()
		{
			SheetXSettingsWindow.ShowWindow();
		}

#if ASSETS_STORE
		[MenuItem("Window/SheetX/Google Sheets Exporter")]
#else
		[MenuItem("RCore/Tools/SheetX/Google Sheets Exporter", priority = GROUP_6 + 6)]
#endif
		public static void ShowGoogleSheetXWindow()
		{
			GoogleSheetXWindow.ShowWindow();
		}

#if ASSETS_STORE
		[MenuItem("Window/SheetX/Excel Sheets Exporter")]
#else
		[MenuItem("RCore/Tools/SheetX/Excel Sheets Exporter", priority = GROUP_6 + 6)]
#endif
		public static void ShowExcelSheetXWindow()
		{
			ExcelSheetXWindow.ShowWindow();
		}
	}
}