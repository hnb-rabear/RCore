/***
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com 
 **/

using UnityEditor;
using UnityEngine;
using RCore.Common;

namespace RCore.Framework.Data
{
    public static class DataMenuTools
    {
        [MenuItem("RCore/Data/Open Data Window %_#_'")]
        private static void OpenDataWindow()
        {
            var window = EditorWindow.GetWindow<DataWindow>("Game Data", true);
            window.Show();
		}

		[MenuItem("RCore/Data/Clear PlayerPrefs")]
		private static void ClearPlayerPrefs()
		{
			if (EditorHelper.ConfirmPopup("Clear PlayerPrefs"))
				PlayerPrefs.DeleteAll();
		}

        /*
        [MenuItem("RCore/Data/Clear Game Data")]
        private static void ClearSaveData()
        {
            EditorHelper.ConfirmPopup(() => { DataSaverContainer.DeleteAll(); });
        }

        [MenuItem("RCore/Data/Backup Game Data")]
        private static void BackUpData()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Backup", "GameData_" + System.DateTime.Now.ToString().Replace("/", "_").Replace(":", "_")
                + ".txt", "txt", "Please enter a file name to save!");
            if (!string.IsNullOrEmpty(path))
            {
                DataSaverContainer.BackupData(path);
            }
        }

        [MenuItem("RCore/Data/Restore Game Data")]
        private static void RestoreData()
        {
            string path = EditorUtility.OpenFilePanel("Select Backup Data File", Application.dataPath, "txt");
            if (!string.IsNullOrEmpty(path))
            {
                DataSaverContainer.RestoreData(path);
            }
        }

        [MenuItem("RCore/Data/Log Game Data")]
        private static void LogData()
        {
            DataSaverContainer.LogData();
        }

        [MenuItem("RCore/Data/Save Game Data (In Game)")]
        private static void Save()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("This Function should be called in Playing!");
                return;
            }
            DataManager.Instance.Save(true);
        }
        */
    }
}