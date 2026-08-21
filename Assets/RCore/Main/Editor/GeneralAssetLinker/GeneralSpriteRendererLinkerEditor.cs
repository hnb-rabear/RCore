using RCore.Config;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	[CustomEditor(typeof(GeneralSpriteRendererLinker))]
	public class GeneralSpriteRendererLinkerEditor : UnityEditor.Editor
	{
		private GeneralSpriteRendererLinker m_Target;
		private SpriteRenderer m_Renderer;
		private bool m_ShowQuickAdd;
		private string m_QuickAddCategory = "Uncategorized";

		private void OnEnable()
		{
			m_Target = target as GeneralSpriteRendererLinker;
			m_Renderer = m_Target != null ? m_Target.GetComponent<SpriteRenderer>() : null;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (m_Target == null)
				return;

			EditorGUILayout.Space(4);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", m_Target.Key);
			using (new EditorGUI.DisabledScope(AssetCatalog.Instance == null))
			{
				if (GUILayout.Button("Browse...", GUILayout.Width(80)))
				{
					AssetCatalogPickerWindow.Open(CatalogAssetType.Sprite, pickedKey =>
					{
						var catalog = AssetCatalog.Instance;
						if (catalog == null)
							return;
						Undo.RecordObject(m_Target, "Set Sprite Renderer Key");
						if (m_Renderer != null)
							Undo.RecordObject(m_Renderer, "Set Sprite Renderer Key");

						m_Target.Key = pickedKey;
						m_Target.AutoActive = catalog.EditorGetSpriteAutoActive(pickedKey);
						if (m_Renderer != null)
						{
							m_Renderer.sprite = null;
							EditorUtility.SetDirty(m_Renderer);
						}
						m_Target.Refresh();
						EditorUtility.SetDirty(m_Target);
					});
				}
			}
			EditorGUILayout.EndHorizontal();

			var catalog = AssetCatalog.Instance;
			var sprite = catalog != null && !string.IsNullOrEmpty(m_Target.Key)
				? catalog.GetSprite(m_Target.Key)
				: null;

			using (new EditorGUI.DisabledScope(sprite == null || m_Renderer == null))
			{
				if (GUILayout.Button("Restore Original Component"))
				{
					Undo.RecordObject(m_Target, "Restore Sprite Renderer Component");
					Undo.RecordObject(m_Renderer, "Restore Sprite Renderer Component");

					m_Renderer.sprite = sprite;
					m_Target.Key = string.Empty;
					EditorUtility.SetDirty(m_Renderer);
					EditorUtility.SetDirty(m_Target);

					if (EditorUtility.DisplayDialog(
						"Remove GeneralSpriteRendererLinker",
						"Sprite restored. Remove GeneralSpriteRendererLinker component?",
						"Yes",
						"No"))
					{
						Undo.DestroyObjectImmediate(m_Target);
						return;
					}
				}
			}

			if (sprite != null)
			{
				AssetCatalogEditorGui.DrawAssetPath(sprite);
				DrawSpritePreview(sprite);
			}
			else
			{
				if (!string.IsNullOrEmpty(m_Target.Key))
					EditorGUILayout.HelpBox($"Sprite key '{m_Target.Key}' not found in AssetCatalog.", MessageType.Warning);

				if (catalog != null && m_Renderer != null && m_Renderer.sprite != null)
				{
					var sourceSprite = m_Renderer.sprite;
					if (AssetCatalogEditorGui.DrawQuickAdd(
						catalog,
						CatalogAssetType.Sprite,
						ref m_ShowQuickAdd,
						ref m_QuickAddCategory,
						sourceSprite.name))
					{
						catalog.EditorAddSprite(sourceSprite.name, m_QuickAddCategory, sourceSprite);

						Undo.RecordObject(m_Target, "Quick Add Sprite Renderer Sprite");
						Undo.RecordObject(m_Renderer, "Quick Add Sprite Renderer Sprite");

						m_Target.Key = sourceSprite.name;
						m_Target.AutoActive = catalog.EditorGetSpriteAutoActive(sourceSprite.name);
						m_Renderer.sprite = null;
						m_Target.Refresh();
						EditorUtility.SetDirty(m_Renderer);
						EditorUtility.SetDirty(m_Target);
						m_ShowQuickAdd = false;
					}
				}
			}
		}

		private static void DrawSpritePreview(Sprite sprite)
		{
			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

			var rect = GUILayoutUtility.GetRect(96, 96, GUILayout.ExpandWidth(false));
			var texture = sprite.texture;
			if (texture == null || sprite.textureRect.width <= 0f || sprite.textureRect.height <= 0f)
				return;

			var textureRect = sprite.textureRect;
			var texCoords = new Rect(
				textureRect.x / texture.width,
				textureRect.y / texture.height,
				textureRect.width / texture.width,
				textureRect.height / texture.height);
			var scale = Mathf.Min(rect.width / textureRect.width, rect.height / textureRect.height);
			var previewRect = new Rect(
				rect.x + (rect.width - textureRect.width * scale) * 0.5f,
				rect.y + (rect.height - textureRect.height * scale) * 0.5f,
				textureRect.width * scale,
				textureRect.height * scale);
			GUI.DrawTextureWithTexCoords(previewRect, texture, texCoords, true);
		}
	}
}
