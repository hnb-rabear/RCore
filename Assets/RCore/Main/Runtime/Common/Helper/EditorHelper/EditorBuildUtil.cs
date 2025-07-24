#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace RCore.Editor
{
    /// <summary>
    /// Provides static utility methods for interacting with build settings and processes within the Unity Editor.
    /// This includes managing scripting define symbols and retrieving build information.
    /// </summary>
    public static class EditorBuildUtil
    {
        /// <summary>
        /// Gets all scripting define symbols for a specified build target group.
        /// </summary>
        /// <param name="pTarget">The target build group. If Unknown, the currently active build target group is used.</param>
        /// <returns>An array of strings, where each string is a defined symbol.</returns>
        public static string[] GetDirectives(BuildTargetGroup pTarget)
        {
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string defineStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        // Split the semicolon-delimited string and trim whitespace from each symbol.
	        string[] currentDefines = defineStr.Split(';');
	        for (int i = 0; i < currentDefines.Length; i++)
		        currentDefines[i] = currentDefines[i].Trim();
	        return currentDefines;
        }
        
        /// <summary>
        /// Adds a list of scripting define symbols to a build target group if they are not already present.
        /// </summary>
        /// <param name="pSymbols">The list of symbols to add.</param>
        /// <param name="pTarget">The target build group. If Unknown, the currently active build target group is used.</param>
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
        
        /// <summary>
        /// Adds a single scripting define symbol to a build target group if it is not already present.
        /// </summary>
        /// <param name="pSymbol">The symbol to add.</param>
        /// <param name="pTarget">The target build group. If Unknown, the currently active build target group is used.</param>
        public static void AddDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        string[] directives = directivesStr.Split(';');
	        // Check if the symbol already exists.
	        foreach (var d in directives)
		        if (d == pSymbol)
			        return;
			
			// Append the new symbol.
	        if (string.IsNullOrEmpty(directivesStr))
		        directivesStr += pSymbol;
	        else
		        directivesStr += $";{pSymbol}";
	        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directivesStr);
        }
        
        /// <summary>
        /// Removes a list of scripting define symbols from a build target group.
        /// </summary>
        /// <param name="pSymbols">The list of symbols to remove.</param>
        /// <param name="pTarget">The target build group. If Unknown, the currently active build target group is used.</param>
        public static void RemoveDirective(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string directives = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        foreach (var s in pSymbols)
	        {
		        // Handle cases where the symbol is followed by a semicolon or is the last one.
		        if (directives.Contains($"{s};"))
			        directives = directives.Replace($"{s};", "");
		        else if (directives.Contains(s))
			        directives = directives.Replace(s, "");
	        }
			
			// Clean up trailing semicolons.
	        if (directives.Length > 1 && directives[directives.Length - 1] == ';')
		        directives = directives.Remove(directives.Length - 1, 1);
	        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directives);
        }
        
        /// <summary>
        /// Removes a single scripting define symbol from a build target group.
        /// </summary>
        /// <param name="pSymbol">The symbol to remove.</param>
        /// <param name="pTarget">The target build group. If Unknown, the currently active build target group is used.</param>
        public static void RemoveDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        if (string.IsNullOrEmpty(pSymbol))
		        return;
	        var target = pTarget == BuildTargetGroup.Unknown
		        ? EditorUserBuildSettings.selectedBuildTargetGroup
		        : pTarget;
	        string directives = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
	        directives = directives.Replace(pSymbol, "");
	        // Clean up trailing semicolons.
	        if (directives.Length > 1 && directives[directives.Length - 1] == ';')
		        directives = directives.Remove(directives.Length - 1, 1);
	        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directives);
        }

        /// <summary>
        /// Gets the file names of all scenes that are enabled in the Build Settings.
        /// </summary>
        /// <returns>An array of scene names without the file extension.</returns>
        public static string[] GetSceneNamesInBuild()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToArray();
        }
        
        /// <summary>
        /// Gets the file names of all scenes listed in the Build Settings, regardless of whether they are enabled.
        /// </summary>
        /// <returns>An array of all scene names in the build settings.</returns>
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
        
        /// <summary>
        /// Checks if a specific scripting define symbol is present for a build target group.
        /// </summary>
        /// <param name="pSymbol">The symbol to check for.</param>
        /// <param name="pTarget">The target build group. If Unknown, the currently active build target group is used.</param>
        /// <returns>True if the symbol is defined, otherwise false.</returns>
        public static bool ContainDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
	        var directives = GetDirectives(pTarget);
	        foreach (var d in directives)
		        if (d == pSymbol)
			        return true;
	        return false;
        }
        
        /// <summary>
        /// Generates a standardized build file name based on the current project settings and time.
        /// </summary>
        /// <returns>A formatted string suitable for a build file name (e.g., "MyGame_v1.2_b10_240726_14h30_dev").</returns>
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
	        // A template for the build name structure.
	        const string NAME_BUILD_PATTERN = "#ProductName#Version#BundleCode#Time";
	        var time = DateTime.Now;
	        // Replace placeholders with actual values.
	        string file = NAME_BUILD_PATTERN.Replace("#ProductName", name)
		        .Replace("#Version", version)
		        .Replace("#BundleCode", bundleCode)
		        .Replace("#Time", $"{time.Year % 100}{time.Month:00}{time.Day:00}_{time.Hour:00}h{time.Minute:00}");
	        // Sanitize the file name.
	        file = file.Replace(" ", "_").Replace("/", "-").Replace(":", "-");
	        // Append a suffix for development builds.
	        file += developmentBuild ? "_dev" : "";
	        return file;
        }
    }
}
#endif