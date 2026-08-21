using System;
using System.Collections.Generic;
using RCore.Config;
using RCore.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Editor
{
	internal static class LinkerRestoreService
	{
		internal sealed class LinkerRestoreIssue
		{
			public string assetPath;
			public string hierarchyPath;
			public string componentType;
			public string reason;

			public override string ToString()
			{
				var displayPath = AssetCatalogEditorGui.StripAssetsPrefix(assetPath);
				return $"{displayPath} | {hierarchyPath} | {componentType}: {reason}";
			}
		}

		internal sealed class LinkerRestoreResult
		{
			public string validationError;
			public bool cancelled;
			public int prefabsScanned;
			public int prefabsChanged;
			public int candidatesFound;
			public int linkersRestored;
			public readonly List<string> affectedAssets = new List<string>();
			public readonly List<string> changedAssets = new List<string>();
			public readonly List<LinkerRestoreIssue> skipped = new List<LinkerRestoreIssue>();
			public readonly List<string> failures = new List<string>();
			private readonly HashSet<string> unsaveableAssets = new HashSet<string>(StringComparer.Ordinal);

			public bool IsValid => string.IsNullOrEmpty(validationError);
			public bool HasCandidates => candidatesFound > 0;

			internal void MarkUnsaveable(string pAssetPath)
			{
				if (!string.IsNullOrEmpty(pAssetPath))
					unsaveableAssets.Add(pAssetPath);
			}

			internal bool IsUnsaveable(string pAssetPath)
			{
				return unsaveableAssets.Contains(pAssetPath);
			}
		}

		private sealed class RestoreRequest
		{
			public CatalogAssetType assetType;
			public string key;
			public Sprite sprite;
			public Texture2D texture;
			public AudioClip audioClip;
		}

		internal static LinkerRestoreResult Preview(AssetCatalog pCatalog, CatalogAssetType pAssetType, string pKey)
		{
			return Run(pCatalog, pAssetType, pKey, false);
		}

		internal static LinkerRestoreResult Restore(AssetCatalog pCatalog, CatalogAssetType pAssetType, string pKey)
		{
			return Run(pCatalog, pAssetType, pKey, true);
		}

		private static LinkerRestoreResult Run(
			AssetCatalog pCatalog,
			CatalogAssetType pAssetType,
			string pKey,
			bool pRestore)
		{
			var result = new LinkerRestoreResult();
			if (!TryCreateRequest(pCatalog, pAssetType, pKey, pRestore, out var request, out var error))
			{
				result.validationError = error;
				return result;
			}

			var prefabPaths = GetAssetPaths("t:Prefab");
			var title = pRestore ? "Restore Linkers" : "Preview Restore Linkers";

			try
			{
				for (int i = 0; i < prefabPaths.Count; i++)
				{
					var path = prefabPaths[i];
					if (ShowProgress(title, path, i, prefabPaths.Count))
					{
						result.cancelled = true;
						return result;
					}

					ProcessPrefab(path, request, result, pRestore);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			return result;
		}

		private static bool TryCreateRequest(
			AssetCatalog pCatalog,
			CatalogAssetType pAssetType,
			string pKey,
			bool pRestore,
			out RestoreRequest pRequest,
			out string pError)
		{
			pRequest = null;
			pError = null;
			pKey = pKey != null ? pKey.Trim() : string.Empty;

			if (pCatalog == null) { pError = "No AssetCatalog loaded."; return false; }
			if (string.IsNullOrEmpty(pKey)) { pError = "No catalog key selected."; return false; }
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				pError = "Exit Play mode before restoring linker components.";
				return false;
			}
			if (pRestore)
			{
				if (!pCatalog.EditorHasKey(pAssetType, pKey))
				{
					pError = $"{pAssetType} key '{pKey}' no longer exists in AssetCatalog.";
					return false;
				}
			}
			else if (!HasCatalogKeyWithoutMigration(pCatalog, pAssetType, pKey))
			{
				pError = $"{pAssetType} key '{pKey}' no longer exists in AssetCatalog.";
				return false;
			}

			pRequest = new RestoreRequest { assetType = pAssetType, key = pKey };
			switch (pAssetType)
			{
				case CatalogAssetType.Sprite:
					pRequest.sprite = pCatalog.GetSprite(pKey);
					if (pRequest.sprite == null) pError = $"Sprite catalog asset for key '{pKey}' is missing.";
					break;
				case CatalogAssetType.Texture2D:
					pRequest.texture = pCatalog.GetTexture(pKey);
					if (pRequest.texture == null) pError = $"Texture2D catalog asset for key '{pKey}' is missing.";
					break;
				case CatalogAssetType.AudioClip:
					pRequest.audioClip = pCatalog.GetAudioClip(pKey);
					if (pRequest.audioClip == null) pError = $"AudioClip catalog asset for key '{pKey}' is missing.";
					break;
				default:
					pError = $"Unsupported catalog type '{pAssetType}'.";
					break;
			}
			return string.IsNullOrEmpty(pError);
		}

		private static bool HasCatalogKeyWithoutMigration(AssetCatalog pCatalog, CatalogAssetType pAssetType, string pKey)
		{
			// AssetCatalog.EditorSprites and EditorHasKey call EditorMigrateSchema, which records Undo and
			// dirties the catalog asset. Preview must stay mutation-free, so read serialized entries instead.
			// Migration only rewrites autoActive and schema version, never key membership, so this key check
			// matches the EditorHasKey check used by the actual restore pass.
			switch (pAssetType)
			{
				case CatalogAssetType.Sprite:
					return SerializedCatalogHasKey(pCatalog, "m_Sprites", pKey);
				case CatalogAssetType.Texture2D:
					return SerializedCatalogHasKey(pCatalog, "m_Textures", pKey);
				case CatalogAssetType.AudioClip:
					return SerializedCatalogHasKey(pCatalog, "m_AudioClips", pKey);
				default:
					return false;
			}
		}

		private static bool SerializedCatalogHasKey(AssetCatalog pCatalog, string pPropertyName, string pKey)
		{
			var serializedCatalog = new SerializedObject(pCatalog);
			var property = serializedCatalog.FindProperty(pPropertyName);
			if (property == null || !property.isArray)
				return false;

			for (int i = 0; i < property.arraySize; i++)
			{
				var entry = property.GetArrayElementAtIndex(i);
				var key = entry.FindPropertyRelative("key");
				if (key != null && key.stringValue == pKey)
					return true;
			}
			return false;
		}

		private static readonly string[] ProjectAssetSearchFolders = { "Assets" };

		private static bool IsProjectAssetPath(string pPath)
		{
			// Restore only scans and writes project prefabs. Package assets stay outside this workflow.
			return !string.IsNullOrEmpty(pPath) && pPath.StartsWith("Assets/", StringComparison.Ordinal);
		}

		private static string BuildDiscardedFailure(string pPath, string pAssetKind, int pRestored)
		{
			return $"{pPath}: {pAssetKind} not saved. All {pRestored} in-memory restore(s) for this " +
				"asset were discarded because an earlier restore failure could not be rolled back. " +
				"Matching linker components remain in this asset.";
		}

		private static List<string> GetAssetPaths(string pFilter)
		{
			var paths = new List<string>();
			foreach (var guid in AssetDatabase.FindAssets(pFilter, ProjectAssetSearchFolders))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (IsProjectAssetPath(path))
					paths.Add(path);
			}
			paths.Sort(StringComparer.Ordinal);
			return paths;
		}

		private static bool ShowProgress(string pTitle, string pPath, int pCurrent, int pTotal)
		{
			var progress = pTotal == 0 ? 1f : (float)pCurrent / pTotal;
			return EditorUtility.DisplayCancelableProgressBar(pTitle, pPath, progress);
		}

		private static int ProcessHierarchy(
			GameObject pRoot,
			string pAssetPath,
			RestoreRequest pRequest,
			LinkerRestoreResult pResult,
			bool pRestore)
		{
			if (pRoot == null)
				return 0;

			switch (pRequest.assetType)
			{
				case CatalogAssetType.Sprite:
					var restored = ProcessSpriteLinkers(pRoot, pAssetPath, pRequest, pResult, pRestore);
					restored += ProcessSpriteRendererLinkers(pRoot, pAssetPath, pRequest, pResult, pRestore);
					return restored;
				case CatalogAssetType.Texture2D:
					return ProcessTextureLinkers(pRoot, pAssetPath, pRequest, pResult, pRestore);
				case CatalogAssetType.AudioClip:
					return ProcessAudioLinkers(pRoot, pAssetPath, pRequest, pResult, pRestore);
				default:
					return 0;
			}
		}

		private static int ProcessSpriteLinkers(
			GameObject pRoot,
			string pAssetPath,
			RestoreRequest pRequest,
			LinkerRestoreResult pResult,
			bool pRestore)
		{
			var restored = 0;
			var linkers = pRoot.GetComponentsInChildren<GeneralSpriteLinker>(true);
			foreach (var linker in linkers)
			{
				if (linker == null || linker.Key != pRequest.key)
					continue;

				var image = linker.GetComponent<Image>();
				if (image == null)
				{
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralSpriteLinker), "Required Image target is missing.");
					continue;
				}

				pResult.candidatesFound++;
				AddUnique(pResult.affectedAssets, pAssetPath);
				if (!pRestore)
					continue;

				var oldSprite = image.sprite;
				var oldOverrideSprite = image.overrideSprite;
				try
				{
					image.sprite = pRequest.sprite;
					image.overrideSprite = null;
					EditorUtility.SetDirty(image);
					UnityEngine.Object.DestroyImmediate(linker);
					restored++;
				}
				catch (Exception ex)
				{
					if (!TryRollbackImage(image, linker, oldSprite, oldOverrideSprite))
						pResult.MarkUnsaveable(pAssetPath);
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralSpriteLinker), $"Restore failed: {ex.Message}");
				}
			}
			return restored;
		}

		private static int ProcessSpriteRendererLinkers(
			GameObject pRoot,
			string pAssetPath,
			RestoreRequest pRequest,
			LinkerRestoreResult pResult,
			bool pRestore)
		{
			var restored = 0;
			var linkers = pRoot.GetComponentsInChildren<GeneralSpriteRendererLinker>(true);
			foreach (var linker in linkers)
			{
				if (linker == null || linker.Key != pRequest.key)
					continue;

				var renderer = linker.GetComponent<SpriteRenderer>();
				if (renderer == null)
				{
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralSpriteRendererLinker), "Required SpriteRenderer target is missing.");
					continue;
				}

				pResult.candidatesFound++;
				AddUnique(pResult.affectedAssets, pAssetPath);
				if (!pRestore)
					continue;

				var oldSprite = renderer.sprite;
				try
				{
					renderer.sprite = pRequest.sprite;
					EditorUtility.SetDirty(renderer);
					UnityEngine.Object.DestroyImmediate(linker);
					restored++;
				}
				catch (Exception ex)
				{
					if (!TryRollbackSpriteRenderer(renderer, linker, oldSprite))
						pResult.MarkUnsaveable(pAssetPath);
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralSpriteRendererLinker), $"Restore failed: {ex.Message}");
				}
			}
			return restored;
		}

		private static int ProcessTextureLinkers(
			GameObject pRoot,
			string pAssetPath,
			RestoreRequest pRequest,
			LinkerRestoreResult pResult,
			bool pRestore)
		{
			var restored = 0;
			var linkers = pRoot.GetComponentsInChildren<GeneralTextureLinker>(true);
			foreach (var linker in linkers)
			{
				if (linker == null || linker.Key != pRequest.key)
					continue;

				var rawImage = linker.GetComponent<RawImage>();
				if (rawImage == null)
				{
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralTextureLinker), "Required RawImage target is missing.");
					continue;

				}

				pResult.candidatesFound++;
				AddUnique(pResult.affectedAssets, pAssetPath);
				if (!pRestore)
					continue;

				var oldTexture = rawImage.texture;
				try
				{
					rawImage.texture = pRequest.texture;
					EditorUtility.SetDirty(rawImage);
					UnityEngine.Object.DestroyImmediate(linker);
					restored++;
				}
				catch (Exception ex)
				{
					if (!TryRollbackRawImage(rawImage, linker, oldTexture))
						pResult.MarkUnsaveable(pAssetPath);
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralTextureLinker), $"Restore failed: {ex.Message}");
				}
			}
			return restored;
		}

		private static int ProcessAudioLinkers(
			GameObject pRoot,
			string pAssetPath,
			RestoreRequest pRequest,
			LinkerRestoreResult pResult,
			bool pRestore)
		{
			var restored = 0;
			var linkers = pRoot.GetComponentsInChildren<GeneralAudioLinker>(true);
			foreach (var linker in linkers)
			{
				if (linker == null || linker.Key != pRequest.key)
					continue;

				var source = linker.GetComponent<AudioSource>();
				if (source == null)
				{
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralAudioLinker), "Required AudioSource target is missing.");
					continue;
				}

				pResult.candidatesFound++;
				AddUnique(pResult.affectedAssets, pAssetPath);
				if (!pRestore)
					continue;

				var oldClip = source.clip;
				try
				{
					source.clip = pRequest.audioClip;
					EditorUtility.SetDirty(source);
					UnityEngine.Object.DestroyImmediate(linker);
					restored++;
				}
				catch (Exception ex)
				{
					if (!TryRollbackAudioSource(source, linker, oldClip))
						pResult.MarkUnsaveable(pAssetPath);
					AddSkipped(pResult, pAssetPath, linker, nameof(GeneralAudioLinker), $"Restore failed: {ex.Message}");
				}
			}
			return restored;
		}

		private static bool TryRollbackImage(Image pImage, Component pLinker, Sprite pSprite, Sprite pOverrideSprite)
		{
			try
			{
				pImage.sprite = pSprite;
				pImage.overrideSprite = pOverrideSprite;
				return pLinker != null;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryRollbackSpriteRenderer(SpriteRenderer pRenderer, Component pLinker, Sprite pSprite)
		{
			try
			{
				pRenderer.sprite = pSprite;
				return pLinker != null;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryRollbackRawImage(RawImage pRawImage, Component pLinker, Texture pTexture)
		{
			try
			{
				pRawImage.texture = pTexture;
				return pLinker != null;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryRollbackAudioSource(AudioSource pSource, Component pLinker, AudioClip pClip)
		{
			try
			{
				pSource.clip = pClip;
				return pLinker != null;
			}
			catch
			{
				return false;
			}
		}

		private static void ProcessPrefab(
			string pPath,
			RestoreRequest pRequest,
			LinkerRestoreResult pResult,
			bool pRestore)
		{
			pResult.prefabsScanned++;
			var displayPath = AssetCatalogEditorGui.StripAssetsPrefix(pPath);
			GameObject root = null;
			try
			{
				root = PrefabUtility.LoadPrefabContents(pPath);
				if (root == null)
				{
					pResult.failures.Add($"{displayPath}: prefab load failed.");
					return;
				}

				var restored = ProcessHierarchy(root, pPath, pRequest, pResult, pRestore);
				if (!pRestore || restored == 0)
					return;

				if (pResult.IsUnsaveable(pPath))
				{
					pResult.failures.Add(BuildDiscardedFailure(displayPath, "prefab", restored));
					return;
				}

				PrefabUtility.SaveAsPrefabAsset(root, pPath, out var success);
				if (!success)
				{
					pResult.failures.Add($"{displayPath}: prefab save failed after {restored} in-memory restore(s).");
					return;
				}
				pResult.prefabsChanged++;
				pResult.linkersRestored += restored;
				AddUnique(pResult.changedAssets, pPath);
			}
			catch (Exception ex)
			{
				pResult.failures.Add($"{displayPath}: {ex.Message}");
			}
			finally
			{
				if (root != null)
				{
					try { PrefabUtility.UnloadPrefabContents(root); }
					catch (Exception ex) { pResult.failures.Add($"{displayPath}: prefab unload failed: {ex.Message}"); }
				}
			}
		}

		private static void AddSkipped(
			LinkerRestoreResult pResult,
			string pAssetPath,
			Component pLinker,
			string pComponentType,
			string pReason)
		{
			pResult.skipped.Add(new LinkerRestoreIssue
			{
				assetPath = pAssetPath,
				hierarchyPath = GetHierarchyPath(pLinker != null ? pLinker.transform : null),
				componentType = pComponentType,
				reason = pReason,
			});
		}

		private static string GetHierarchyPath(Transform pTransform)
		{
			if (pTransform == null)
				return "<missing>";

			var names = new List<string>();
			for (var current = pTransform; current != null; current = current.parent)
				names.Add(current.gameObject.name);
			names.Reverse();
			return string.Join("/", names);
		}

		private static void AddUnique(List<string> pValues, string pValue)
		{
			if (!string.IsNullOrEmpty(pValue) && !pValues.Contains(pValue))
				pValues.Add(pValue);
		}
	}
}
