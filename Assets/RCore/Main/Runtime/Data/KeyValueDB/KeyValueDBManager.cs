/***
 * Author HNB-RaBear - 2018
 **/

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Data.KeyValue
{
	public class KeyValueDBManager : MonoBehaviour
	{
		private const float MIN_TIME_BETWEEN_SAVES = 5;

		private bool m_EnabledAutoSave = true;
		/// <summary>
		/// Key is saver id string
		/// Children are first generations of this saver
		/// </summary>
		private Dictionary<string, List<DataGroup>> m_mainGroups = new Dictionary<string, List<DataGroup>>();
		private Coroutine m_saveCoroutine;

		//===========================================

		public void OnApplicationPause(bool paused)
		{
			foreach (var item in m_mainGroups)
				foreach (var g in item.Value)
					g.OnApplicationPaused(paused);

			if (paused)
				Save(true);
		}

		public void OnApplicationQuit()
		{
			foreach (var item in m_mainGroups)
				foreach (var g in item.Value)
					g.OnApplicationQuit();
			Save(true);
		}

		//===========================================

		/// <summary>
		/// Step 0: Preparation, add all main data groups to the manager
		/// </summary>
		protected T AddMainDataGroup<T>(T pDataGroup, KeyValueCollection pSaver) where T : DataGroup
		{
			if (!m_mainGroups.ContainsKey(pSaver.idString))
				m_mainGroups.Add(pSaver.idString, new List<DataGroup>());
			else
			{
				var list = m_mainGroups[pSaver.idString];
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].Id == pDataGroup.Id)
						Debug.LogError($"Main Data Group with id {pDataGroup.Id} is already existed");
				}
			}
			m_mainGroups[pSaver.idString].Add(pDataGroup);
			return pDataGroup;
		}

		/// <summary>
		/// Step 1: Init main data groups
		/// </summary>
		public virtual void Init()
		{
			Load();
			PostLoad();
		}

		/// <summary>
		/// Discard all changes, back to last data save
		/// </summary>
		public void Reload()
		{
			foreach (var item in m_mainGroups)
				foreach (var g in item.Value)
					g.Reload();
		}

		public virtual void Save(bool pNow = false)
		{
			if (m_saveCoroutine != null)
				StopCoroutine(m_saveCoroutine);

			if (pNow)
				SaveNow();
			else
				m_saveCoroutine = StartCoroutine(SaveWithDelay(pNow ? 0 : MIN_TIME_BETWEEN_SAVES));
		}

		private IEnumerator SaveWithDelay(float pDelay)
		{
			if (!m_EnabledAutoSave)
				yield break;

			yield return new WaitForSeconds(pDelay);
			SaveNow();
		}

		private void SaveNow()
		{
			var groups = new List<DataGroup>();
			foreach (var item in m_mainGroups)
				foreach (var g in item.Value)
				{
					if (g.Stage())
						groups.Add(g);
				}

			foreach (var g in groups)
				g.KeyValueCollection.Save(false);

			PlayerPrefs.Save();
			PostSave();
		}

		protected virtual void PostSave() { }

		public void Import(string pJsonData)
		{
			KeyValueDB.ImportData(pJsonData);
			Reload();
		}

		public void EnableAutoSave(bool pValue)
		{
			m_EnabledAutoSave = pValue;
		}

		//=================================================

		/// <summary>
		/// Step 2: Load Data Saver
		/// </summary>
		private void Load()
		{
			foreach (var item in m_mainGroups)
				foreach (var g in item.Value)
					g.Load("", item.Key);
		}

		/// <summary>
		/// Step 3: Verify all data
		/// </summary>
		private void PostLoad()
		{
			foreach (var item in m_mainGroups)
				foreach (var g in item.Value)
					g.PostLoad();
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(KeyValueDBManager), true)]
#if ODIN_INSPECTOR
	public class KeyValueDBManagerEditor : Sirenix.OdinInspector.Editor.OdinEditor
#else
	public class KeyValueDBManagerEditor : UnityEditor.Editor
#endif
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Clear"))
			{
				KeyValueDB.DeleteAll();
			}
			if (GUILayout.Button("Back Up"))
			{
				string fileName = "GameData_" + DateTime.Now.ToString().Replace("/", "_").Replace(":", "_");
				string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "txt");
				if (!string.IsNullOrEmpty(path))
					KeyValueDB.BackupData(path);
			}
			if (GUILayout.Button("Restore"))
			{
				string jsonData = EditorUtility.OpenFilePanel("Select Backup Data File", Application.dataPath, "txt");
				if (!string.IsNullOrEmpty(jsonData))
					KeyValueDB.RestoreData(jsonData);
			}
			if (GUILayout.Button("Log"))
			{
				KeyValueDB.LogData();
			}
			if (GUILayout.Button("Save (In Game)"))
			{
				if (!Application.isPlaying)
					return;

				foreach (var saver in KeyValueDB.collections)
					saver.Value.Save(true);
			}
		}
	}
#endif
}