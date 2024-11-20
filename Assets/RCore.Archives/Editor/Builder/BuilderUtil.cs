/**
 * Author HNB-RaBear - 2019
 **/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Builder
{
    //NOTE: used by cmd
    static void BuildCurrent()
    {
        var profile = new RCore.Editor.BuildProfile();
        profile.Reset();
        RCore.Editor.BuilderUtil.Build(profile);
    }
}

namespace RCore.Editor
{
    public enum CustomBuildTarget
    {
        NoTarget = BuildTarget.NoTarget,
        StandaloneWindows64 = BuildTarget.StandaloneWindows64,
        iOS = BuildTarget.iOS,
        Android = BuildTarget.Android,
        WebGL = BuildTarget.WebGL,
    }

    [Serializable]
    public class Directive
    {
        public bool enable;
        public string directive;
    }

    //==========================================================================================================

    public static class BuilderUtil
    {
        /// <summary>
        /// Build with given settings, call back if required
        /// </summary>
        /// <param name="outputFolder">Destination Folder</param>
        /// <param name="developmentBuild">Is Building Development</param>
        /// <param name="pCallBack">
        /// {1}: The Build Options
        /// {2}: 0 to 1, indicating how far the process is
        /// {3}: False for Pre-call, True for Post-call
        /// {4}: True for continue, False for abort
        /// </param>
        /// <returns></returns>
        public static bool Build(BuildProfile pProfile, Func<BuildPlayerOptions, float, bool, bool> callback = null)
        {
            BackUpSettings();

            var buildSteps = GetPlayerBuildOptions(pProfile);
            int i = 1;
            foreach (var opts in buildSteps)
            {
                if (callback != null && !callback(opts, i / (float)buildSteps.Count, false))
                    return false;

                BackUpTargetSettings(opts.target);
                var report = BuildPipeline.BuildPlayer(opts);
                if (report.summary.result == BuildResult.Succeeded)
                    Debug.Log($"Build succeeded: {report.summary.platform} {report.summary.totalTime} {report.summary.totalSize} bytes");

                if (report.summary.result == BuildResult.Failed)
                    Debug.Log("Build failed");
                RestoreTargetSettings(opts.target);

                ++i;
                if (callback != null && !callback(opts, i / (float)buildSteps.Count, true))
                {
                    RestoreSettings();
                    return false;
                }
            }
            RestoreSettings();
            return true;
        }

        private static BuildProfile mBackupProfile = new BuildProfile();
        private static BuildProfile mBackupTargetProfile = new BuildProfile();

        private static void BackUpTargetSettings(BuildTarget pTarget)
        {
            var targetGroup = GroupForTarget(pTarget);
            mBackupTargetProfile.bundleIdentifier = PlayerSettings.GetApplicationIdentifier(targetGroup);
            mBackupTargetProfile.scriptBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            string[] directives = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';');
            mBackupTargetProfile.directives = new List<Directive>();
            foreach (var d in directives)
                mBackupTargetProfile.directives.Add(new Directive
                {
                    enable = true,
                    directive = d
                });
        }

        private static void RestoreTargetSettings(BuildTarget pTarget)
        {
            var targetGroup = GroupForTarget(pTarget);
            PlayerSettings.SetApplicationIdentifier(targetGroup, mBackupTargetProfile.bundleIdentifier);
            PlayerSettings.SetScriptingBackend(targetGroup, mBackupTargetProfile.scriptBackend);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, mBackupTargetProfile.GetDirectivesString());
        }

        private static void BackUpSettings()
        {
            mBackupProfile.companyName = PlayerSettings.companyName;
            mBackupProfile.productName = PlayerSettings.productName;
            mBackupProfile.bundleIdentifier = PlayerSettings.applicationIdentifier;
            mBackupProfile.bundleVersion = PlayerSettings.bundleVersion;
#if UNITY_ANDROID
            mBackupProfile.bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
#endif
            mBackupProfile.arm64 = PlayerSettings.Android.targetArchitectures == (AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7);
            mBackupProfile.developmentBuild = EditorUserBuildSettings.development;
            mBackupProfile.autoConnectProfiler = EditorUserBuildSettings.connectProfiler;
            mBackupProfile.buildAppBundle = EditorUserBuildSettings.buildAppBundle;
            mBackupProfile.allowDebugging = EditorUserBuildSettings.allowDebugging;
            mBackupProfile.enableHeadlessMode = EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server;
        }

        private static void RestoreSettings()
        {
            PlayerSettings.companyName = mBackupProfile.companyName;
            PlayerSettings.productName = mBackupProfile.productName;
            PlayerSettings.applicationIdentifier = mBackupProfile.bundleIdentifier;
            PlayerSettings.bundleVersion = mBackupProfile.bundleVersion;
#if UNITY_ANDROID
            PlayerSettings.Android.bundleVersionCode = mBackupProfile.bundleVersionCode;
#endif
            if (mBackupProfile.arm64)
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            else
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            EditorUserBuildSettings.development = mBackupProfile.developmentBuild;
            EditorUserBuildSettings.connectProfiler = mBackupProfile.autoConnectProfiler;
            EditorUserBuildSettings.buildAppBundle = mBackupProfile.buildAppBundle;
            EditorUserBuildSettings.allowDebugging = mBackupProfile.allowDebugging;
            EditorUserBuildSettings.standaloneBuildSubtarget = mBackupProfile.enableHeadlessMode ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player;
        }

        public static List<BuildPlayerOptions> GetPlayerBuildOptions(BuildProfile pProfile)
        {
            var options = new List<BuildPlayerOptions>();
            foreach (var target in pProfile.targets)
            {
                options.Add(GetPlayerOptions(pProfile, (BuildTarget)target));
            }
            return options;
        }

        public static BuildPlayerOptions GetPlayerOptions(BuildProfile pProfile, BuildTarget ptarget)
        {
            var playerOptions = new BuildPlayerOptions();

            //var scenePaths = new List<string>();
            //for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            //{
            //    var scene = EditorBuildSettings.scenes[i];
            //    if (scene.enabled)
            //        scenePaths.Add(scene.path);
            //}
            //playerOptions.scenes = scenePaths.ToArray();
            var scenes = new List<string>();
            foreach (var s in pProfile.buildScenes)
                if (s.active)
                    scenes.Add(s.GetPath());
            playerOptions.scenes = scenes.ToArray();

            playerOptions.locationPathName = Path.Combine(pProfile.outputFolder + "/" + ptarget, pProfile.GetBuildName());
            if (ptarget == BuildTarget.StandaloneWindows || ptarget == BuildTarget.StandaloneWindows64)
                playerOptions.locationPathName += ".exe";
            else if (ptarget == BuildTarget.Android)
                playerOptions.locationPathName += ".apk";
            playerOptions.target = ptarget;

            var options = BuildOptions.None;
            if (pProfile.developmentBuild)
            {
                options |= BuildOptions.Development;
                if (pProfile.autoConnectProfiler)
                    options |= BuildOptions.ConnectWithProfiler;
                if (pProfile.allowDebugging)
                    options |= BuildOptions.AllowDebugging;
            }

            playerOptions.options = options;
            return playerOptions;
        }

        public static BuildTargetGroup GroupForTarget(BuildTarget pTarget)
        {
            switch (pTarget)
            {
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.WebGL:
                    return BuildTargetGroup.WebGL;
                case BuildTarget.WSAPlayer:
                    return BuildTargetGroup.WSA;
                case BuildTarget.PS4:
                    return BuildTargetGroup.PS4;
                case BuildTarget.XboxOne:
                    return BuildTargetGroup.XboxOne;
                case BuildTarget.tvOS:
                    return BuildTargetGroup.tvOS;
                case BuildTarget.Switch:
                    return BuildTargetGroup.Switch;
                default:
                    return BuildTargetGroup.Unknown;
            }
        }

        public static void OverwritePlayerBuildSettings(BuildProfile pProfile)
        {
            string directivesStr = pProfile.GetDirectivesString();
            var targets = pProfile.targets;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = (BuildTarget)targets[i];
                var targetGroup = GroupForTarget(target);
                PlayerSettings.SetApplicationIdentifier(targetGroup, pProfile.bundleIdentifier);
                PlayerSettings.SetScriptingBackend(targetGroup, pProfile.scriptBackend);
                if (pProfile.customDirectives)
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, directivesStr);
            }

            if (pProfile.customBuildName)
            {
                PlayerSettings.companyName = pProfile.companyName;
                PlayerSettings.productName = pProfile.productName;
            }
            if (pProfile.customPackage)
            {
                PlayerSettings.applicationIdentifier = pProfile.bundleIdentifier;
                PlayerSettings.bundleVersion = pProfile.bundleVersion;
#if UNITY_ANDROID
                PlayerSettings.Android.bundleVersionCode = pProfile.bundleVersionCode;
#elif UNITY_IOS
                
#endif
            }
            if (pProfile.arm64)
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            else
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            EditorUserBuildSettings.development = pProfile.developmentBuild;
            EditorUserBuildSettings.connectProfiler = pProfile.developmentBuild && pProfile.autoConnectProfiler;
            EditorUserBuildSettings.buildAppBundle = pProfile.buildAppBundle;
            EditorUserBuildSettings.allowDebugging = pProfile.developmentBuild && pProfile.allowDebugging;
            EditorUserBuildSettings.standaloneBuildSubtarget = pProfile.enableHeadlessMode ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player;
        }

        public static Texture2D FindIcon(BuildTargetGroup target, bool small = false)
        {
            string name;
            switch (target)
            {
                case BuildTargetGroup.iOS: name = "iPhone"; break;
                default: name = target.ToString(); break;
            }
            var path = $"BuildSettings.{name}{(small ? ".small" : "")}";
            return EditorGUIUtility.FindTexture(path);
        }

        private static bool HasDirective(this BuildProfile profile, string directive)
        {
            var directives = profile.directives;
            foreach (var d in directives)
                if (d.directive == directive)
                    return true;
            return false;
        }

        public static BuildProfile AddDirective(this BuildProfile profile, string directive)
        {
            if (!profile.HasDirective(directive))
            {
                profile.directives.Add(new Directive
                {
                    directive = directive,
                    enable = true
                });
            }
            return profile;
        }

        public static BuildProfile AddDirectives(this BuildProfile profile, string[] directives)
        {
            foreach (var directive in directives)
                if (!profile.HasDirective(directive))
                {
                    profile.directives.Add(new Directive
                    {
                        directive = directive,
                        enable = true
                    });
                }
            return profile;
        }

        public static string GetDirectivesString(this BuildProfile profile)
        {
            string directivesStr = "";
            for (int i = 0; i < profile.directives.Count; i++)
                if (profile.directives[i].enable)
                {
                    if (i < profile.directives.Count - 1)
                        directivesStr += profile.directives[i].directive + ";";
                    else
                        directivesStr += profile.directives[i].directive;
                }
            return directivesStr;
        }

        /// <summary>
        /// Build with default Setup
        /// Usage 
        /// # OSX
        /// cd ~/Documents/MyUnityProject
        /// Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -executeMethod Utilities.Editor.BuilderUtil.BuildByCommandLine
        /// # Windows
        /// <Unity.exe-Path> -quit -batchmode -projectPath <Project-Path> -executeMethod Utilities.Editor.BuilderUtil.BuildByCommandLine
        /// </summary>
        public static void BuildByCommandLine()
        {
            // We get all the args, including UNity.exe, -quit -batchmode etc
            // read everything after our execute call
            var args = Environment.GetCommandLineArgs();
            int profileIndex = 0;
            string outputFolder = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-profileIndex" && i < args.Length - 1)
                    int.TryParse(args[i + 1], out profileIndex);
                if (args[i] == "-outputFolder" && i < args.Length - 1)
                    outputFolder = args[i + 1];
            }
            var collection = BuildSettingsCollection.LoadOrCreateSettings();
            if (profileIndex > collection.profiles.Count || profileIndex < 0)
                return;

            var profile = collection.profiles[profileIndex];
            if (!string.IsNullOrEmpty(outputFolder))
            {
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                profile.outputFolder = outputFolder;
            }
            Build(profile);
        }

        public static void BuildFirstProfile()
        {
            var collection = BuildSettingsCollection.LoadOrCreateSettings();
            var profile = collection.profiles[0];
            Build(profile);
        }

        /*
        [MenuItem("RCore/Tools/Build Now")]
        static void BuildCurrent()
        {
            var profile = new BuildProfile();
            profile.Reset();
            Build(profile, null);
        }
        */
    }
}