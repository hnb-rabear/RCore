using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
#if ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;
#endif

namespace RevCore.Editor
{
    internal static class AudioCollectionMenuTools
    {
        private const int Priority = 1600;

        [MenuItem("RevCore/Audio/Create Audio Collection", priority = Priority)]
        private static void CreateAudioCollection()
        {
            string folder = GetSelectedFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "AudioCollection.asset").Replace("\\", "/"));
            var collection = ScriptableObject.CreateInstance<AudioCollection>();
            AssetDatabase.CreateAsset(collection, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = collection;
        }

        [MenuItem("RevCore/Audio/Generate Audio IDs", priority = Priority + 1)]
        private static void GenerateSelectedAudioIDs()
        {
            if (!TryGetSelectedCollection(out var collection))
                return;

            Generate(collection);
        }

        [MenuItem("RevCore/Audio/Sort Active Collection", priority = Priority + 2)]
        private static void SortSelectedCollection()
        {
            if (!TryGetSelectedCollection(out var collection))
                return;

            Sort(collection);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }

#if ADDRESSABLES
        [MenuItem("RevCore/Audio/Validate Addressable Collections", priority = Priority + 3)]
        private static void ValidateSelectedAddressables()
        {
            if (!TryGetSelectedCollection(out var collection))
                return;

            ValidateAddressables(collection);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }
#endif

        public static void Sort(AudioCollection collection)
        {
            collection.musicClips = (collection.musicClips ?? Array.Empty<AudioClip>())
                .Where(x => x != null)
                .OrderBy(x => x.name)
                .ToArray();
            collection.sfxClips = (collection.sfxClips ?? Array.Empty<AudioClip>())
                .Where(x => x != null)
                .OrderBy(x => x.name)
                .ToArray();
        }

        public static void Generate(AudioCollection collection)
        {
            collection.musicClips ??= Array.Empty<AudioClip>();
            collection.sfxClips ??= Array.Empty<AudioClip>();

            if (!string.IsNullOrEmpty(collection.generator.inputMusicsFolder))
                AddClips(collection.generator.inputMusicsFolder, ref collection.musicClips);

            if (!string.IsNullOrEmpty(collection.generator.inputSfxsFolder))
                AddClips(collection.generator.inputSfxsFolder, ref collection.sfxClips);

            Sort(collection);

            string content = GenerateAudioIDsContent(collection);
            if (!string.IsNullOrEmpty(collection.generator.outputIDsFolder))
            {
                string exportPath = collection.generator.outputIDsFolder;
                if (!exportPath.StartsWith("Assets"))
                    exportPath = "Assets/" + exportPath;
                string fullPath = Application.dataPath + "/" + exportPath.Replace("Assets/", "") + "/AudioIDs.cs";
                File.WriteAllText(fullPath, content);
                AssetDatabase.Refresh();
                Debug.Log($"AudioIDs.cs generated at {fullPath}");
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }

#if ADDRESSABLES
        public static void ValidateAddressables(AudioCollection collection)
        {
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            collection.abSfxClips = new AssetReferenceT<AudioClip>[collection.sfxClips.Length];
            for (int i = 0; i < collection.sfxClips.Length; i++)
            {
                var clip = collection.sfxClips[i];
                if (clip == null) continue;
                string path = AssetDatabase.GetAssetPath(clip);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (addressableSettings.FindAssetEntry(guid) != null)
                {
                    collection.abSfxClips[i] = new AssetReferenceT<AudioClip>(guid);
                    collection.sfxClips[i] = null;
                }
            }

            collection.abMusicClips = new AssetReferenceT<AudioClip>[collection.musicClips.Length];
            for (int i = 0; i < collection.musicClips.Length; i++)
            {
                var clip = collection.musicClips[i];
                if (clip == null) continue;
                string path = AssetDatabase.GetAssetPath(clip);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (addressableSettings.FindAssetEntry(guid) != null)
                {
                    collection.abMusicClips[i] = new AssetReferenceT<AudioClip>(guid);
                    collection.musicClips[i] = null;
                }
            }
        }
#endif

        public static string GenerateAudioIDsContent(AudioCollection collection)
        {
            string musicStringKeys = GenerateStringKeys(collection.musicClips);
            string musicEnumKeys = GenerateIntKeys(collection.musicClips);
            string sfxStringKeys = GenerateStringKeys(collection.sfxClips);
            string sfxEnumKeys = GenerateIntKeys(collection.sfxClips);

            string body = $@"public static class MusicIDs
{{
    public static readonly string[] Names = new string[]
    {{
        {musicStringKeys}
    }};

    public enum Music
    {{
        {musicEnumKeys}
    }}
}}

public static class SfxIDs
{{
    public static readonly string[] Names = new string[]
    {{
        {sfxStringKeys}
    }};

    public enum Sfx
    {{
        {sfxEnumKeys}
    }}
}}";

            if (!string.IsNullOrEmpty(collection.generator.@namespace))
            {
                body = AddTabToEachLine(body);
                body = $"namespace {collection.generator.@namespace}\n{{\n{body}\n}}";
            }

            return body;
        }

        private static bool TryGetSelectedCollection(out AudioCollection collection)
        {
            collection = Selection.activeObject as AudioCollection;
            if (collection != null)
                return true;

            Debug.LogWarning("Select an AudioCollection asset first.");
            return false;
        }

        private static string GetSelectedFolder()
        {
            string path = "Assets";
            if (Selection.activeObject == null)
                return path;

            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(selectedPath))
                return selectedPath;

            string directory = Path.GetDirectoryName(selectedPath);
            return string.IsNullOrEmpty(directory) ? path : directory.Replace("\\", "/");
        }

        private static void AddClips(string folder, ref AudioClip[] clips)
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            foreach (string guid in guids)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null && !clips.Contains(clip))
                    ArrayUtility.Add(ref clips, clip);
            }
        }

        private static string SanitizeName(string name)
        {
            return Regex.Replace(name.Replace(" ", "_").Replace("-", "_"), "[^a-zA-Z0-9_]", "");
        }

        private static string GenerateIntKeys(AudioClip[] clips)
        {
            var validClips = (clips ?? Array.Empty<AudioClip>()).Where(c => c != null).ToArray();
            var parts = new string[validClips.Length];
            for (int i = 0; i < validClips.Length; i++)
                parts[i] = $"{SanitizeName(validClips[i].name)} = {i}";
            return string.Join(",\n        ", parts);
        }

        private static string GenerateStringKeys(AudioClip[] clips)
        {
            var validClips = (clips ?? Array.Empty<AudioClip>()).Where(c => c != null).ToArray();
            var parts = new string[validClips.Length];
            for (int i = 0; i < validClips.Length; i++)
                parts[i] = $"\"{SanitizeName(validClips[i].name)}\"";
            return string.Join(",\n        ", parts);
        }

        private static string AddTabToEachLine(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = "\t" + lines[i];
            return string.Join(Environment.NewLine, lines);
        }
    }
}
