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
			var raw = m_Target.GetComponent<UnityEngine.UI.RawImage>();
			using (new EditorGUI.DisabledScope(texture == null || raw == null))
			{
				if (GUILayout.Button("Restore Original Component"))
				{
					Undo.RecordObject(m_Target, "Restore Texture Component");
					Undo.RecordObject(raw, "Restore Texture Component");
					raw.texture = texture;
					m_Target.Key = string.Empty;
					EditorUtility.SetDirty(raw);
					EditorUtility.SetDirty(m_Target);

					if (EditorUtility.DisplayDialog(
						"Remove GeneralTextureLinker",
						"Texture restored. Remove GeneralTextureLinker component?",
						"Yes",
						"No"))
					{
						Undo.DestroyObjectImmediate(m_Target);
						return;
					}
				}
			}

			if (texture != null)
			{
				AssetCatalogEditorGui.DrawAssetPath(texture);

				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField("Preview (inspector-only — not applied in edit mode)", EditorStyles.boldLabel);
				var rect = GUILayoutUtility.GetRect(96, 96, GUILayout.ExpandWidth(false));
				GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
			}
			else
			{
				if (!string.IsNullOrEmpty(m_Target.Key))
					EditorGUILayout.HelpBox($"Texture key '{m_Target.Key}' not found in AssetCatalog.", MessageType.Warning);

				var tex = raw != null ? raw.texture as Texture2D : null;
				if (catalog != null && tex != null)
				{
					if (AssetCatalogEditorGui.DrawQuickAdd(catalog, CatalogAssetType.Texture2D, ref m_ShowQuickAdd, ref m_QuickAddCategory, tex.name))
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
				}
			}
		}
	}
}
