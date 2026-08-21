using System.Reflection;
using RCore.Config;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	[CustomEditor(typeof(GeneralAudioLinker))]
	public class GeneralAudioLinkerEditor : UnityEditor.Editor
	{
		private GeneralAudioLinker m_Target;
		private bool m_ShowQuickAdd;
		private string m_QuickAddCategory = "Uncategorized";

		private void OnEnable()
		{
			m_Target = target as GeneralAudioLinker;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space(4);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", m_Target.Key);
			if (GUILayout.Button("Browse...", GUILayout.Width(80)))
			{
				AssetCatalogPickerWindow.Open(CatalogAssetType.AudioClip, pickedKey =>
				{
					Undo.RecordObject(m_Target, "Set Audio Key");
					m_Target.Key = pickedKey;
					EditorUtility.SetDirty(m_Target);
				});
			}
			EditorGUILayout.EndHorizontal();

			var catalog = AssetCatalog.Instance;
			var clip = catalog != null ? catalog.GetAudioClip(m_Target.Key) : null;
			var src = m_Target.GetComponent<AudioSource>();
			using (new EditorGUI.DisabledScope(clip == null || src == null))
			{
				if (GUILayout.Button("Restore Original Component"))
				{
					Undo.RecordObject(m_Target, "Restore Audio Component");
					Undo.RecordObject(src, "Restore Audio Component");
					src.clip = clip;
					m_Target.Key = string.Empty;
					EditorUtility.SetDirty(src);
					EditorUtility.SetDirty(m_Target);

					if (EditorUtility.DisplayDialog(
						"Remove GeneralAudioLinker",
						"Audio restored. Remove GeneralAudioLinker component?",
						"Yes",
						"No"))
					{
						Undo.DestroyObjectImmediate(m_Target);
						return;
					}
				}
			}

			if (clip != null)
			{
				AssetCatalogEditorGui.DrawAssetPath(clip);

				EditorGUILayout.LabelField("Resolved Clip (inspector-only — not applied in edit mode)", clip.name);

				EditorGUILayout.BeginHorizontal();
				bool isPlaying = IsClipPlaying(clip);
				if (GUILayout.Button(isPlaying ? "■ Stop" : "▶ Play", GUILayout.Width(70)))
				{
					if (isPlaying)
						StopAllClips();
					else
					{
						StopAllClips();
						PlayClip(clip);
					}
				}
				EditorGUILayout.LabelField($"{clip.length:F2}s / {clip.frequency}Hz", EditorStyles.miniLabel);
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				if (!string.IsNullOrEmpty(m_Target.Key))
					EditorGUILayout.HelpBox($"AudioClip key '{m_Target.Key}' not found in AssetCatalog.", MessageType.Warning);

				if (catalog != null && src != null && src.clip != null)
				{
					var sourceClip = src.clip;
					if (AssetCatalogEditorGui.DrawQuickAdd(catalog, CatalogAssetType.AudioClip, ref m_ShowQuickAdd, ref m_QuickAddCategory, sourceClip.name))
					{
						catalog.EditorAddAudioClip(sourceClip.name, m_QuickAddCategory, sourceClip);

						Undo.RecordObject(m_Target, "Quick Add Audio");
						Undo.RecordObject(src, "Quick Add Audio");

						m_Target.Key = sourceClip.name;
						src.clip = null; // Clear serialized field (AudioLinker is runtime-only)
						EditorUtility.SetDirty(src);
						EditorUtility.SetDirty(m_Target);
						m_ShowQuickAdd = false;
					}
				}
			}
		}
		private static void PlayClip(AudioClip pClip)
		{
			var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
			if (audioUtilType == null) return;
			var method = audioUtilType.GetMethod("PlayPreviewClip",
				BindingFlags.Static | BindingFlags.Public,
				null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
			method?.Invoke(null, new object[] { pClip, 0, false });
		}

		private static void StopAllClips()
		{
			var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
			if (audioUtilType == null) return;
			var method = audioUtilType.GetMethod("StopAllPreviewClips",
				BindingFlags.Static | BindingFlags.Public);
			method?.Invoke(null, null);
		}

		private static bool IsClipPlaying(AudioClip pClip)
		{
			var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
			if (audioUtilType == null) return false;
			var method = audioUtilType.GetMethod("IsPreviewClipPlaying",
				BindingFlags.Static | BindingFlags.Public);
			if (method == null) return false;
			return (bool)method.Invoke(null, null);
		}
	}
}
