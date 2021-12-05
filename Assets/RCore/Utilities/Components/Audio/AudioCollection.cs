using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
    /// <summary>
    /// Simple audio collection
    /// Simple enough for small game which does not require many sounds
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCollection", menuName = "RUtilities/Audio Collection")]
    public class AudioCollection : ScriptableObject
    {
        private static AudioCollection mInstance;
        public static AudioCollection Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = Resources.Load<AudioCollection>("AudioCollection");
                return mInstance;
            }
        }

        public string musicsPath;
        public string sfxsPath;
        public string configPath;
        public AudioClip[] sfxClips;
        public AudioClip[] musicClips;

        public AudioClip GetMusicClip(int pKey)
        {
            if (pKey < musicClips.Length)
                return musicClips[pKey];
            return null;
        }

        public AudioClip GetMusicClip(string pKey)
        {
            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i].name == pKey)
                    return musicClips[i];
            }
            return null;
        }

        public AudioClip GetMusicClip(string pKey, ref int pIndex)
        {
            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i].name == pKey)
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
                if (sfxClips[i].name == pName)
                    return sfxClips[i];
            }
            return null;
        }

        public AudioClip GetSFXClip(string pKey, ref int pIndex)
        {
            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i].name == pKey)
                {
                    pIndex = i;
                    return sfxClips[i];
                }
            }
            return null;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AudioCollection))]
        private class AudioCollectionEditor : Editor
        {
            private AudioCollection mScript;

            private void OnEnable()
            {
                mScript = target as AudioCollection;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorHelper.BoxVertical(() =>
                {
                    string musicsSourcePath = mScript.musicsPath;
                    string sfxsSourcePath = mScript.sfxsPath;
                    string exportConfigPath = mScript.configPath;
                    musicsSourcePath = EditorHelper.FolderSelector("Musics Sources Path", "musicsSourcePath", Application.dataPath, true);
                    sfxsSourcePath = EditorHelper.FolderSelector("SFX Sources Path", "sfxsSourcePath", Application.dataPath, true);
                    exportConfigPath = EditorHelper.FolderSelector("Export Config Path", "exportConfigPath", Application.dataPath, true);

                    if (EditorHelper.Button("Build"))
                    {
                        musicsSourcePath = Application.dataPath + musicsSourcePath;
                        sfxsSourcePath = Application.dataPath + sfxsSourcePath;
                        exportConfigPath = Application.dataPath + exportConfigPath;

                        var musicFiles = EditorHelper.GetObjects<AudioClip>(musicsSourcePath, "t:AudioClip");
                        var sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxsSourcePath, "t:AudioClip");
                        string result = GetConfigTemplate();

                        //Musics
                        mScript.musicClips = new AudioClip[musicFiles.Count];
                        string stringKeys = "";
                        string intKeys = "";
                        string enumKeys = "";
                        for (int i = 0; i < musicFiles.Count; i++)
                        {
                            mScript.musicClips[i] = musicFiles[i];

                            stringKeys += $"\"{musicFiles[i].name}\"";
                            intKeys += $"{musicFiles[i].name.ToUpper()} = {i}";
                            enumKeys += $"{musicFiles[i].name} = {i}";
                            if (i < musicFiles.Count - 1)
                            {
                                stringKeys += $",{Environment.NewLine}\t\t";
                                intKeys += $",{Environment.NewLine}\t\t";
                                enumKeys += $",{Environment.NewLine}\t\t";
                            }
                        }
                        result = result.Replace("<M_CONSTANT_INT_KEYS>", intKeys).Replace("<M_CONSTANT_STRING_KEYS>", stringKeys).Replace("<M_CONSTANTS_ENUM_KEYS>", enumKeys);

                        //SFXs
                        mScript.sfxClips = new AudioClip[sfxFiles.Count];
                        stringKeys = "";
                        intKeys = "";
                        enumKeys = "";
                        for (int i = 0; i < sfxFiles.Count; i++)
                        {
                            mScript.sfxClips[i] = sfxFiles[i];

                            stringKeys += $"\"{sfxFiles[i].name}\"";
                            intKeys += $"{sfxFiles[i].name.ToUpper()} = {i}";
                            enumKeys += $"{sfxFiles[i].name} = {i}";
                            if (i < sfxFiles.Count - 1)
                            {
                                stringKeys += $",{Environment.NewLine}\t\t";
                                intKeys += $",{Environment.NewLine}\t\t";
                                enumKeys += $",{Environment.NewLine}\t\t";
                            }
                        }
                        result = result.Replace("<S_CONSTANT_INT_KEYS>", intKeys).Replace("<S_CONSTANT_STRING_KEYS>", stringKeys).Replace("<S_CONSTANTS_ENUM_KEYS>", enumKeys);

                        //Write result
                        System.IO.File.WriteAllText(exportConfigPath + "/AudioIDs.cs", result);

                        if (GUI.changed)
                        {
                            mScript.musicsPath = musicsSourcePath.Replace(Application.dataPath, "");
                            mScript.sfxsPath = sfxsSourcePath.Replace(Application.dataPath, "");
                            mScript.configPath = exportConfigPath.Replace(Application.dataPath, "");
                            EditorUtility.SetDirty(mScript);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                }, ColorHelper.LightAzure, true);
            }

            private string GetConfigTemplate()
            {
                string musicTemplate =
                    "public class MusicIDs\n"
                    + "{\n"
                    + "\tpublic const int <M_CONSTANT_INT_KEYS>;\n"
                    + "\tpublic static readonly string[] idStrings = new string[] { <M_CONSTANT_STRING_KEYS> };\n"
                    + "\tpublic enum Music { <M_CONSTANTS_ENUM_KEYS> };\n"
                    + "}";
                string sfxTemplate =
                    "public class SfxIDs\n"
                    + "{\n"
                    + "\tpublic const int <S_CONSTANT_INT_KEYS>;\n"
                    + "\tpublic static readonly string[] idStrings = new string[] { <S_CONSTANT_STRING_KEYS> };\n"
                    + "\tpublic enum Sfx { <S_CONSTANTS_ENUM_KEYS> };\n"
                    + "}";
                return musicTemplate + "\n" + sfxTemplate;
            }
        }
#endif
    }
}