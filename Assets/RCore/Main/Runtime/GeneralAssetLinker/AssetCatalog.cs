using System;
using System.Collections.Generic;
using RCore.Inspector;
using UnityEngine;

namespace RCore.Config
{
	public enum CatalogAssetType
	{
		Sprite,
		Texture2D,
		AudioClip,
	}

	[Serializable]
	public class SpriteCatalogEntry
	{
		public string key;
		public string category;
		[SpriteBox] public Sprite asset;
#if UNITY_EDITOR
		[System.NonSerialized] public List<string> prefabUsages;
		[System.NonSerialized] public List<string> sceneUsages;
#endif
	}

	[Serializable]
	public class TextureCatalogEntry
	{
		public string key;
		public string category;
		[SpriteBox] public Texture2D asset;
#if UNITY_EDITOR
		[System.NonSerialized] public List<string> prefabUsages;
		[System.NonSerialized] public List<string> sceneUsages;
#endif
	}

	[Serializable]
	public class AudioCatalogEntry
	{
		public string key;
		public string category;
		public AudioClip asset;
#if UNITY_EDITOR
		[System.NonSerialized] public List<string> prefabUsages;
		[System.NonSerialized] public List<string> sceneUsages;
#endif
	}

	[CreateAssetMenu(fileName = "AssetCatalog", menuName = "RCore/Config/Asset Catalog")]
	public class AssetCatalog : ScriptableObject
	{
		[SerializeField] private List<SpriteCatalogEntry> m_Sprites = new List<SpriteCatalogEntry>();
		[SerializeField] private List<TextureCatalogEntry> m_Textures = new List<TextureCatalogEntry>();
		[SerializeField] private List<AudioCatalogEntry> m_AudioClips = new List<AudioCatalogEntry>();

		private Dictionary<string, Sprite> m_SpriteLookup;
		private Dictionary<string, Texture2D> m_TextureLookup;
		private Dictionary<string, AudioClip> m_AudioLookup;
		private bool m_LookupCacheDirty = true;

		private static AssetCatalog m_Instance;
		public static AssetCatalog Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = Resources.Load<AssetCatalog>(nameof(AssetCatalog));
				return m_Instance;
			}
		}

		private void EnsureLookupCache()
		{
			if (!m_LookupCacheDirty && m_SpriteLookup != null && m_TextureLookup != null && m_AudioLookup != null)
				return;

			m_SpriteLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
			m_TextureLookup = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
			m_AudioLookup = new Dictionary<string, AudioClip>(StringComparer.Ordinal);

			foreach (var entry in m_Sprites)
				AddIfMissing(m_SpriteLookup, entry.key, entry.asset);
			foreach (var entry in m_Textures)
				AddIfMissing(m_TextureLookup, entry.key, entry.asset);
			foreach (var entry in m_AudioClips)
				AddIfMissing(m_AudioLookup, entry.key, entry.asset);

			m_LookupCacheDirty = false;
		}

		private void InvalidateLookupCache()
		{
			m_LookupCacheDirty = true;
		}

		private static void AddIfMissing<T>(Dictionary<string, T> pLookup, string pKey, T pAsset) where T : UnityEngine.Object
		{
			if (string.IsNullOrEmpty(pKey) || pLookup.ContainsKey(pKey))
				return;
			pLookup.Add(pKey, pAsset);
		}

		public Sprite GetSprite(string key)
		{
			if (string.IsNullOrEmpty(key))
				return null;
			EnsureLookupCache();
			if (m_SpriteLookup.TryGetValue(key, out var sprite))
				return sprite;
			Debug.LogError($"[AssetCatalog] Not found Sprite entry for key '{key}'.");
			return null;
		}

		public Texture2D GetTexture(string key)
		{
			if (string.IsNullOrEmpty(key))
				return null;
			EnsureLookupCache();
			if (m_TextureLookup.TryGetValue(key, out var texture))
				return texture;
			Debug.LogError($"[AssetCatalog] Not found Texture2D entry for key '{key}'.");
			return null;
		}

		public AudioClip GetAudioClip(string key)
		{
			if (string.IsNullOrEmpty(key))
				return null;
			EnsureLookupCache();
			if (m_AudioLookup.TryGetValue(key, out var audioClip))
				return audioClip;
			Debug.LogError($"[AssetCatalog] Not found AudioClip entry for key '{key}'.");
			return null;
		}

#if UNITY_EDITOR
		public IReadOnlyList<SpriteCatalogEntry> EditorSprites => m_Sprites;
		public IReadOnlyList<TextureCatalogEntry> EditorTextures => m_Textures;
		public IReadOnlyList<AudioCatalogEntry> EditorAudioClips => m_AudioClips;

		public void EditorAddSprite(string key, string category, Sprite asset)
		{
			UnityEditor.Undo.RecordObject(this, "Quick Add Asset");
			foreach (var e in m_Sprites)
			{
				if (e.key == key)
				{
					e.category = category;
					e.asset = asset;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
			m_Sprites.Add(new SpriteCatalogEntry { key = key, category = category, asset = asset });
			InvalidateLookupCache();
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorAddTexture(string key, string category, Texture2D asset)
		{
			UnityEditor.Undo.RecordObject(this, "Quick Add Asset");
			foreach (var e in m_Textures)
			{
				if (e.key == key)
				{
					e.category = category;
					e.asset = asset;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
			m_Textures.Add(new TextureCatalogEntry { key = key, category = category, asset = asset });
			InvalidateLookupCache();
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorAddAudioClip(string key, string category, AudioClip asset)
		{
			UnityEditor.Undo.RecordObject(this, "Quick Add Asset");
			foreach (var e in m_AudioClips)
			{
				if (e.key == key)
				{
					e.category = category;
					e.asset = asset;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
			m_AudioClips.Add(new AudioCatalogEntry { key = key, category = category, asset = asset });
			InvalidateLookupCache();
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public string[] EditorGetDistinctCategories(CatalogAssetType type)
		{
			var set = new HashSet<string>();
			switch (type)
			{
				case CatalogAssetType.Sprite:
					foreach (var e in m_Sprites) if (!string.IsNullOrEmpty(e.category)) set.Add(e.category);
					break;
				case CatalogAssetType.Texture2D:
					foreach (var e in m_Textures) if (!string.IsNullOrEmpty(e.category)) set.Add(e.category);
					break;
				case CatalogAssetType.AudioClip:
					foreach (var e in m_AudioClips) if (!string.IsNullOrEmpty(e.category)) set.Add(e.category);
					break;
			}
			var arr = new string[set.Count];
			set.CopyTo(arr);
			Array.Sort(arr);
			return arr;
		}

		public bool EditorHasKey(CatalogAssetType pType, string pKey)
		{
			if (string.IsNullOrEmpty(pKey))
				return false;

			switch (pType)
			{
				case CatalogAssetType.Sprite:
					foreach (var entry in m_Sprites)
					{
						if (entry.key == pKey)
							return true;
					}
					break;
				case CatalogAssetType.Texture2D:
					foreach (var entry in m_Textures)
					{
						if (entry.key == pKey)
							return true;
					}
					break;
				case CatalogAssetType.AudioClip:
					foreach (var entry in m_AudioClips)
					{
						if (entry.key == pKey)
							return true;
					}
					break;
			}
			return false;
		}

		public bool EditorTryRenameKey(CatalogAssetType pType, string pOldKey, string pNewKey, out string pError)
		{
			pError = null;
			if (string.IsNullOrEmpty(pOldKey))
			{
				pError = "Old key is empty.";
				return false;
			}
			if (string.IsNullOrEmpty(pNewKey))
			{
				pError = "New key is empty.";
				return false;
			}
			if (pOldKey == pNewKey)
			{
				pError = "Old key and new key are the same.";
				return false;
			}
			if (!EditorHasKey(pType, pOldKey))
			{
				pError = $"Old key '{pOldKey}' not found in AssetCatalog.";
				return false;
			}
			if (EditorHasKey(pType, pNewKey))
			{
				pError = $"New key '{pNewKey}' already exists in AssetCatalog.";
				return false;
			}

			UnityEditor.Undo.RecordObject(this, "Rename AssetCatalog Key");
			switch (pType)
			{
				case CatalogAssetType.Sprite:
					foreach (var entry in m_Sprites)
					{
						if (entry.key == pOldKey)
						{
							entry.key = pNewKey;
							InvalidateLookupCache();
							UnityEditor.EditorUtility.SetDirty(this);
							return true;
						}
					}
					break;
				case CatalogAssetType.Texture2D:
					foreach (var entry in m_Textures)
					{
						if (entry.key == pOldKey)
						{
							entry.key = pNewKey;
							InvalidateLookupCache();
							UnityEditor.EditorUtility.SetDirty(this);
							return true;
						}
					}
					break;
				case CatalogAssetType.AudioClip:
					foreach (var entry in m_AudioClips)
					{
						if (entry.key == pOldKey)
						{
							entry.key = pNewKey;
							InvalidateLookupCache();
							UnityEditor.EditorUtility.SetDirty(this);
							return true;
						}
					}
					break;
			}

			pError = $"Old key '{pOldKey}' not found in AssetCatalog.";
			return false;
		}

		public void EditorDeleteEntry(CatalogAssetType pType, string pKey)
		{
			UnityEditor.Undo.RecordObject(this, "Delete AssetCatalog Entry");
			switch (pType)
			{
				case CatalogAssetType.Sprite:
					m_Sprites.RemoveAll(e => e.key == pKey);
					break;
				case CatalogAssetType.Texture2D:
					m_Textures.RemoveAll(e => e.key == pKey);
					break;
				case CatalogAssetType.AudioClip:
					m_AudioClips.RemoveAll(e => e.key == pKey);
					break;
			}
			InvalidateLookupCache();
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public void EditorUpdateSpriteCategory(string pKey, string pCategory)
		{
			UnityEditor.Undo.RecordObject(this, "Update Sprite Category");
			foreach (var e in m_Sprites)
			{
				if (e.key == pKey)
				{
					e.category = pCategory;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
		}

		public void EditorUpdateSpriteAsset(string pKey, Sprite pAsset)
		{
			UnityEditor.Undo.RecordObject(this, "Update Sprite Asset");
			foreach (var e in m_Sprites)
			{
				if (e.key == pKey)
				{
					e.asset = pAsset;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
		}

		public void EditorUpdateTextureCategory(string pKey, string pCategory)
		{
			UnityEditor.Undo.RecordObject(this, "Update Texture Category");
			foreach (var e in m_Textures)
			{
				if (e.key == pKey)
				{
					e.category = pCategory;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
		}

		public void EditorUpdateTextureAsset(string pKey, Texture2D pAsset)
		{
			UnityEditor.Undo.RecordObject(this, "Update Texture Asset");
			foreach (var e in m_Textures)
			{
				if (e.key == pKey)
				{
					e.asset = pAsset;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
		}

		public void EditorUpdateAudioClipCategory(string pKey, string pCategory)
		{
			UnityEditor.Undo.RecordObject(this, "Update AudioClip Category");
			foreach (var e in m_AudioClips)
			{
				if (e.key == pKey)
				{
					e.category = pCategory;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
		}

		public void EditorUpdateAudioClipAsset(string pKey, AudioClip pAsset)
		{
			UnityEditor.Undo.RecordObject(this, "Update AudioClip Asset");
			foreach (var e in m_AudioClips)
			{
				if (e.key == pKey)
				{
					e.asset = pAsset;
					InvalidateLookupCache();
					UnityEditor.EditorUtility.SetDirty(this);
					return;
				}
			}
		}

		public void EditorEnsureUsageCache()
		{
			foreach (var entry in m_Sprites)
			{
				entry.prefabUsages ??= new List<string>();
				entry.sceneUsages ??= new List<string>();
			}
			foreach (var entry in m_Textures)
			{
				entry.prefabUsages ??= new List<string>();
				entry.sceneUsages ??= new List<string>();
			}
			foreach (var entry in m_AudioClips)
			{
				entry.prefabUsages ??= new List<string>();
				entry.sceneUsages ??= new List<string>();
			}
		}

		public void EditorClearUsageCache()
		{
			EditorEnsureUsageCache();
			foreach (var entry in m_Sprites)
			{
				entry.prefabUsages.Clear();
				entry.sceneUsages.Clear();
			}
			foreach (var entry in m_Textures)
			{
				entry.prefabUsages.Clear();
				entry.sceneUsages.Clear();
			}
			foreach (var entry in m_AudioClips)
			{
				entry.prefabUsages.Clear();
				entry.sceneUsages.Clear();
			}
			InvalidateLookupCache();
		}
#endif

		private void OnValidate()
		{
			InvalidateLookupCache();
		}
	}
}