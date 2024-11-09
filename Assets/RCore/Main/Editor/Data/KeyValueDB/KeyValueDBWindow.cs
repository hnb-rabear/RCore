/***
 * Author RaBear - HNB - 2020
 **/

using RCore.Data.KeyValue;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EditorPrefs = UnityEditor.EditorPrefs;

namespace RCore.Editor.Data.KeyValue
{
	public class KeyValueDBWindow : EditorWindow
	{
		private Dictionary<string, List<KeyValueSS>> m_dictKeyValues;
		private Vector2 m_scrollPosition;

		private void OnEnable()
		{
			m_dictKeyValues = KeyValueDB.GetAllDataKeyValues();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			var actions = new List<IDraw>();
			actions.Add(new EditorButton()
			{
				label = "Clear",
				onPressed = () =>
				{
					if (EditorHelper.ConfirmPopup())
					{
						KeyValueDB.DeleteAll();
						m_dictKeyValues = KeyValueDB.GetAllDataKeyValues();
						Repaint();
					}
				}
			});
			actions.Add(new EditorButton()
			{
				label = "Back Up",
				onPressed = () =>
				{
					string fileName = "GameData_" + System.DateTime.Now.ToString().Replace("/", "_").Replace(":", "_");
					string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "txt");
					if (!string.IsNullOrEmpty(path))
					{
						KeyValueDB.BackupData(path);
					}
				}
			});
			actions.Add(new EditorButton()
			{
				label = "Restore",
				onPressed = () =>
				{
					string path = EditorUtility.OpenFilePanel("Select Backup Data File", Application.dataPath, "txt");
					if (!string.IsNullOrEmpty(path))
					{
						KeyValueDB.RestoreData(path);
						m_dictKeyValues = KeyValueDB.GetAllDataKeyValues();
						Repaint();
					}
				}
			});
			actions.Add(new EditorButton()
			{
				label = "Log",
				onPressed = KeyValueDB.LogData
			});
			actions.Add(new EditorButton()
			{
				label = "Save (In Game)",
				color = Application.isPlaying ? Color.yellow : Color.grey,
				onPressed = () =>
				{
					if (!Application.isPlaying)
					{
						UnityEngine.Debug.Log("This Function should be called in Playing!");
						return;
					}
					foreach (var saver in KeyValueDB.collections)
						saver.Value.Save(true);
				},
			});
			EditorHelper.GridDraws(2, actions);
			EditorHelper.BoxVertical("Key-Value Pairs", () =>
			{
				foreach (var keyValues in m_dictKeyValues)
				{
					var list = keyValues.Value;
					ListKeyValues(ref list, keyValues.Key);
				}
			}, default, true);
			GUILayout.EndScrollView();
		}

		public void ListKeyValues(ref List<KeyValueSS> pList, string pSaverKey)
		{
			if (pList == null)
				return;

			GUILayout.Space(3);

			var prevColor = GUI.color;
			GUI.backgroundColor = new Color(1, 1, 0.5f);

			var show = EditorHelper.HeaderFoldout($"{pSaverKey} ({pList.Count})", pSaverKey);
			var list = pList;
			if (show)
			{
				int page = EditorPrefs.GetInt(pSaverKey + "_page", 0);
				int totalPages = Mathf.CeilToInt(list.Count * 1f / 20f);
				if (page < 0)
					page = 0;
				int from = page * 20;
				int to = page * 20 + 20 - 1;
				if (to > list.Count - 1)
					to = list.Count - 1;

				EditorHelper.BoxVertical(() =>
				{
					if (totalPages > 0)
					{
						EditorGUILayout.BeginHorizontal();
						if (EditorHelper.Button("<Prev<"))
						{
							if (page > 0) page--;
							EditorPrefs.SetInt(pSaverKey + "_page", page);
						}
						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
						if (EditorHelper.Button(">Next>"))
						{
							if (page < totalPages - 1) page++;
							EditorPrefs.SetInt(pSaverKey + "_page", page);
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorHelper.BoxHorizontal(() =>
					{
						EditorHelper.LabelField("#", 40);
						EditorHelper.LabelField("Key", 100);
						EditorHelper.LabelField("Alias", 100);
						EditorHelper.LabelField("Value", 200);
					});
					for (int i = from; i <= to; i++)
					{
						int i1 = i;
						EditorHelper.BoxHorizontal(() =>
						{
							EditorHelper.LabelField((i1 + 1).ToString(), 40);
							list[i1].Key = EditorHelper.TextField(list[i1].Key, "", 0, 100);
							list[i1].Value = EditorHelper.TextField(list[i1].Value, "", 0, 200);
						});
					}
					if (totalPages > 0)
					{
						EditorGUILayout.BeginHorizontal();
						if (EditorHelper.Button("<Prev<"))
						{
							if (page > 0)
								page--;
							EditorPrefs.SetInt(pSaverKey + "_page", page);
						}
						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
						if (EditorHelper.Button(">Next>"))
						{
							if (page < totalPages - 1)
								page++;
							EditorPrefs.SetInt(pSaverKey + "_page", page);
						}
						EditorGUILayout.EndHorizontal();
					}

					if (EditorHelper.Button("Sort"))
						list.Sort();
					if (Application.isPlaying)
						EditorGUILayout.HelpBox("Cannot Apply Changes In Play Mode", UnityEditor.MessageType.Warning);
					else
					{
						if (EditorHelper.ButtonColor("Apply Changes", Color.green))
						{
							string dataStr = JsonHelper.ToJson(list);
							KeyValueDB.SetData(pSaverKey, dataStr);
							m_dictKeyValues = KeyValueDB.GetAllDataKeyValues();
						}
					}
				}, default, true);
			}
			pList = list;

			if (GUI.changed)
				EditorPrefs.SetBool(pSaverKey, show);

			GUI.backgroundColor = prevColor;
		}
	}
}