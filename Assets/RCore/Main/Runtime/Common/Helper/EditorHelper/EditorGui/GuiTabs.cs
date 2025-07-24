using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a set of selectable tabs, persisting the current selection via EditorPrefs.
	/// </summary>
	public class GuiTabs : IDraw
	{
		/// <summary>A unique key used to save the current tab's state in EditorPrefs.</summary>
		public string key;
		/// <summary>An array of strings representing the names of the tabs.</summary>
		public string[] tabsName;
		
		/// <summary>Gets the name of the currently selected tab.</summary>
		public string CurrentTab { get; private set; }

		/// <summary>
		/// Draws the tab group.
		/// </summary>
		/// <param name="style">This parameter is ignored for this element.</param>
		public void Draw(GUIStyle style = null)
		{
			CurrentTab = EditorPrefs.GetString($"{key}_current_tab", tabsName[0]);
            
			GUILayout.BeginHorizontal();
			foreach (var tabName in tabsName)
			{
				bool isOn = CurrentTab == tabName;
				var buttonStyle = new GUIStyle(EditorStyles.toolbarButton)
				{
					fixedHeight = 0,
					padding = new RectOffset(4, 4, 4, 4),
					normal =
					{
						textColor = EditorGUIUtility.isProSkin ? Color.white : (isOn ? Color.black : Color.black * 0.6f)
					},
					fontStyle = FontStyle.Bold,
					fontSize = 13
				};

				var preColor = GUI.color;
				var color = isOn ? Color.white : Color.gray;
				GUI.color = color;

				if (GUILayout.Button(tabName, buttonStyle))
				{
					CurrentTab = tabName;
					EditorPrefs.SetString($"{key}_current_tab", CurrentTab);
				}

				GUI.color = preColor;
			}
			GUILayout.EndHorizontal();
		}
	}
}