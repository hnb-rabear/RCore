using System.Threading.Tasks;
using RevCore.Inspector;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace RevCore
{
    [CreateAssetMenu(fileName = "AudioCollection", menuName = "RevCore/Audio Collection")]
    public class AudioCollection : ScriptableObject
    {
        [System.Serializable]
        public struct ScriptGenerator
        {
            public string @namespace;
            [FolderPath] public string inputMusicsFolder;
            [FolderPath] public string inputSfxsFolder;
            [FolderPath] public string outputIDsFolder;
        }

        public AudioClip[] sfxClips;
        public AudioClip[] musicClips;
#if ADDRESSABLES
        public AssetReferenceT<AudioClip>[] abSfxClips;
        public AssetReferenceT<AudioClip>[] abMusicClips;
#endif
        public ScriptGenerator generator;

        public AudioClip GetMusicClip(int index)
        {
            if (index >= 0 && index < musicClips.Length)
                return musicClips[index];
            return null;
        }

        public AudioClip GetMusicClip(string key)
        {
            for (int i = 0; i < musicClips.Length; i++)
                if (musicClips[i] != null && musicClips[i].name.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                    return musicClips[i];
            return null;
        }

        public AudioClip GetMusicClip(string key, ref int index)
        {
            index = -1;
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

        public AudioClip GetSFXClip(int index)
        {
            if (index >= 0 && index < sfxClips.Length)
                return sfxClips[index];
            return null;
        }

        public AudioClip GetSFXClip(string name)
        {
            for (int i = 0; i < sfxClips.Length; i++)
                if (sfxClips[i] != null && sfxClips[i].name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return sfxClips[i];
            return null;
        }

        public AudioClip GetSFXClip(string key, out int index)
        {
            index = -1;
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

        public string[] GetSFXNames()
        {
            var names = new string[sfxClips.Length];
            for (int i = 0; i < sfxClips.Length; i++)
                if (sfxClips[i] != null)
                    names[i] = sfxClips[i].name;
            return names;
        }

#if ADDRESSABLES
        public async Task LoadABSfx(int index)
        {
            if (index < 0 || index >= abSfxClips.Length || sfxClips[index] != null)
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

        public void UnloadABSfx(int index)
        {
            if (index < 0 || index >= abSfxClips.Length)
                return;
            var ab = abSfxClips[index];
            if (ab != null && ab.IsValid())
            {
                sfxClips[index] = null;
                ab.ReleaseAsset();
            }
        }

        public async Task LoadABMusic(int index)
        {
            if (index < 0 || index >= abMusicClips.Length || musicClips[index] != null)
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

        public void UnloadABMusic(int index)
        {
            if (index < 0 || index >= abMusicClips.Length)
                return;
            var ab = abMusicClips[index];
            if (ab != null && ab.IsValid())
            {
                musicClips[index] = null;
                ab.ReleaseAsset();
            }
        }
#endif
    }
}
