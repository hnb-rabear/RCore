using RCore.Common;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Editor
{
    [System.Serializable]
    public class AtlasTexture
    {
        private Texture m_Atlas;
        private Sprite[] m_Sprites;
        public Texture Atlas
        {
            get { return m_Atlas; }
            set
            {
                if (m_Atlas != value)
                {
                    m_Atlas = value;
                    var atlasPath = AssetDatabase.GetAssetPath(value);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(atlasPath).Where(a => a is Sprite).ToArray();
                    m_Sprites = new Sprite[sprites.Length];
                    for (int i = 0; i < sprites.Length; i++)
                        m_Sprites[i] = sprites[i] as Sprite;
                }
                else
                    m_Sprites = null;
            }
        }
        public Sprite[] Sprites => m_Sprites;
        public int Length => m_Sprites == null ? 0 : m_Sprites.Length;
        public string Name => m_Atlas == null ? "" : m_Atlas.name;
    }

    [System.Serializable]
    public class SpriteToSprite
    {
        public Sprite left;
        public Sprite right;
    }

    public class TexturesReplacerSave : ScriptableObject
    {
        public static readonly string FilePath = "Assets/Editor/TexturesReplacerSave.asset";
        public List<Sprite> leftInputSprites = new List<Sprite>();
        public List<Sprite> rightInputSprites = new List<Sprite>();
        public List<AtlasTexture> leftAtlasTextures = new List<AtlasTexture>();
        public List<AtlasTexture> rightAtlasTextures = new List<AtlasTexture>();
        public List<SpriteToSprite> spritesToSprites = new List<SpriteToSprite>();
        public static TexturesReplacerSave LoadOrCreateSettings()
        {
            var collection = AssetDatabase.LoadAssetAtPath(FilePath, typeof(TexturesReplacerSave)) as TexturesReplacerSave;
            if (collection == null)
                collection = EditorHelper.CreateScriptableAsset<TexturesReplacerSave>(FilePath);
            return collection;
        }
    }
}