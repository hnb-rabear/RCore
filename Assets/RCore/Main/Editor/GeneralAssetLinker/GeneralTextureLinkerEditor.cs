using RCore.Config;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	[CustomEditor(typeof(GeneralTextureLinker))]
	public class GeneralTextureLinkerEditor : UnityEditor.Editor
	{
		private GeneralTextureLinker m_Target;
		private bool m_ShowQuickAdd;
		private string m_QuickAddCategory = "Uncategorized";

		private void OnEnable()
		{
			m_Target = target as GeneralTextureLinker;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space(4);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", m_Target.Key);
			if (GUILayout.Button("Browse...", GUILayout.Width(80)))
			{
				AssetCatalogPickerWindow.Open(CatalogAssetType.Texture2D, pickedKey =>
				{
					Undo.RecordObject(m_Target, "Set Texture Key");
					m_Target.Key = pickedKey;
					EditorUtility.SetDirty(m_Target);
				});
			}
			EditorGUILayout.EndHorizontal();

			var catalog = AssetCatalog.Instance;
			var texture = catalog != null ? catalog.GetTexture(m_Target.Key) : null;
			if (texture != null)
			{
				var assetPath = AssetDatabase.GetAssetPath(texture);
				if (!string.IsNullOrEmpty(assetPath))
				{
					EditorGUILayout.BeginHorizontal();
					var displayPath = assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath;
					EditorGUILayout.LabelField("Path", displayPath, EditorStyles.miniLabel);
					if (GUILayout.Button("Ping", GUILayout.Width(40)))
						EditorGUIUtility.PingObject(texture);
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField("Preview (inspector-only — not applied in edit mode)", EditorStyles.boldLabel);
				var rect = GUILayoutUtility.GetRect(96, 96, GUILayout.ExpandWidth(false));
				GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
			}
			else
			{
				if (!string.IsNullOrEmpty(m_Target.Key))
					EditorGUILayout.HelpBox($"Texture key '{m_Target.Key}' not found in AssetCatalog.", MessageType.Warning);

				var raw = m_Target.GetComponent<UnityEngine.UI.RawImage>();
				var tex = raw != null ? raw.texture as Texture2D : null;
				if (catalog != null && tex != null)
				{
					EditorGUILayout.Space();
					if (!m_ShowQuickAdd)
					{
						if (GUILayout.Button($"Add '{tex.name}' to AssetCatalog", GUILayout.Height(30)))
						{
							m_ShowQuickAdd = true;
						}
					}
					else
					{
						EditorGUILayout.BeginVertical("box");
						EditorGUILayout.LabelField("Quick Add to Catalog", EditorStyles.boldLabel);
						m_QuickAddCategory = EditorGUILayout.TextField("Category", m_QuickAddCategory);

						var existingCats = catalog.EditorGetDistinctCategories(CatalogAssetType.Texture2D);
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
							catalog.EditorAddTexture(tex.name, m_QuickAddCategory, tex);

							Undo.RecordObject(m_Target, "Quick Add Texture");
							Undo.RecordObject(raw, "Quick Add Texture");

							m_Target.Key = tex.name;
							raw.texture = null; // Clear serialized field (TextureLinker is runtime-only)
								EditorUtility.SetDirty(raw);

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
