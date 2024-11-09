/***
 * Author RaBear - HNB - 2019
 **/

using RCore.Data.KeyValue;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.KeyValue
{
	public static class KeyValueDBMenu
	{
		[MenuItem("RCore/KeyValueDB/KeyValueDB Editor %_#_'", priority = RMenu.GROUP_4 + 1)]
		private static void OpenDataWindow()
		{
			var window = EditorWindow.GetWindow<KeyValueDBWindow>("KeyValueDB", true);
			window.Show();
		}

		[MenuItem("RCore/KeyValueDB/Clear", priority = RMenu.GROUP_4 + 2)]
		private static void ClearSaveData()
		{
			if (EditorHelper.ConfirmPopup())
				KeyValueDB.DeleteAll();
		}

		[MenuItem("RCore/KeyValueDB/Backup", priority = RMenu.GROUP_4 + 3)]
		private static void BackUpData()
		{
			string fileName = "PlayerData_" + System.DateTime.Now.ToString().Replace("/", "_").Replace(":", "_");
			string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "txt");
			if (!string.IsNullOrEmpty(path))
				KeyValueDB.BackupData(path);
		}

		[MenuItem("RCore/KeyValueDB/Restore", priority = RMenu.GROUP_4 + 4)]
		private static void RestoreData()
		{
			string path = EditorUtility.OpenFilePanel("Select Backup Data File", Application.dataPath, "txt");
			if (!string.IsNullOrEmpty(path))
				KeyValueDB.RestoreData(path);
		}

		[MenuItem("RCore/KeyValueDB/Log", priority = RMenu.GROUP_4 + 5)]
		private static void LogData()
		{
			KeyValueDB.LogData();
		}
	}
}