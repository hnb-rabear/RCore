/***
 * Author RaBear - HNB - 2019
 **/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RCore.Editor
{
    [Serializable]
    public class SceneAssetReference
    {
        public SceneAsset asset;
        public bool active;
        public SceneAssetReference(SceneAsset pSceneAsset, bool pActive)
        {
            asset = pSceneAsset;
            active = pActive;
        }
        public string GetPath()
        {
            return AssetDatabase.GetAssetPath(asset);
        }
    }

    [Serializable]
    public class BuildProfile
    {
        public static string NAME_BUILD_PATTERN = "#ProductName#Version#BundleCode#Time";
        /// <summary>
        /// Name of build
        /// </summary>
        public string buildName;
        public string note;
        /// <summary>
        /// Automatic generate name for build
        /// </summary>
        public bool autoNameBuild;
        /// <summary>
        /// Folder to place Final Result
        /// </summary>
        public string outputFolder;
        /// <summary>
        /// Should use development build
        /// </summary>
        public bool developmentBuild;
        public bool autoConnectProfiler;
        public bool allowDebugging;
        public string bundleIdentifier;
        public ScriptingImplementation scriptBackend;
        public bool arm64;
        public bool buildAppBundle;
        public List<CustomBuildTarget> targets;
        public string companyName;
        public string productName;
        public string bundleVersion;
        public int bundleVersionCode;
        public string buildNumber;
        public bool enableHeadlessMode;
        /// <summary>
        /// Suffix which automatic add to end of build name
        /// </summary>
        public string suffix;
        public List<SceneAssetReference> buildScenes = new List<SceneAssetReference>();
        public ReorderableList reorderBuildScenes;
        public bool customDirectives;
        public List<Directive> directives = new List<Directive>();
        public ReorderableList reorderDirectives;
        public bool selected;
        public bool customBuildName;
        public bool customPackage;

        public BuildProfile() { }

        public BuildProfile(BuildProfile other)
        {
            buildName = other.buildName;
            note = other.note;
            autoNameBuild = other.autoNameBuild;
            outputFolder = other.outputFolder;
            developmentBuild = other.developmentBuild;
            autoConnectProfiler = other.autoConnectProfiler;
            allowDebugging = other.allowDebugging;
            bundleIdentifier = other.bundleIdentifier;
            scriptBackend = other.scriptBackend;
            arm64 = other.arm64;
            buildAppBundle = other.buildAppBundle;
            targets = other.targets;
            companyName = other.companyName;
            productName = other.productName;
            bundleVersion = other.bundleVersion;
            bundleVersionCode = other.bundleVersionCode;
            buildNumber = other.buildNumber;
            suffix = other.suffix;
            directives = other.directives;
            buildScenes = other.buildScenes;
            reorderBuildScenes = other.reorderBuildScenes;
            customDirectives = other.customDirectives;
            selected = other.selected;
            customBuildName = other.customBuildName;
            customPackage = other.customPackage;
        }

        public void Reset()
        {
            var currentTarget = EditorUserBuildSettings.activeBuildTarget;
            outputFolder = Directory.GetParent(Application.dataPath).FullName;
            targets = new List<CustomBuildTarget>();
            targets.Add((CustomBuildTarget)currentTarget);
            developmentBuild = false;
            buildName = Application.productName;
            bundleIdentifier = Application.identifier;
            companyName = PlayerSettings.companyName;
            productName = PlayerSettings.productName;
            bundleVersion = PlayerSettings.bundleVersion;
            bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            buildNumber = PlayerSettings.iOS.buildNumber;
            scriptBackend = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup);
            autoNameBuild = true;
            autoConnectProfiler = true;
            arm64 = (PlayerSettings.Android.targetArchitectures | AndroidArchitecture.ARM64) == PlayerSettings.Android.targetArchitectures;
            buildAppBundle = EditorUserBuildSettings.buildAppBundle;
            var curDirectives = EditorHelper.GetDirectives();
            directives = new List<Directive>();
            foreach (var d in curDirectives)
                directives.Add(new Directive()
                {
                    directive = d,
                    enable = true
                });
            customDirectives = false;
        }

        public string GetBuildName()
        {
            if (autoNameBuild)
            {
                string bundleVersion = this.bundleVersion;
                int bundleVersionCode = this.bundleVersionCode;
                string buildNumber = this.buildNumber;
                string identifer = bundleIdentifier;
                if (!customPackage)
                {
                    bundleVersion = PlayerSettings.bundleVersion;
                    bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
                    buildNumber = PlayerSettings.iOS.buildNumber;
                    identifer = Application.identifier;
                }

                var strs = identifer.Split('.');
                string name = string.IsNullOrEmpty(identifer) ? "" : $"{strs[strs.Length - 1]}_";
                string version = string.IsNullOrEmpty(bundleVersion) ? "" : $"v{bundleVersion}_";
                string bundleCode = "";
#if UNITY_ANDROID
                bundleCode = bundleVersionCode == 0 ? "" : $"b{bundleVersionCode}_";
#elif UNITY_IOS
                bundleCode = $"b{buildNumber}_";
#endif
                var time = DateTime.Now;
                string file = NAME_BUILD_PATTERN.Replace("#ProductName", name)
                    .Replace("#Version", version)
                    .Replace("#BundleCode", bundleCode)
                    .Replace("#Time", $"{time.Year % 100}{time.Month:00}{time.Day:00}_{time.Hour:00}h{time.Minute:00}");
                file = file.Replace(" ", "_").Replace("/", "-").Replace(":", "-");
                file += developmentBuild ? "_dev" : "";
                file += suffix;
                return file;
            }
            return buildName + suffix;
        }

        public bool ContainScene(string pPath)
        {
            for (int i = 0; i < buildScenes.Count; i++)
            {
                var path = AssetDatabase.GetAssetPath(buildScenes[i].asset);
                if (path == pPath)
                    return true;
            }
            return false;
        }

        public void RemoveScene(string pPath)
        {
            for (int i = buildScenes.Count - 1; i >= 0; i--)
            {
                var path = AssetDatabase.GetAssetPath(buildScenes[i].asset);
                if (path == pPath)
                {
                    buildScenes.RemoveAt(i);
                    break;
                }
            }
        }

        public void AddScene(string pPath, bool pActive)
        {
            for (int i = buildScenes.Count - 1; i >= 0; i--)
            {
                var path = AssetDatabase.GetAssetPath(buildScenes[i].asset);
                if (path == pPath)
                {
                    buildScenes[i].active = pActive;
                    return;
                }
            }
            var objScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(pPath);
            buildScenes.Add(new SceneAssetReference(objScene, pActive));
        }
    }
}