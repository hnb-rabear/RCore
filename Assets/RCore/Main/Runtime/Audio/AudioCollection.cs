/**
 * Author HNB-RaBear - 2021
 **/

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using RCore.Inspector;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

namespace RCore.Audio
{
    /// <summary>
    /// A ScriptableObject that serves as a central repository for all audio clips in the game.
    /// It supports both directly referenced AudioClips and clips loaded via the Addressable Assets system.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCollection", menuName = "RCore/Audio Collection")]
    public class AudioCollection : ScriptableObject
    {
        /// <summary>
        /// Contains settings for the automatic generation of audio ID scripts.
        /// This helps in accessing audio clips in a type-safe manner using generated constants or enums.
        /// </summary>
        [System.Serializable]
        public struct ScriptGenerator
        {
            [Tooltip("The namespace for the generated C# script.")]
            public string @namespace;
            [Tooltip("The source folder for music files to be processed by the generator.")]
            public string inputMusicsFolder;
            [Tooltip("The source folder for sound effect files to be processed by the generator.")]
            public string inputSfxsFolder;
            [Tooltip("The destination folder for the generated script file.")]
            public string outputIDsFolder;
        }

        [Tooltip("Array of directly referenced sound effect clips.")]
        public AudioClip[] sfxClips;
        [Tooltip("Array of directly referenced music clips.")]
        public AudioClip[] musicClips;
        [Tooltip("Addressable asset references for sound effect clips that are loaded on demand.")]
        public AssetReferenceT<AudioClip>[] abSfxClips;
        [Tooltip("Addressable asset references for music clips that are loaded on demand.")]
        public AssetReferenceT<AudioClip>[] abMusicClips;
        [Tooltip("Settings for the automatic audio script generator.")]
        public ScriptGenerator generator;

        /// <summary>
        /// Retrieves a music clip by its index in the musicClips array.
        /// </summary>
        /// <param name="pIndex">The index of the music clip.</param>
        /// <returns>The AudioClip at the specified index, or null if the index is out of bounds.</returns>
        public AudioClip GetMusicClip(int pIndex)
        {
            if (pIndex >= 0 && pIndex < musicClips.Length)
                return musicClips[pIndex];
            return null;
        }

        /// <summary>
        /// Retrieves a music clip by its name (case-insensitive).
        /// </summary>
        /// <param name="pKey">The name of the music clip to find.</param>
        /// <returns>The matching AudioClip, or null if not found.</returns>
        public AudioClip GetMusicClip(string pKey)
        {
            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i] != null && musicClips[i].name.Equals(pKey, System.StringComparison.OrdinalIgnoreCase))
                    return musicClips[i];
            }
            return null;
        }

        /// <summary>
        /// Retrieves a music clip by its name and returns its index.
        /// </summary>
        /// <param name="pKey">The name of the music clip to find.</param>
        /// <param name="pIndex">The index of the found clip will be assigned to this variable.</param>
        /// <returns>The matching AudioClip, or null if not found.</returns>
        public AudioClip GetMusicClip(string pKey, ref int pIndex)
        {
            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i] != null && musicClips[i].name.Equals(pKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    pIndex = i;
                    return musicClips[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves a sound effect clip by its index in the sfxClips array.
        /// </summary>
        /// <param name="pIndex">The index of the SFX clip.</param>
        /// <returns>The AudioClip at the specified index, or null if the index is out of bounds.</returns>
        public AudioClip GetSFXClip(int pIndex)
        {
            if (pIndex >= 0 && pIndex < sfxClips.Length)
                return sfxClips[pIndex];
            return null;
        }

        /// <summary>
        /// Retrieves a sound effect clip by its name (case-insensitive).
        /// </summary>
        /// <param name="pName">The name of the SFX clip to find.</param>
        /// <returns>The matching AudioClip, or null if not found.</returns>
        public AudioClip GetSFXClip(string pName)
        {
            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i] != null && sfxClips[i].name.Equals(pName, System.StringComparison.OrdinalIgnoreCase))
                    return sfxClips[i];
            }
            return null;
        }

        /// <summary>
        /// Retrieves a sound effect clip by its name and returns its index.
        /// </summary>
        /// <param name="pKey">The name of the SFX clip to find.</param>
        /// <param name="pIndex">The index of the found clip will be assigned to this out variable.</param>
        /// <returns>The matching AudioClip, or null if not found.</returns>
        public AudioClip GetSFXClip(string pKey, out int pIndex)
        {
            pIndex = -1;
            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i] != null && sfxClips[i].name.Equals(pKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    pIndex = i;
                    return sfxClips[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Gets an array of all sound effect clip names.
        /// </summary>
        /// <returns>A string array containing the names of all SFX clips.</returns>
        public string[] GetSFXNames()
        {
            var names = new string[sfxClips.Length];
            for (var i = 0; i < sfxClips.Length; i++)
                if(sfxClips[i] != null)
                    names[i] = sfxClips[i].name;
            return names;
        }

#region Addressable Assets

        /// <summary>
        /// Asynchronously loads an Addressable sound effect clip at a specific index
        /// and assigns it to the `sfxClips` array.
        /// </summary>
        /// <param name="pIndex">The index of the clip to load.</param>
        public async Task LoadABSfx(int pIndex)
        {
            if (pIndex < 0 || pIndex >= abSfxClips.Length || sfxClips[pIndex] != null)
                return;

            var asset = abSfxClips[pIndex];
            // Check if the asset reference is valid
            if (asset != null && !string.IsNullOrEmpty(asset.AssetGUID))
            {
                // If the handle is already valid, use it; otherwise, start a new load operation.
                var operation = asset.IsValid() ? asset.OperationHandle : asset.LoadAssetAsync<AudioClip>();
                await operation.Task;
                if (operation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    sfxClips[pIndex] = operation.Result as AudioClip;
                }
            }
        }

        /// <summary>
        /// Releases the asset associated with an Addressable sound effect clip,
        /// freeing up memory. It also nullifies the entry in the `sfxClips` array.
        /// </summary>
        /// <param name="pIndex">The index of the clip to unload.</param>
        public void UnloadABSfx(int pIndex)
        {
            if (pIndex < 0 || pIndex >= abSfxClips.Length)
                return;

            var ab = abSfxClips[pIndex];
            if (ab != null && ab.IsValid())
            {
                sfxClips[pIndex] = null;
                ab.ReleaseAsset();
            }
        }

        /// <summary>
        /// Asynchronously loads an Addressable music clip at a specific index
        /// and assigns it to the `musicClips` array.
        /// </summary>
        /// <param name="pIndex">The index of the clip to load.</param>
        public async Task LoadABMusic(int pIndex)
        {
            if (pIndex < 0 || pIndex >= abMusicClips.Length || musicClips[pIndex] != null)
                return;

            var ab = abMusicClips[pIndex];
            if (ab != null && !string.IsNullOrEmpty(ab.AssetGUID))
            {
                var operation = ab.IsValid() ? ab.OperationHandle : ab.LoadAssetAsync<AudioClip>();
                await operation.Task;
                if (operation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    musicClips[pIndex] = operation.Result as AudioClip;
                }
            }
        }

        /// <summary>
        /// Releases the asset associated with an Addressable music clip,
        /// freeing up memory. It also nullifies the entry in the `musicClips` array.
        /// </summary>
        /// <param name="pIndex">The index of the clip to unload.</param>
        public void UnloadABMusic(int pIndex)
        {
            if (pIndex < 0 || pIndex >= abMusicClips.Length)
                return;
                
            var ab = abMusicClips[pIndex];
            if (ab != null && ab.IsValid())
            {
                musicClips[pIndex] = null;
                ab.ReleaseAsset();
            }
        }

#endregion
    }
}