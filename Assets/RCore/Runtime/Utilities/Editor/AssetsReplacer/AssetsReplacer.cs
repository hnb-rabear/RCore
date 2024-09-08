using RCore.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Editor
{
	public class AssetsReplacer : ScriptableObject
	{
		private static readonly string m_FilePath = $"Assets/Editor/{nameof(AssetsReplacer)}Cache.asset";
		public SpritesReplacer spritesReplacer;
		public SpritesCutter spritesCutter;
		public ImageComponentPropertiesFixer imageComponentPropertiesFixer;
		public ObjectsReplacer objectsReplacer;

		public static AssetsReplacer Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(m_FilePath, typeof(AssetsReplacer)) as AssetsReplacer;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<AssetsReplacer>(m_FilePath);
			return collection;
		}
	}
}