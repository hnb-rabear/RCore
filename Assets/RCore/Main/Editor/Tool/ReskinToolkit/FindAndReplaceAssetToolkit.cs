using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// ScriptableObject configured as a toolkit container for finding and replacing various asset types (Sprites, Fonts, Objects, etc.).
	/// </summary>
	public class FindAndReplaceAssetToolkit : ScriptableObject
	{
		private static readonly string m_FilePath = $"Assets/Editor/{nameof(FindAndReplaceAssetToolkit)}.asset";
		public SpriteReplacer spriteReplacer;
		public SpriteSheetCutter spriteSheetCutter;
		public ImagePropertyFixer imagePropertyFixer;
		public ObjectReplacer objectReplacer;

		public static FindAndReplaceAssetToolkit Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(m_FilePath, typeof(FindAndReplaceAssetToolkit)) as FindAndReplaceAssetToolkit;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<FindAndReplaceAssetToolkit>(m_FilePath);
			return collection;
		}
	}
}