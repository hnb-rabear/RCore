/**
 * Author HNB-RaBear - 2024
 **/

using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// Menu items for managing the JObject Database (Editor Window, Clear, Backup, Restore, Log).
	/// </summary>
	public static class JObjectDBMenu
	{
		private const string ALT = "&";
		private const string SHIFT = "#";
		private const string CTRL = "%";
		
		[MenuItem("RCore/JObject Database/JObjectDB Editor " + CTRL + "_" + ALT + "_n", priority = RMenu.GROUP_6 + 1)]
		private static void OpenDataWindow()
		{
			JObjectDBWindow.ShowWindow();
		}

		[MenuItem("RCore/JObject Database/Clear", priority = RMenu.GROUP_6 + 2)]
		private static void ClearSaveData()
		{
			if (EditorHelper.ConfirmPopup())
				JObjectDB.DeleteAll();
		}

		[MenuItem("RCore/JObject Database/Backup", priority = RMenu.GROUP_6 + 3)]
		private static void BackUpData()
		{
			string fileName = "PlayerData_" + System.DateTime.Now.ToString().Replace("/", "_").Replace(":", "_");
			string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "txt");
			if (!string.IsNullOrEmpty(path))
				JObjectDB.Backup(path);
		}

		[MenuItem("RCore/JObject Database/Restore", priority = RMenu.GROUP_6 + 4)]
		private static void RestoreData()
		{
			string path = EditorUtility.OpenFilePanel("Select Backup Data File", Application.dataPath, "txt");
			if (!string.IsNullOrEmpty(path))
				JObjectDB.Restore(path);
		}

		[MenuItem("RCore/JObject Database/Copy All", priority = RMenu.GROUP_6 + 5)]
		private static void LogData()
		{
			JObjectDB.CopyAllData();
		}
	}
}