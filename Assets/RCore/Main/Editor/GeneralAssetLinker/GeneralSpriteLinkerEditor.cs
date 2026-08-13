using RCore.Config;
using RCore.Editor;
using RCore.UI;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	[CustomEditor(typeof(GeneralSpriteLinker))]
	public class GeneralSpriteLinkerEditor : UnityEditor.Editor
	{
		private GeneralSpriteLinker m_Target;
		private bool m_ShowQuickAdd;
		private string m_QuickAddCategory = "Uncategorized";
		private float m_NativeSizeRatio = 1f;

		private void OnEnable()
		{
			m_Target = target as GeneralSpriteLinker;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var img = m_Target.GetComponent<UnityEngine.UI.Image>();
			if (img != null)
			{
				EditorGUI.BeginChangeCheck();
				var preserveAspect = EditorGUILayout.Toggle("Preserve Aspect", m_Target.PreserveAspect);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(img, "Set Preserve Aspect");
					m_Target.PreserveAspect = preserveAspect;
					EditorUtility.SetDirty(img);
				}
			}

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
						var img = m_Target.GetComponent<UnityEngine.UI.Image>();
						Undo.RecordObject(m_Target, "Set Sprite Key");
						if (img != null)
							Undo.RecordObject(img, "Set Sprite Key");
						m_Target.Key = pickedKey;
						m_Target.AutoActive = catalog.EditorGetSpriteAutoActive(pickedKey);
						if (img != null && img.sprite != null)
						{
							img.sprite = null;
							EditorUtility.SetDirty(img);
						}
						m_Target.Refresh();
						EditorUtility.SetDirty(m_Target);
					});
				}
			}
			EditorGUILayout.EndHorizontal();

			var catalog = AssetCatalog.Instance;
			var sprite = catalog != null ? catalog.GetSprite(m_Target.Key) : null;
			using (new EditorGUI.DisabledScope(sprite == null || img == null))
			{
				if (GUILayout.Button("Restore Original Component"))
				{
					Undo.RecordObject(m_Target, "Restore Sprite Component");
					Undo.RecordObject(img, "Restore Sprite Component");
					img.sprite = sprite;
					img.overrideSprite = null;
					m_Target.Key = string.Empty;
					EditorUtility.SetDirty(img);
					EditorUtility.SetDirty(m_Target);

					if (EditorUtility.DisplayDialog(
						"Remove GeneralSpriteLinker",
						"Sprite restored. Remove GeneralSpriteLinker component?",
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

				EditorGUILayout.LabelField("Size", $"{sprite.rect.width} x {sprite.rect.height}", EditorStyles.miniLabel);

				EditorGUILayout.BeginHorizontal();
				m_NativeSizeRatio = EditorGUILayout.FloatField("Ratio", m_NativeSizeRatio);
				if (GUILayout.Button("Set Native Size", GUILayout.Width(110)))
				{
					var rt = m_Target.GetComponent<RectTransform>();
					if (rt != null)
					{
						Undo.RecordObject(rt, "Set Native Size");
						rt.sizeDelta = new Vector2(
							sprite.rect.width * m_NativeSizeRatio,
							sprite.rect.height * m_NativeSizeRatio);
						EditorUtility.SetDirty(rt);
					}
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
				var rect = GUILayoutUtility.GetRect(96, 96, GUILayout.ExpandWidth(false));
				var texture = sprite.texture;
				if (texture != null)
				{
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
			else
			{
				if (!string.IsNullOrEmpty(m_Target.Key))
					EditorGUILayout.HelpBox($"Sprite key '{m_Target.Key}' not found in AssetCatalog.", MessageType.Warning);

				if (catalog != null && img != null && img.sprite != null)
				{
					var sourceSprite = img.sprite;
					if (AssetCatalogEditorGui.DrawQuickAdd(catalog, CatalogAssetType.Sprite, ref m_ShowQuickAdd, ref m_QuickAddCategory, sourceSprite.name))
					{
						catalog.EditorAddSprite(sourceSprite.name, m_QuickAddCategory, sourceSprite);

						Undo.RecordObject(m_Target, "Quick Add Sprite");
						Undo.RecordObject(img, "Quick Add Sprite");

						m_Target.Key = sourceSprite.name;
						m_Target.AutoActive = catalog.EditorGetSpriteAutoActive(sourceSprite.name);
						img.sprite = null;
						img.overrideSprite = sourceSprite; // Immediately take over
						EditorUtility.SetDirty(img);
						EditorUtility.SetDirty(m_Target);
						m_ShowQuickAdd = false;
					}
				}
			}
		}
	}
}
