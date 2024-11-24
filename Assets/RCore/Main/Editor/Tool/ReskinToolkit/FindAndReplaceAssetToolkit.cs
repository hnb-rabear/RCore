using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class FindAndReplaceAssetToolkit : ScriptableObject
	{
		private static readonly string m_FilePath = $"Assets/Editor/{nameof(FindAndReplaceAssetToolkit)}.asset";
		public ReplaceSpriteTool replaceSpriteTool;
		public CutSpriteSheetTool cutSpriteSheetTool;
		public UpdateImagePropertyTool updateImagePropertyTool;
		public ReplaceObjectTool replaceObjectTool;

		public static FindAndReplaceAssetToolkit Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(m_FilePath, typeof(FindAndReplaceAssetToolkit)) as FindAndReplaceAssetToolkit;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<FindAndReplaceAssetToolkit>(m_FilePath);
			return collection;
		}
	}
}