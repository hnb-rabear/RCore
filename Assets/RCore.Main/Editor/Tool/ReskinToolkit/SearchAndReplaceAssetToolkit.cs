using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class SearchAndReplaceAssetToolkit : ScriptableObject
	{
		private static readonly string m_FilePath = $"Assets/Editor/{nameof(SearchAndReplaceAssetToolkit)}.asset";
		public ReplaceSpriteTool replaceSpriteTool;
		public CutSpriteSheetTool cutSpriteSheetTool;
		public UpdateImagePropertyTool updateImagePropertyTool;
		public ReplaceObjectTool replaceObjectTool;

		public static SearchAndReplaceAssetToolkit Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(m_FilePath, typeof(SearchAndReplaceAssetToolkit)) as SearchAndReplaceAssetToolkit;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<SearchAndReplaceAssetToolkit>(m_FilePath);
			return collection;
		}
	}
}