using RCore.Config;
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

			EditorGUILayout.Space(4);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", m_Target.Key);
			if (GUILayout.Button("Browse...", GUILayout.Width(80)))
			{
				AssetCatalogPickerWindow.Open(CatalogAssetType.Sprite, pickedKey =>
				{
					var img = m_Target.GetComponent<UnityEngine.UI.Image>();
					Undo.RecordObject(m_Target, "Set Sprite Key");
					if (img != null)
						Undo.RecordObject(img, "Set Sprite Key");
					m_Target.Key = pickedKey;
					if (img != null && img.sprite != null)
					{
						img.sprite = null;
						EditorUtility.SetDirty(img);
					}
					m_Target.Refresh();
					EditorUtility.SetDirty(m_Target);
				});
			}
			EditorGUILayout.EndHorizontal();

			var catalog = AssetCatalog.Instance;
			var sprite = catalog != null ? catalog.GetSprite(m_Target.Key) : null;
			if (sprite != null)
			{
				var assetPath = AssetDatabase.GetAssetPath(sprite);
				if (!string.IsNullOrEmpty(assetPath))
				{
					EditorGUILayout.BeginHorizontal();
					var displayPath = assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath;
					EditorGUILayout.LabelField("Path", displayPath, EditorStyles.miniLabel);
					if (GUILayout.Button("Ping", GUILayout.Width(40)))
						EditorGUIUtility.PingObject(sprite);
					EditorGUILayout.EndHorizontal();
				}

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
				var preview = AssetPreview.GetAssetPreview(sprite);
				if (preview != null)
					GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
			}
			else
			{
				if (!string.IsNullOrEmpty(m_Target.Key))
					EditorGUILayout.HelpBox($"Sprite key '{m_Target.Key}' not found in AssetCatalog.", MessageType.Warning);

				var img = m_Target.GetComponent<UnityEngine.UI.Image>();
				if (catalog != null && img != null && img.sprite != null)
				{
					EditorGUILayout.Space();
					if (!m_ShowQuickAdd)
					{
						if (GUILayout.Button($"Add '{img.sprite.name}' to AssetCatalog", GUILayout.Height(30)))
						{
							m_ShowQuickAdd = true;
						}
					}
					else
					{
						EditorGUILayout.BeginVertical("box");
						EditorGUILayout.LabelField("Quick Add to Catalog", EditorStyles.boldLabel);
						m_QuickAddCategory = EditorGUILayout.TextField("Category", m_QuickAddCategory);

						var existingCats = catalog.EditorGetDistinctCategories(CatalogAssetType.Sprite);
						if (existingCats.Length > 0)
						{
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField("Existing:", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
							foreach (var cat in existingCats)
							{
								if (GUILayout.Button(cat, EditorStyles.miniButton))
									m_QuickAddCategory = cat;
							}
							EditorGUILayout.EndHorizontal();
						}

						EditorGUILayout.Space();
						EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button("Confirm Add"))
						{
							catalog.EditorAddSprite(img.sprite.name, m_QuickAddCategory, img.sprite);

							Undo.RecordObject(m_Target, "Quick Add Sprite");
							Undo.RecordObject(img, "Quick Add Sprite");

							m_Target.Key = img.sprite.name;
							var s = img.sprite;
							img.sprite = null;
							img.overrideSprite = s; // Immediately take over
								EditorUtility.SetDirty(img);

							EditorUtility.SetDirty(m_Target);
							m_ShowQuickAdd = false;
						}
						if (GUILayout.Button("Cancel"))
							m_ShowQuickAdd = false;
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndVertical();
					}
				}
			}
		}
	}
}
