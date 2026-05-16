using System.Threading.Tasks;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace RevCore
{
    /// <summary>
    /// ScriptableObject holding a project's sound effect and music clip references. Looked up by
    /// name or index. When <c>ADDRESSABLES</c> is defined, parallel <c>AssetReferenceT</c> arrays
    /// can lazy-load clips on demand to keep memory low.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCollection", menuName = "RevCore/Audio Collection")]
    public class AudioCollection : ScriptableObject
    {
        /// <summary>Code-generation config for the optional clip-ID enum generator (editor tooling).</summary>
        [System.Serializable]
        public struct ScriptGenerator
        {
            /// <summary>Namespace of the generated enum.</summary>
            public string @namespace;
            /// <summary>Folder containing music clips to enumerate.</summary>
            [FolderPath] public string inputMusicsFolder;
            /// <summary>Folder containing SFX clips to enumerate.</summary>
            [FolderPath] public string inputSfxsFolder;
            /// <summary>Folder where the generated enum source file is written.</summary>
            [FolderPath] public string outputIDsFolder;
        }

        /// <summary>SFX clips authored directly (non-addressable).</summary>
        public AudioClip[] sfxClips;
        /// <summary>Music clips authored directly (non-addressable).</summary>
        public AudioClip[] musicClips;
#if ADDRESSABLES
        /// <summary>Addressable SFX clip references; index-aligned with <see cref="sfxClips"/>.</summary>
        public AssetReferenceT<AudioClip>[] abSfxClips;
        /// <summary>Addressable music clip references; index-aligned with <see cref="musicClips"/>.</summary>
        public AssetReferenceT<AudioClip>[] abMusicClips;
#endif
        /// <summary>Editor-only generator configuration.</summary>
        public ScriptGenerator generator;

        /// <summary>Returns the music clip at <paramref name="index"/>, or <c>null</c> when out of range.</summary>
        public AudioClip GetMusicClip(int index)
        {
            if (musicClips != null && index >= 0 && index < musicClips.Length)
                return musicClips[index];
            return null;
        }

        /// <summary>Looks up a music clip by case-insensitive name match. Returns <c>null</c> if not found.</summary>
        public AudioClip GetMusicClip(string key)
        {
            if (string.IsNullOrEmpty(key) || musicClips == null)
                return null;

            for (int i = 0; i < musicClips.Length; i++)
                if (musicClips[i] != null && musicClips[i].name.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                    return musicClips[i];
            return null;
        }

        /// <summary>Looks up a music clip by case-insensitive name match and returns its array index via <paramref name="index"/>. Index is -1 on miss.</summary>
        public AudioClip GetMusicClip(string key, ref int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(key) || musicClips == null)
                return null;

            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i] != null && musicClips[i].name.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    return musicClips[i];
                }
            }
            return null;
        }

        /// <summary>Returns the SFX clip at <paramref name="index"/>, or <c>null</c> when out of range.</summary>
        public AudioClip GetSFXClip(int index)
        {
            if (sfxClips != null && index >= 0 && index < sfxClips.Length)
                return sfxClips[index];
            return null;
        }

        /// <summary>Looks up an SFX clip by case-insensitive name match. Returns <c>null</c> if not found.</summary>
        public AudioClip GetSFXClip(string name)
        {
            if (string.IsNullOrEmpty(name) || sfxClips == null)
                return null;

            for (int i = 0; i < sfxClips.Length; i++)
                if (sfxClips[i] != null && sfxClips[i].name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return sfxClips[i];
            return null;
        }

        /// <summary>Looks up an SFX clip by case-insensitive name match and returns its array index via <paramref name="index"/>. Index is -1 on miss.</summary>
        public AudioClip GetSFXClip(string key, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(key) || sfxClips == null)
                return null;

            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i] != null && sfxClips[i].name.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    return sfxClips[i];
                }
            }
            return null;
        }

        /// <summary>Returns the names of every non-null entry in <see cref="sfxClips"/>. Allocates a fresh array.</summary>
        public string[] GetSFXNames()
        {
            if (sfxClips == null)
                return System.Array.Empty<string>();

            var names = new string[sfxClips.Length];
            for (int i = 0; i < sfxClips.Length; i++)
                if (sfxClips[i] != null)
                    names[i] = sfxClips[i].name;
            return names;
        }

#if ADDRESSABLES
        /// <summary>Lazy-loads the addressable SFX clip at <paramref name="index"/> into <see cref="sfxClips"/>. No-op if already loaded or index out of range.</summary>
        public async Task LoadABSfx(int index)
        {
            if (abSfxClips == null || index < 0 || index >= abSfxClips.Length)
                return;
            sfxClips ??= new AudioClip[abSfxClips.Length];
            if (index >= sfxClips.Length || sfxClips[index] != null)
                return;
            var asset = abSfxClips[index];
            if (asset != null && !string.IsNullOrEmpty(asset.AssetGUID))
            {
                var operation = asset.IsValid() ? asset.OperationHandle : asset.LoadAssetAsync<AudioClip>();
                await operation.Task;
                if (operation.Status == AsyncOperationStatus.Succeeded)
                    sfxClips[index] = operation.Result as AudioClip;
            }
        }

        /// <summary>Releases the addressable SFX asset at <paramref name="index"/> and clears the corresponding <see cref="sfxClips"/> slot.</summary>
        public void UnloadABSfx(int index)
        {
            if (abSfxClips == null || index < 0 || index >= abSfxClips.Length)
                return;
            var ab = abSfxClips[index];
            if (ab != null && ab.IsValid())
            {
                if (sfxClips != null && index < sfxClips.Length)
                    sfxClips[index] = null;
                ab.ReleaseAsset();
            }
        }

        /// <summary>Lazy-loads the addressable music clip at <paramref name="index"/> into <see cref="musicClips"/>.</summary>
        public async Task LoadABMusic(int index)
        {
            if (abMusicClips == null || index < 0 || index >= abMusicClips.Length)
                return;
            musicClips ??= new AudioClip[abMusicClips.Length];
            if (index >= musicClips.Length || musicClips[index] != null)
                return;
            var ab = abMusicClips[index];
            if (ab != null && !string.IsNullOrEmpty(ab.AssetGUID))
            {
                var operation = ab.IsValid() ? ab.OperationHandle : ab.LoadAssetAsync<AudioClip>();
                await operation.Task;
                if (operation.Status == AsyncOperationStatus.Succeeded)
                    musicClips[index] = operation.Result as AudioClip;
            }
        }

        /// <summary>Releases the addressable music asset at <paramref name="index"/> and clears the corresponding <see cref="musicClips"/> slot.</summary>
        public void UnloadABMusic(int index)
        {
            if (abMusicClips == null || index < 0 || index >= abMusicClips.Length)
                return;
            var ab = abMusicClips[index];
            if (ab != null && ab.IsValid())
            {
                if (musicClips != null && index < musicClips.Length)
                    musicClips[index] = null;
                ab.ReleaseAsset();
            }
        }
#endif
    }
}
