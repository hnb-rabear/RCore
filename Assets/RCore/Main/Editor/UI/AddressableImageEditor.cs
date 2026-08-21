#if UNITY_EDITOR
using System.Collections.Generic;
using RCore;
using RCore.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RCore.Editor.UI
{
	public static class AddressableImageEditor
	{
		public struct StripSummary
		{
			public int prefabsScanned;
			public int prefabsChanged;
			public int spritesCleared;

			public override string ToString()
			{
				return $"Prefabs scanned: {prefabsScanned}, prefabs changed: {prefabsChanged}, sprites cleared: {spritesCleared}";
			}
		}

		public struct ValidationSummary
		{
			public int objectsScanned;
			public int errors;
			public int warnings;

			public bool HasErrors => errors > 0;

			public override string ToString()
			{
				return $"Objects scanned: {objectsScanned}, errors: {errors}, warnings: {warnings}";
			}
		}

		public static string GetHierarchyPath(Transform transform)
		{
			var path = transform.name;
			while (transform.parent != null)
			{
				transform = transform.parent;
				path = transform.name + "/" + path;
			}
			return path;
		}

		internal static Sprite LoadEditorSprite(AssetReferenceSprite reference)
		{
			if (reference == null || string.IsNullOrEmpty(reference.AssetGUID))
				return null;

			var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
			if (string.IsNullOrEmpty(path))
				return null;

			if (!string.IsNullOrEmpty(reference.SubObjectName))
			{
				var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
				foreach (var asset in subAssets)
				{
					if (asset is Sprite sprite && sprite.name == reference.SubObjectName)
						return sprite;
				}
			}

			return AssetDatabase.LoadAssetAtPath<Sprite>(path);
		}

		public static void RefreshEditorPreview(AddressableImage addressableImage)
		{
			if (addressableImage == null || !addressableImage.HasReference())
				return;

			var image = addressableImage.GetComponent<Image>();
			if (image == null)
				return;

			var sprite = LoadEditorSprite(addressableImage.SpriteReference);
			if (sprite != null)
				image.overrideSprite = sprite;
		}

		public static bool CaptureImageSprite(AddressableImage addressableImage, bool logErrors)
		{
			if (addressableImage == null)
				return false;

			var image = addressableImage.GetComponent<Image>();
			if (image == null)
				return false;

			var sprite = image.sprite != null ? image.sprite : LoadEditorSprite(addressableImage.SpriteReference);
			if (sprite == null)
				return false;

			Undo.RecordObject(image, "Set Addressable Asset");
			Undo.RecordObject(addressableImage, "Set Addressable Asset");
			var path = AssetDatabase.GetAssetPath(sprite);
			if (string.IsNullOrEmpty(path) || !AssetDatabase.Contains(sprite))
			{
				if (logErrors)
					Debug.LogError($"[AddressableImage] Sprite '{sprite.name}' is not a project asset and cannot be addressable.", addressableImage);
				return false;
			}

			var guid = AssetDatabase.AssetPathToGUID(path);
			if (string.IsNullOrEmpty(guid))
			{
				if (logErrors)
					Debug.LogError($"[AddressableImage] Cannot resolve GUID for sprite '{sprite.name}' at '{path}'.", addressableImage);
				return false;
			}

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null || settings.DefaultGroup == null)
			{
				if (logErrors)
					Debug.LogError("[AddressableImage] Addressable settings/default group not found.", addressableImage);
				return false;
			}

			var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
			if (entry == null)
			{
				if (logErrors)
					Debug.LogError($"[AddressableImage] Failed to create Addressable entry for '{path}'.", addressableImage);
				return false;
			}

			if (addressableImage.SpriteReference == null || addressableImage.SpriteReference.AssetGUID != guid)
				addressableImage.SetSpriteReference(new AssetReferenceSprite(guid));

			addressableImage.SpriteReference.SetEditorAsset(sprite);
			if (AssetDatabase.IsSubAsset(sprite))
				addressableImage.SpriteReference.SetEditorSubObject(sprite);

			image.sprite = null;
			image.overrideSprite = sprite;

			EditorUtility.SetDirty(addressableImage);
			EditorUtility.SetDirty(image);
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
			return true;
		}

		public static void CaptureOrRefresh(AddressableImage addressableImage, bool logErrors)
		{
			var image = addressableImage.GetComponent<Image>();
			if (image == null)
				return;

			if (image.sprite != null && !addressableImage.HasReference())
				CaptureImageSprite(addressableImage, logErrors);
			else
				RefreshEditorPreview(addressableImage);
		}

		private static bool IsReferenceAddressable(AssetReferenceSprite reference)
		{
			if (reference == null || string.IsNullOrEmpty(reference.AssetGUID))
				return false;

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null)
				return false;

			var entry = settings.FindAssetEntry(reference.AssetGUID, true);
			if (entry == null)
				return false;

			var excludedGroup = settings.FindGroup("Excluded Content");
			return excludedGroup == null || excludedGroup.GetAssetEntry(reference.AssetGUID, true) == null;
		}

		public static StripSummary StripAllPersistedSprites()
		{
			var summary = new StripSummary();
			var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

			foreach (var guid in prefabGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path))
					continue;

				summary.prefabsScanned++;
				var root = PrefabUtility.LoadPrefabContents(path);
				var changed = false;

				try
				{
					var addressableImages = root.GetComponentsInChildren<AddressableImage>(true);
					foreach (var addressableImage in addressableImages)
					{
						if (addressableImage == null || !addressableImage.HasReference())
							continue;

						var image = addressableImage.GetComponent<Image>();
						if (image != null && image.sprite != null)
						{
							image.sprite = null;
							image.overrideSprite = null;
							summary.spritesCleared++;
							changed = true;
						}
					}

					if (changed)
					{
						PrefabUtility.SaveAsPrefabAsset(root, path);
						summary.prefabsChanged++;
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}

			AssetDatabase.SaveAssets();
			return summary;
		}

		[MenuItem("RCore/Tools/AddressableImage/Validate All")]
		public static void ValidateAllMenu()
		{
			ValidateAll(true, false);
		}

		public static ValidationSummary ValidateAll(bool logDetails, bool throwOnError)
		{
			var summary = new ValidationSummary();
			var messages = new List<string>();
			var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

			foreach (var guid in prefabGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path))
					continue;

				var root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					var addressableImages = root.GetComponentsInChildren<AddressableImage>(true);
					foreach (var addressableImage in addressableImages)
					{
						summary.objectsScanned++;
						var hierarchyPath = GetHierarchyPath(addressableImage.transform);
						var image = addressableImage.GetComponent<Image>();

						if (image == null)
						{
							summary.errors++;
							messages.Add($"ERROR {path} | {hierarchyPath}: Missing Image component.");
							continue;
						}

						if (!addressableImage.HasReference())
						{
							summary.errors++;
							messages.Add($"ERROR {path} | {hierarchyPath}: Missing AssetReferenceSprite.");
						}
						else if (!IsReferenceAddressable(addressableImage.SpriteReference))
						{
							summary.errors++;
							messages.Add($"ERROR {path} | {hierarchyPath}: Referenced sprite is not included in Addressables build.");
						}

						if (image.sprite != null)
						{
							summary.errors++;
							messages.Add($"ERROR {path} | {hierarchyPath}: Image.sprite still serialized as '{image.sprite.name}'. Run Strip All.");
						}

						if (image.sprite == null && addressableImage.HasReference())
						{
							var sprite = LoadEditorSprite(addressableImage.SpriteReference);
							if (sprite == null)
							{
								summary.errors++;
								messages.Add($"ERROR {path} | {hierarchyPath}: Cannot resolve referenced sprite from GUID '{addressableImage.SpriteReference.AssetGUID}'.");
							}
						}
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}

			if (logDetails)
			{
				foreach (var message in messages)
					Debug.LogError($"[AddressableImage] {message}");
				Debug.Log($"[AddressableImage] Validate All complete. {summary}");
			}

			if (throwOnError && summary.HasErrors)
				throw new BuildFailedException($"AddressableImage validation failed. {summary}");

			return summary;
		}

	}

	[CustomEditor(typeof(AddressableImage))]
	[CanEditMultipleObjects]
	public class AddressableImageInspector : UnityEditor.Editor
	{
		private AddressableImage m_Target;
		private float m_NativeSizeRatio = 1f;

		private void OnEnable()
		{
			m_Target = target as AddressableImage;
			if (m_Target != null)
				AddressableImageEditor.RefreshEditorPreview(m_Target);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawDefaultInspector();

			if (m_Target == null)
				return;

			AddressableImageEditor.RefreshEditorPreview(m_Target);
			var image = m_Target.GetComponent<Image>();
			var reference = m_Target.SpriteReference;
			var hasReference = m_Target.HasReference();
			var editorSprite = hasReference ? AddressableImageEditor.LoadEditorSprite(reference) : null;

			var images = new List<Image>();
			foreach (var selected in targets)
			{
				var selectedAddressableImage = selected as AddressableImage;
				var selectedImage = selectedAddressableImage != null ? selectedAddressableImage.GetComponent<Image>() : null;
				if (selectedImage != null)
					images.Add(selectedImage);
			}

			if (images.Count > 0)
			{
				EditorGUILayout.Space(6);
				DrawPreserveAspect(images.ToArray());
			}

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("AddressableImage Status", EditorStyles.boldLabel);

			if (image == null)
				EditorGUILayout.HelpBox("Missing Image component.", MessageType.Error);
			else if (image.sprite == null && !hasReference)
				EditorGUILayout.HelpBox("No sprite assigned. Assign a sprite to Image.sprite to capture it.", MessageType.Error);
			else if (image.sprite == null && hasReference)
				EditorGUILayout.HelpBox("Sprite stripped. Editor/runtime display uses Image.overrideSprite.", MessageType.Info);
			else if (image.sprite != null && !hasReference)
				EditorGUILayout.HelpBox("Image.sprite assigned but not captured yet. OnValidate should capture it.", MessageType.Warning);
			else if (image.sprite != null && hasReference)
				EditorGUILayout.HelpBox("Image.sprite is still serialized. Click Strip Sprite Now.", MessageType.Warning);

			if (hasReference)
			{
				var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
				var settings = AddressableAssetSettingsDefaultObject.Settings;
				var entry = settings != null ? settings.FindAssetEntry(reference.AssetGUID, true) : null;
				EditorGUILayout.LabelField("Sprite", editorSprite != null ? editorSprite.name : "<missing>");
				EditorGUILayout.LabelField("GUID", reference.AssetGUID);
				EditorGUILayout.LabelField("SubObject", string.IsNullOrEmpty(reference.SubObjectName) ? "<none>" : reference.SubObjectName);
				EditorGUILayout.LabelField("Address", entry != null ? entry.address : "<not addressable>");
				EditorGUILayout.LabelField("Group", entry != null && entry.parentGroup != null ? entry.parentGroup.Name : "<none>");

				if (editorSprite == null)
				{
					EditorGUILayout.LabelField("Path", ShortenAssetPath(path));
					EditorGUILayout.HelpBox("Referenced sprite cannot be loaded in editor.", MessageType.Error);
				}
				else
				{
					DrawSpriteInfo(editorSprite);
					DrawNativeSizeTools(editorSprite, image);
					DrawSpritePreview(editorSprite);
				}
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Strip Sprite Now"))
			{
				if (image != null)
				{
					Undo.RecordObject(image, "Strip Sprite");
					Undo.RecordObject(m_Target, "Strip Sprite");
					image.sprite = null;
					image.overrideSprite = editorSprite;
					EditorUtility.SetDirty(image);
					EditorUtility.SetDirty(m_Target);
				}
			}

			if (GUILayout.Button("Restore Sprite To Image"))
			{
				if (image != null && editorSprite != null)
				{
					Undo.RecordObject(image, "Restore Sprite To Image");
					Undo.RecordObject(m_Target, "Restore Sprite To Image");
					image.sprite = editorSprite;
					image.overrideSprite = null;
					m_Target.SetSpriteReference(new AssetReferenceSprite(string.Empty));
					EditorUtility.SetDirty(image);
					EditorUtility.SetDirty(m_Target);

					if (EditorUtility.DisplayDialog(
							"Remove Addressable Image?",
							"Sprite restored to Image. Remove AddressableImage component now?",
							"Remove",
							"Keep"))
					{
						Undo.DestroyObjectImmediate(m_Target);
					}
				}
			}

			if (GUILayout.Button("Set Addressable Asset"))
				AddressableImageEditor.CaptureImageSprite(m_Target, true);
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}

		private static string ShortenAssetPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return "<missing>";
			return path.StartsWith("Assets/") ? path.Substring(7) : path;
		}

		private static void DrawSpriteInfo(Sprite sprite)
		{
			var assetPath = AssetDatabase.GetAssetPath(sprite);
			if (!string.IsNullOrEmpty(assetPath))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Path", ShortenAssetPath(assetPath), EditorStyles.miniLabel);
				if (GUILayout.Button("Ping", GUILayout.Width(40)))
					EditorGUIUtility.PingObject(sprite);
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.LabelField("Size", $"{sprite.rect.width} x {sprite.rect.height}", EditorStyles.miniLabel);
		}

		private static void DrawPreserveAspect(Image[] images)
		{
			if (images == null || images.Length == 0)
				return;

			var imageSerializedObject = new SerializedObject(images);
			var preserveAspectProperty = imageSerializedObject.FindProperty("m_PreserveAspect");
			if (preserveAspectProperty == null)
				return;

			imageSerializedObject.Update();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(preserveAspectProperty, new GUIContent("Preserve Aspect"));
			if (EditorGUI.EndChangeCheck())
			{
				imageSerializedObject.ApplyModifiedProperties();
				foreach (var image in images)
					image.SetAllDirty();
				Canvas.ForceUpdateCanvases();
				SceneView.RepaintAll();
			}
		}

		private void DrawNativeSizeTools(Sprite sprite, Image image)
		{
			if (image == null)
				return;

			EditorGUILayout.BeginHorizontal();
			m_NativeSizeRatio = EditorGUILayout.FloatField("Ratio", m_NativeSizeRatio);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			var imageSerializedObject = new SerializedObject(image);
			var pixelsPerUnitMultiplierProperty = imageSerializedObject.FindProperty("m_PixelsPerUnitMultiplier");
			imageSerializedObject.Update();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(pixelsPerUnitMultiplierProperty, new GUIContent("Pixel Per Unit Multiplier"));
			if (EditorGUI.EndChangeCheck())
			{
				imageSerializedObject.ApplyModifiedProperties();
				image.SetAllDirty();
				Canvas.ForceUpdateCanvases();
				SceneView.RepaintAll();
			}

			if (GUILayout.Button("Set Native Size", GUILayout.Width(110)))
			{
				var rt = m_Target.GetComponent<RectTransform>();
				if (rt != null)
				{
					Undo.RecordObject(rt, "Set Native Size");
					var nativeSize = sprite.NativeSize() * m_NativeSizeRatio;
					rt.sizeDelta = nativeSize;
					EditorUtility.SetDirty(rt);
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private static void DrawSpritePreview(Sprite sprite)
		{
			var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
			EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));

			var spriteRect = sprite.rect;
			var texture = sprite.texture;
			if (texture == null)
				return;

			var aspect = spriteRect.width / spriteRect.height;
			var width = 120f;
			var height = 120f;
			if (aspect > 1f)
				height = width / aspect;
			else
				width = height * aspect;

			var drawRect = new Rect(rect.x + (rect.width - width) * 0.5f, rect.y + (rect.height - height) * 0.5f, width, height);
			var texCoords = new Rect(spriteRect.x / texture.width, spriteRect.y / texture.height, spriteRect.width / texture.width, spriteRect.height / texture.height);
			GUI.DrawTextureWithTexCoords(drawRect, texture, texCoords);
		}
	}

	public class AddressableImageSceneBuildStripper : IProcessSceneWithReport
	{
		public int callbackOrder => 0;

		public void OnProcessScene(Scene scene, BuildReport report)
		{
			foreach (var root in scene.GetRootGameObjects())
			{
				var addressableImages = root.GetComponentsInChildren<AddressableImage>(true);
				foreach (var addressableImage in addressableImages)
				{
					if (addressableImage == null || !addressableImage.HasReference())
						continue;

					var image = addressableImage.GetComponent<Image>();
					if (image != null)
						image.sprite = null;
				}
			}
		}
	}

	public class AddressableImagePreBuildProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			AddressableImageEditor.StripAllPersistedSprites();
			AddressableImageEditor.ValidateAll(true, false);
		}
	}
}
#endif
