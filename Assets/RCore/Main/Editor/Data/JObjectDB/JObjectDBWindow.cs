/**
 * Author HNB-RaBear - 2024
 **/

using System.Collections.Generic;
using Newtonsoft.Json;
using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// Editor window for viewing and managing JObject Database data.
	/// </summary>
	public class JObjectDBWindow : EditorWindow
	{
		private Dictionary<string, string> m_data;
		private Vector2 m_scrollPosition;

		private void OnEnable()
		{
			m_data = JObjectDB.GetAllData();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			var actions = new List<IDraw>();
			actions.Add(new GuiButton
			{
				label = "Delete All",
				color = Color.red,
				onPressed = () =>
				{
					if (EditorHelper.ConfirmPopup())
					{
						JObjectDB.DeleteAll();
						m_data = JObjectDB.GetAllData();
						Repaint();
					}
				}
			});
			actions.Add(new GuiButton
			{
				label = "Back Up",
				onPressed = () => JObjectDB.Backup(openDirectory:true)
			});
			actions.Add(new GuiButton
			{
				label = "Restore",
				onPressed = () =>
				{
					string path = EditorUtility.OpenFilePanel("Select Backup Data File", Application.dataPath, "json");
					if (!string.IsNullOrEmpty(path))
					{
						JObjectDB.Restore(path);
						m_data = JObjectDB.GetAllData();
						Repaint();
					}
				}
			});
			actions.Add(new GuiButton
			{
				label = "Copy All",
				onPressed = JObjectDB.CopyAllData
			});
			actions.Add(new GuiButton
			{
				label = "Save (In Game)",
				color = Application.isPlaying ? Color.yellow : Color.grey,
				onPressed = () =>
				{
					if (!Application.isPlaying)
					{
						Debug.Log("This Function should be called in Playing!");
						return;
					}
					JObjectDB.Save();
				},
			});
			actions.Add(new GuiButton
			{
				label = "Reload",
				onPressed = () => m_data = JObjectDB.GetAllData()
			});
			EditorHelper.GridDraws(3, actions);

			EditorHelper.BoxVertical("JObjects", () =>
			{
				if (m_data != null)
					foreach (var pair in m_data)
					{
						GUILayout.BeginHorizontal();
						EditorHelper.LabelField(pair.Key, 150);
						EditorHelper.TextField(pair.Value, null);
						if (EditorHelper.Button("Edit", 60))
							Edit(pair.Key, pair.Value);
						GUILayout.EndHorizontal();
					}
			}, default, true);
			GUILayout.EndScrollView();
		}

		private void Edit(string key, string content)
		{
			var parsedJson = JsonConvert.DeserializeObject(content);
			content = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
			TextEditorWindow.ShowWindow(content, result =>
			{
				parsedJson = JsonConvert.DeserializeObject(result);
				result = JsonConvert.SerializeObject(parsedJson);
				
				PlayerPrefs.SetString(key, result);
				if (!Application.isPlaying)
					JObjectDB.collections.Clear();
				m_data = JObjectDB.GetAllData();
			});
		}

		public static void ShowWindow()
		{
			var window = GetWindow<JObjectDBWindow>("JObject Database", true);
			window.Show();
		}
	}
}