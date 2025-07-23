#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace RCore.Editor
{
    /// <summary>
    /// Provides utility methods for interacting with build settings,
    /// such as managing scripting define symbols and retrieving build information.
    /// </summary>
    public static class EditorBuildUtil
    {
        /// <summary>
        /// Gets all scripting define symbols for a build target group.
        /// </summary>
        public static string[] GetDirectives(BuildTargetGroup pTarget)
        {
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string defineStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        string[] currentDefines = defineStr.Split(';');
	        for (int i = 0; i < currentDefines.Length; i++)
		        currentDefines[i] = currentDefines[i].Trim();
	        return currentDefines;
        }
        
        public static void AddDirectives(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        string[] directives = directivesStr.Split(';');
	        foreach (var s in pSymbols)
	        {
		        bool existed = false;
		        foreach (var d in directives)
			        if (d == s)
			        {
				        existed = true;
				        break;
			        }

		        if (existed)
			        continue;

		        if (string.IsNullOrEmpty(directivesStr))
			        directivesStr += s;
		        else
			        directivesStr += $";{s}";
	        }

	        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directivesStr);
        }
        
        public static void AddDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        string[] directives = directivesStr.Split(';');
	        foreach (var d in directives)
		        if (d == pSymbol)
			        return;

	        if (string.IsNullOrEmpty(directivesStr))
		        directivesStr += pSymbol;
	        else
		        directivesStr += $";{pSymbol}";
	        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directivesStr);
        }
        
        /// <summary>
        /// Removes a scripting define symbol.
        /// </summary>
        public static void RemoveDirective(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string directives = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        foreach (var s in pSymbols)
	        {
		        if (directives.Contains($"{s};"))
			        directives = directives.Replace($"{s};", "");
		        else if (directives.Contains(s))
			        directives = directives.Replace(s, "");
	        }

	        if (directives.Length > 1 && directives[directives.Length - 1] == ';')
		        directives = directives.Remove(directives.Length - 1, 1);
	        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directives);
        }
        
        public static void RemoveDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        if (string.IsNullOrEmpty(pSymbol))
		        return;
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string directives = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        directives = directives.Replace(pSymbol, "");
	        if (directives.Length > 1 && directives[directives.Length - 1] == ';')
		        directives = directives.Remove(directives.Length - 1, 1);
	        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directives);
        }

        /// <summary>
        /// Gets the file names of all scenes included in the Build Settings.
        /// </summary>
        public static string[] GetSceneNamesInBuild()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToArray();
        }
        
        public static string[] GetScenePaths()
        {
	        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
	        string[] scenes = new string[sceneCount];
	        for (int i = 0; i < sceneCount; i++)
	        {
		        scenes[i] = Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility
			        .GetScenePathByBuildIndex(i));
	        }

	        return scenes;
        }
        
        public static bool ContainDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        var directives = GetDirectives(pTarget);
	        foreach (var d in directives)
		        if (d == pSymbol)
			        return true;
	        return false;
        }
        
        public static string GetBuildName()
        {
	        bool developmentBuild = EditorUserBuildSettings.development;
	        string bundleVersion = PlayerSettings.bundleVersion;
	        string appName = PlayerSettings.productName.Replace(" ", "").RemoveSpecialCharacters();

	        string name = $"{appName}_";
	        string version = string.IsNullOrEmpty(bundleVersion) ? "" : $"v{bundleVersion}_";
	        string bundleCode = "";
#if UNITY_ANDROID
	        int bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
	        bundleCode = bundleVersionCode == 0 ? "" : $"b{bundleVersionCode}_";
#endif
	        const string NAME_BUILD_PATTERN = "#ProductName#Version#BundleCode#Time";
	        var time = DateTime.Now;
	        string file = NAME_BUILD_PATTERN.Replace("#ProductName", name)
		        .Replace("#Version", version)
		        .Replace("#BundleCode", bundleCode)
		        .Replace("#Time", $"{time.Year % 100}{time.Month:00}{time.Day:00}_{time.Hour:00}h{time.Minute:00}");
	        file = file.Replace(" ", "_").Replace("/", "-").Replace(":", "-");
	        file += developmentBuild ? "_dev" : "";
	        return file;
        }
    }
}
#endif