#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a stylized header that can be folded (expanded/collapsed).
	/// Its state is persisted using EditorPrefs.
	/// </summary>
	public class GuiHeaderFoldout : IDraw
	{
		/// <summary>A unique key used to save the foldout's state in EditorPrefs.</summary>
		public string key;
		/// <summary>If true, a more compact, minimalistic style is used.</summary>
		public bool minimalistic;
		/// <summary>The text label for the header.</summary>
		public string label;
		
		/// <summary>Gets a value indicating whether the foldout is currently open (expanded).</summary>
		public bool IsFoldout { get; private set; }

		/// <summary>
		/// Draws the header foldout.
		/// </summary>
		/// <param name="style">Optional custom GUIStyle to override the default appearance.</param>
		public void Draw(GUIStyle style = null)
		{
			IsFoldout = EditorPrefs.GetBool(key, false);

			if (!minimalistic) GUILayout.Space(3f);
			if (!IsFoldout) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
			GUILayout.BeginHorizontal();
			GUI.changed = false;

			if (minimalistic)
			{
				if (IsFoldout) label = $"\u25BC{(char)0x200a}{label}";
				else label = $"\u25BA{(char)0x200a}{label}";

				style ??= new GUIStyle("PreToolbar2");

				GUILayout.BeginHorizontal();
				GUI.contentColor = EditorGUIUtility.isProSkin
					? new Color(1f, 1f, 1f, 0.7f)
					: new Color(0f, 0f, 0f, 0.7f);
				if (!GUILayout.Toggle(true, label, style, GUILayout.MinWidth(20f)))
					IsFoldout = !IsFoldout;
				GUI.contentColor = Color.white;
				GUILayout.EndHorizontal();
			}
			else
			{
				if (IsFoldout) label = $"\u25BC {label}";
				else label = $"\u25BA {label}";
				if (style == null)
				{
					string styleString = IsFoldout ? "Button" : "DropDownButton";
					style = new GUIStyle(styleString)
					{
						alignment = TextAnchor.MiddleLeft,
						fontSize = 11,
						fontStyle = IsFoldout ? FontStyle.Bold : FontStyle.Normal
					};
				}

				if (!GUILayout.Toggle(true, label, style, GUILayout.MinWidth(20f)))
					IsFoldout = !IsFoldout;
			}

			if (GUI.changed) EditorPrefs.SetBool(key, IsFoldout);

			if (!minimalistic) GUILayout.Space(2f);
			GUILayout.EndHorizontal();
			GUI.backgroundColor = Color.white;
			if (!IsFoldout) GUILayout.Space(3f);
		}
	}
}
#endif