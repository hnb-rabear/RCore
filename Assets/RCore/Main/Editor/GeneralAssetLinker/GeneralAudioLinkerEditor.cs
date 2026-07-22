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
			if (clip != null)
			{
				var assetPath = AssetDatabase.GetAssetPath(clip);
				if (!string.IsNullOrEmpty(assetPath))
				{
					EditorGUILayout.BeginHorizontal();
					var displayPath = assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath;
					EditorGUILayout.LabelField("Path", displayPath, EditorStyles.miniLabel);
					if (GUILayout.Button("Ping", GUILayout.Width(40)))
						EditorGUIUtility.PingObject(clip);
					EditorGUILayout.EndHorizontal();
				}

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

				var src = m_Target.GetComponent<AudioSource>();
				if (catalog != null && src != null && src.clip != null)
				{
					EditorGUILayout.Space();
					if (!m_ShowQuickAdd)
					{
						if (GUILayout.Button($"Add '{src.clip.name}' to AssetCatalog", GUILayout.Height(30)))
						{
							m_ShowQuickAdd = true;
						}
					}
					else
					{
						EditorGUILayout.BeginVertical("box");
						EditorGUILayout.LabelField("Quick Add to Catalog", EditorStyles.boldLabel);
						m_QuickAddCategory = EditorGUILayout.TextField("Category", m_QuickAddCategory);

						var existingCats = catalog.EditorGetDistinctCategories(CatalogAssetType.AudioClip);
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
							catalog.EditorAddAudioClip(src.clip.name, m_QuickAddCategory, src.clip);

							Undo.RecordObject(m_Target, "Quick Add Audio");
							Undo.RecordObject(src, "Quick Add Audio");

							m_Target.Key = src.clip.name;
							src.clip = null; // Clear serialized field (AudioLinker is runtime-only)
								EditorUtility.SetDirty(src);

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
