#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using RCore.Inspector;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RCore.Audio
{
    [CreateAssetMenu(fileName = "AudioCollection", menuName = "RCore/Audio Collection")]
    public class AudioCollection : ScriptableObject
    {
        [System.Serializable]
        public struct ScriptGenerator
        {
            public string @namespace;
            public string inputMusicsFolder;
            public string inputSfxsFolder;
            public string outputIDsFolder;
        }
        
        public AudioClip[] sfxClips;
        public AudioClip[] musicClips;
        public AssetReferenceT<AudioClip>[] abSfxClips;
        public AssetReferenceT<AudioClip>[] abMusicClips;
        public ScriptGenerator generator;
        
        public AudioClip GetMusicClip(int pIndex)
        {
            if (pIndex < musicClips.Length)
                return musicClips[pIndex];
            return null;
        }

        public AudioClip GetMusicClip(string pKey)
        {
            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i].name.ToLower() == pKey.ToLower())
                    return musicClips[i];
            }
            return null;
        }

        public AudioClip GetMusicClip(string pKey, ref int pIndex)
        {
            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i].name.ToLower() == pKey.ToLower())
                {
                    pIndex = i;
                    return musicClips[i];
                }
            }
            return null;
        }

        public AudioClip GetSFXClip(int pIndex)
        {
            if (pIndex < sfxClips.Length)
                return sfxClips[pIndex];
            return null;
        }

        public AudioClip GetSFXClip(string pName)
        {
            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i].name.ToLower() == pName.ToLower())
                    return sfxClips[i];
            }
            return null;
        }

        public AudioClip GetSFXClip(string pKey, out int pIndex)
        {
            pIndex = -1;
            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i].name.ToLower() == pKey.ToLower())
                {
                    pIndex = i;
                    return sfxClips[i];
                }
            }
            return null;
        }

        public string[] GetSFXNames()
        {
            var names = new string[sfxClips.Length];
            for (var i = 0; i < sfxClips.Length; i++)
                names[i] = sfxClips[i].name;
            return names;
        }

#region Addressable Assets

        public async void LoadABSfx(int pIndex)
        {
            if (pIndex < 0 || pIndex >= sfxClips.Length || sfxClips[pIndex] != null)
                return;
            var asset = abSfxClips[pIndex];
            if (asset != null)
            {
                var operation = asset.IsValid() ? asset.OperationHandle.Convert<AudioClip>() : asset.LoadAssetAsync<AudioClip>();
                await operation.Task;
                sfxClips[pIndex] = operation.Result;
            }
        }

        public void UnloadABSfx(int pIndex)
        {
            var ab = abSfxClips[pIndex];
            if (ab != null)
            {
                sfxClips[pIndex] = null;
                ab.ReleaseAsset();
            }
        }
        
        public async void LoadABMusic(int pIndex)
        {
            if (pIndex < 0 || pIndex >= musicClips.Length || musicClips[pIndex] != null)
                return;
            var ab = abMusicClips[pIndex];
            if (ab != null)
            {
                var operation = Addressables.LoadAssetAsync<AudioClip>(ab);
                await operation.Task;
                musicClips[pIndex] = operation.Result;
            }
        }
        
        public void UnloadABMusic(int pIndex)
        {
            var ab = abMusicClips[pIndex];
            if (ab != null)
            {
                musicClips[pIndex] = null;
                ab.ReleaseAsset();
            }
        }
        
#endregion
    }
}