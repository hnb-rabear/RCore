using System;
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
    [CustomEditor(typeof(AudioCollection))]
    public class AudioCollectionEditor : UnityEditor.Editor
    {
        private AudioCollection m_collection;

        private void OnEnable()
        {
            m_collection = target as AudioCollection;
            if (m_collection != null)
            {
                m_collection.musicClips ??= Array.Empty<AudioClip>();
                m_collection.sfxClips ??= Array.Empty<AudioClip>();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Audio IDs Generator", EditorStyles.boldLabel);

            m_collection.generator.inputMusicsFolder = EditorGUILayout.TextField("Musics Sources Path", m_collection.generator.inputMusicsFolder);
            m_collection.generator.inputSfxsFolder = EditorGUILayout.TextField("SFX Sources Path", m_collection.generator.inputSfxsFolder);
            m_collection.generator.outputIDsFolder = EditorGUILayout.TextField("Audio Ids Export Folder", m_collection.generator.outputIDsFolder);

            if (GUILayout.Button("Sort clips"))
            {
                m_collection.musicClips = (m_collection.musicClips ?? Array.Empty<AudioClip>())
                    .Where(x => x != null)
                    .OrderBy(x => x.name)
                    .ToArray();
                m_collection.sfxClips = (m_collection.sfxClips ?? Array.Empty<AudioClip>())
                    .Where(x => x != null)
                    .OrderBy(x => x.name)
                    .ToArray();
                Debug.Log("Audio clips sorted alphabetically.");
            }

            if (GUILayout.Button("Generate"))
            {
                m_collection.musicClips ??= Array.Empty<AudioClip>();
                m_collection.sfxClips ??= Array.Empty<AudioClip>();

                if (!string.IsNullOrEmpty(m_collection.generator.inputMusicsFolder))
                {
                    string musicPath = m_collection.generator.inputMusicsFolder;
                    var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { musicPath });
                    foreach (string guid in guids)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                        if (clip != null && !m_collection.musicClips.Contains(clip))
                            ArrayUtility.Add(ref m_collection.musicClips, clip);
                    }
                }

                if (!string.IsNullOrEmpty(m_collection.generator.inputSfxsFolder))
                {
                    string sfxPath = m_collection.generator.inputSfxsFolder;
                    var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { sfxPath });
                    foreach (string guid in guids)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                        if (clip != null && !m_collection.sfxClips.Contains(clip))
                            ArrayUtility.Add(ref m_collection.sfxClips, clip);
                    }
                }

                m_collection.musicClips = m_collection.musicClips.Where(c => c != null).ToArray();
                m_collection.sfxClips = m_collection.sfxClips.Where(c => c != null).ToArray();

                string content = GenerateAudioIDsContent();

                if (!string.IsNullOrEmpty(m_collection.generator.outputIDsFolder))
                {
                    string exportPath = m_collection.generator.outputIDsFolder;
                    if (!exportPath.StartsWith("Assets"))
                        exportPath = "Assets/" + exportPath;
                    string fullPath = Application.dataPath + "/" + exportPath.Replace("Assets/", "") + "/AudioIDs.cs";
                    System.IO.File.WriteAllText(fullPath, content);
                    AssetDatabase.Refresh();
                    Debug.Log($"AudioIDs.cs generated at {fullPath}");
                }
            }

#if ADDRESSABLES
            if (GUILayout.Button("Validate Addressable Sounds"))
            {
                var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                m_collection.abSfxClips = new AssetReferenceT<AudioClip>[m_collection.sfxClips.Length];
                for (int i = 0; i < m_collection.sfxClips.Length; i++)
                {
                    var clip = m_collection.sfxClips[i];
                    if (clip == null) continue;
                    string path = AssetDatabase.GetAssetPath(clip);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (addressableSettings.FindAssetEntry(guid) != null)
                    {
                        m_collection.abSfxClips[i] = new AssetReferenceT<AudioClip>(guid);
                        m_collection.sfxClips[i] = null;
                    }
                }
                m_collection.abMusicClips = new AssetReferenceT<AudioClip>[m_collection.musicClips.Length];
                for (int i = 0; i < m_collection.musicClips.Length; i++)
                {
                    var clip = m_collection.musicClips[i];
                    if (clip == null) continue;
                    string path = AssetDatabase.GetAssetPath(clip);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (addressableSettings.FindAssetEntry(guid) != null)
                    {
                        m_collection.abMusicClips[i] = new AssetReferenceT<AudioClip>(guid);
                        m_collection.musicClips[i] = null;
                    }
                }
                Debug.Log("Validated Addressable sounds. Direct references were converted.");
            }
#endif

            if (GUI.changed)
            {
                EditorUtility.SetDirty(m_collection);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndVertical();
        }

        private string GenerateAudioIDsContent()
        {
            string musicStringKeys = GenerateStringKeys(m_collection.musicClips);
            string musicEnumKeys = GenerateIntKeys(m_collection.musicClips);
            string sfxStringKeys = GenerateStringKeys(m_collection.sfxClips);
            string sfxEnumKeys = GenerateIntKeys(m_collection.sfxClips);

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

            if (!string.IsNullOrEmpty(m_collection.generator.@namespace))
            {
                body = AddTabToEachLine(body);
                body = $"namespace {m_collection.generator.@namespace}\n{{\n{body}\n}}";
            }

            return body;
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
