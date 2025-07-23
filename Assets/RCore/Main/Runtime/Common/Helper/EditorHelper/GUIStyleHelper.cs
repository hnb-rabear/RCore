using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public static class GUIStyleHelper
	{
		public static readonly GUIStyle HeaderTitle = new GUIStyle(EditorStyles.boldLabel)
		{
			fontSize = 15,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter,
			fixedHeight = 30,
		};
	}
}