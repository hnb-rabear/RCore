using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	public static class JObjectDBMenu
	{
		[MenuItem("RCore/JObjectDB/JObjectDB Editor %_#_'", priority = RMenu.GROUP_4 + 1)]
		private static void OpenDataWindow()
		{
			JObjectDBWindow.ShowWindow();
		}

		[MenuItem("RCore/JObjectDB/Clear", priority = RMenu.GROUP_4 + 2)]
		private static void ClearSaveData()
		{
			if (EditorHelper.ConfirmPopup())
				JObjectDB.DeleteAll();
		}

		[MenuItem("RCore/JObjectDB/Backup", priority = RMenu.GROUP_4 + 3)]
		private static void BackUpData()
		{
			string path = EditorUtility.SaveFilePanelInProject("Save Backup", "PlayerData_" + System.DateTime.Now.ToString().Replace("/", "_").Replace(":", "_")
				+ ".txt", "txt", "Please enter a file name to save!");
			if (!string.IsNullOrEmpty(path))
				JObjectDB.Backup(path);
		}

		[MenuItem("RCore/JObjectDB/Restore", priority = RMenu.GROUP_4 + 4)]
		private static void RestoreData()
		{
			string path = EditorUtility.OpenFilePanel("Select Backup Data File", Application.dataPath, "txt");
			if (!string.IsNullOrEmpty(path))
				JObjectDB.Restore(path);
		}

		[MenuItem("RCore/JObjectDB/Copy All", priority = RMenu.GROUP_4 + 5)]
		private static void LogData()
		{
			JObjectDB.CopyAllData();
		}
	}
}