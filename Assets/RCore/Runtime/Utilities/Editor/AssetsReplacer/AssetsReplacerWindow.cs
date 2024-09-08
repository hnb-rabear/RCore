using RCore.Common;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class AssetsReplacerWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private AssetsReplacer m_assetsReplacer;
		private SpritesReplacer m_spritesReplacer;
		private SpritesCutter m_spritesCutter;
		private ImageComponentPropertiesFixer m_imageComponentPropertiesFixer;
		private ObjectsReplacer m_objectsReplacer;
		private string m_tab;
		private SpritesReplacer.Tps m_tps;
		private bool m_displayNullR;

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			m_assetsReplacer ??= AssetsReplacer.Load();
			m_spritesReplacer = m_assetsReplacer.spritesReplacer;
			m_imageComponentPropertiesFixer = m_assetsReplacer.imageComponentPropertiesFixer;
			m_spritesCutter = m_assetsReplacer.spritesCutter;
			m_objectsReplacer = m_assetsReplacer.objectsReplacer;

			m_tab = EditorHelper.Tabs("m_assetsReplacer.spriteReplace", "Sprites Replacer", "Export sprites from sheet", "Sprite Utilities", "Objects Replacer");
			GUILayout.BeginVertical("box");
			switch (m_tab)
			{
				case "Sprites Replacer":
					m_spritesReplacer.Draw();
					break;
				case "Export sprites from sheet":
					m_spritesCutter.Draw();
					break;
				case "Sprite Utilities":
					m_imageComponentPropertiesFixer.Draw();
					break;
				case "Objects Replacer":
					m_objectsReplacer.Draw();
					break;
			}
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}

		[MenuItem("RCore/Tools/Assets Replacer")]
		private static void OpenEditorWindow()
		{
			var window = GetWindow<AssetsReplacerWindow>("Assets Replacer", true);
			window.Show();
		}
	}
}