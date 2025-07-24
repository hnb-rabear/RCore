using System;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a standard editor foldout group, which persists its state using EditorPrefs.
	/// </summary>
	public class GuiFoldout : IDraw
	{
		/// <summary>The label displayed for the foldout.</summary>
		public string label;
		/// <summary>An action to be invoked when the foldout is open (expanded).</summary>
		public Action onFoldout;
		
		/// <summary>Gets a value indicating whether the foldout is currently open (expanded).</summary>
		public bool IsFoldout { get; private set; }

		/// <summary>
		/// Draws the foldout control.
		/// </summary>
		/// <param name="style">This parameter is ignored for this element.</param>
		public void Draw(GUIStyle style = null)
		{
			IsFoldout = EditorPrefs.GetBool($"{label}foldout", false);
			IsFoldout = EditorGUILayout.Foldout(IsFoldout, label);
			if (IsFoldout && onFoldout != null)
				onFoldout();
			if (GUI.changed)
				EditorPrefs.SetBool($"{label}foldout", IsFoldout);
		}
	}
}