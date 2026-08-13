using System.Collections.Generic;
using RCore.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Editor.UI
{
	public static class AddressableImageMigrationTool
	{
		private const string MENU = "Assets/RCore/Tools/AddressableImage/Add To GameObjects Using This Sprite";

		[MenuItem(MENU, true)]
		private static bool ValidateAddToGameObjects()
		{
			if (Selection.objects.Length != 1) return false;
			if (Selection.activeObject is Sprite) return true;
			if (Selection.activeObject is Texture2D)
			{
				var path = AssetDatabase.GetAssetPath(Selection.activeObject);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
				return importer != null && importer.textureType == TextureImporterType.Sprite;
			}
			return false;
		}

		[MenuItem(MENU)]
		private static void AddToGameObjects()
		{
			Sprite sprite = Selection.activeObject as Sprite;
			if (sprite == null && Selection.activeObject is Texture2D)
			{
				var path = AssetDatabase.GetAssetPath(Selection.activeObject);
				sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			}

			if (sprite == null)
				return;

			var matches = ScanPrefabs(sprite);
			if (matches.Count == 0)
			{
				EditorUtility.DisplayDialog("AddressableImage", $"No matches found for sprite '{sprite.name}'.", "OK");
				return;
			}

			AddressableImageMigrationWindow.Open(sprite, matches);
		}

		public class Match
		{
			public string prefabPath;
			public string hierarchyPath;
			public int imageIndex;
			public bool alreadyHasComponent;
			public bool selected;
		}

		internal static List<Match> ScanPrefabs(Sprite sprite)
		{
			var matches = new List<Match>();
			var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

			foreach (var guid in prefabGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path))
					continue;

				var root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					var images = root.GetComponentsInChildren<Image>(true);
					for (int i = 0; i < images.Length; i++)
					{
						var image = images[i];
						if (image.sprite != sprite)
							continue;

						bool hasComponent = image.GetComponent<AddressableImage>() != null;
						matches.Add(new Match
						{
							prefabPath = path,
							hierarchyPath = AddressableImageEditor.GetHierarchyPath(image.transform),
							imageIndex = i,
							alreadyHasComponent = hasComponent,
							selected = !hasComponent,
						});
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}
			return matches;
		}

		internal static void Apply(List<Match> matches, out int succeeded, out int failed)
		{
			succeeded = 0;
			failed = 0;

			// Group by prefab path so each prefab is loaded/saved once even with multiple matched rows.
			var byPrefab = new Dictionary<string, List<Match>>();
			foreach (var match in matches)
			{
				if (!match.selected || match.alreadyHasComponent)
					continue;
				if (!byPrefab.TryGetValue(match.prefabPath, out var list))
					byPrefab[match.prefabPath] = list = new List<Match>();
				list.Add(match);
			}

			foreach (var pair in byPrefab)
			{
				var root = PrefabUtility.LoadPrefabContents(pair.Key);
				var changed = false;
				try
				{
					// Loaded contents are a fresh object graph; scan-phase references are stale.
					// Re-resolve each target by its ordinal index within GetComponentsInChildren<Image>(true),
					// which is deterministic across scan and apply since the hierarchy is unchanged between phases.
					// (Hierarchy-path string equality is not reliable here since Unity allows duplicate
					// sibling names, which can produce colliding path strings for distinct GameObjects.)
					var images = root.GetComponentsInChildren<Image>(true);
					foreach (var match in pair.Value)
					{
						Image target = (match.imageIndex >= 0 && match.imageIndex < images.Length) ? images[match.imageIndex] : null;

						if (target == null || target.GetComponent<AddressableImage>() != null)
						{
							failed++;
							continue;
						}

						var addressableImage = target.gameObject.AddComponent<AddressableImage>();
						if (AddressableImageEditor.CaptureImageSprite(addressableImage, true))
						{
							succeeded++;
							changed = true;
						}
						else
						{
							// Capture failed: remove the component so the prefab is left untouched.
							Object.DestroyImmediate(addressableImage);
							failed++;
						}
					}

					if (changed)
						PrefabUtility.SaveAsPrefabAsset(root, pair.Key);
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}

			AssetDatabase.SaveAssets();
		}
	}

	public class AddressableImageMigrationWindow : EditorWindow
	{
		private Sprite m_Sprite;
		private List<AddressableImageMigrationTool.Match> m_Matches;
		private Vector2 m_Scroll;

		public static void Open(Sprite sprite, List<AddressableImageMigrationTool.Match> matches)
		{
			var window = GetWindow<AddressableImageMigrationWindow>(true, "Add AddressableImage");
			window.m_Sprite = sprite;
			window.m_Matches = matches;
			window.minSize = new Vector2(560, 300);
			window.Show();
		}

		private void OnGUI()
		{
			if (m_Sprite == null || m_Matches == null)
			{
				EditorGUILayout.HelpBox("No scan data. Re-run the tool from a sprite's context menu.", MessageType.Info);
				return;
			}

			int alreadyCount = 0;
			foreach (var match in m_Matches)
				if (match.alreadyHasComponent)
					alreadyCount++;

			EditorGUILayout.LabelField($"Sprite: {m_Sprite.name}", EditorStyles.boldLabel);
			EditorGUILayout.LabelField($"Matches: {m_Matches.Count}   Already have AddressableImage: {alreadyCount}");

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Select All"))
			{
				foreach (var match in m_Matches)
					if (!match.alreadyHasComponent)
						match.selected = true;
			}
			if (GUILayout.Button("Select None"))
			{
				foreach (var match in m_Matches)
					match.selected = false;
			}
			EditorGUILayout.EndHorizontal();

			m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
			foreach (var match in m_Matches)
			{
				if (match.alreadyHasComponent)
				{
					using (new EditorGUI.DisabledScope(true))
						EditorGUILayout.ToggleLeft($"{match.prefabPath}  |  {match.hierarchyPath}  (already has AddressableImage)", false);
				}
				else
				{
					match.selected = EditorGUILayout.ToggleLeft($"{match.prefabPath}  |  {match.hierarchyPath}", match.selected);
				}
			}
			EditorGUILayout.EndScrollView();

			if (GUILayout.Button("Apply Selected"))
			{
				int selectedCount = 0;
				foreach (var match in m_Matches)
					if (match.selected && !match.alreadyHasComponent)
						selectedCount++;

				AddressableImageMigrationTool.Apply(m_Matches, out int succeeded, out int failed);
				Debug.Log($"[AddressableImageMigrationTool] Applied to {succeeded}/{selectedCount} targets. Already had component: {alreadyCount}. Failed: {failed}.");
				Close();
			}
		}
	}
}
