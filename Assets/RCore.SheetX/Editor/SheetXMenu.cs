using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.SheetX
{
	public static class SheetXMenu
	{
		public const int GROUP_6 = 100;
		
		[MenuItem("RCore/Tools/SheetX/Excel Sheets Exporter", priority = GROUP_6 + 6)]
		// [MenuItem("Window/SheetX/Settings")]
		public static void ShowSheetXSettingsWindow()
		{
			SheetXSettingsWindow.ShowWindow();
		}
		
		[MenuItem("RCore/Tools/SheetX/Google Sheets Exporter", priority = GROUP_6 + 6)]
		// [MenuItem("Window/SheetX/Google Sheets Exporter")]
		public static void ShowGoogleSheetXWindow()
		{
			GoogleSheetXWindow.ShowWindow();
		}
		
		[MenuItem("RCore/Tools/SheetX/Settings", priority = GROUP_6 + 6)]
		// [MenuItem("Window/SheetX/Excel Sheets Exporter")]
		public static void ShowExcelSheetXWindow()
		{
			ExcelSheetXWindow.ShowWindow();
		}
	}
}