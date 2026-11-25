#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Defines SPINE_ANIMATION for build target groups that have Spine installed, so RCore's Spine-only
	/// assemblies compile without anyone editing Scripting Define Symbols by hand.
	///
	/// This lives in the unconstrained editor assembly on purpose: the Spine assembly it enables is gated
	/// behind the very symbol set here, so it could never define it itself. Detection is by type lookup
	/// rather than a package query because Spine usually ships as plain files under Assets/, not as UPM.
	/// </summary>
	[InitializeOnLoad]
	public static class SpineDefineSymbolSetup
	{
		private const string SYMBOL = "SPINE_ANIMATION";

		static SpineDefineSymbolSetup()
		{
			// The check itself is cheap, but it must not run while Unity is mid-compile or the resulting
			// PlayerSettings write would trigger a second reload on top of the one in flight.
			EditorApplication.delayCall += Apply;
		}

		private static void Apply()
		{
			if (EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				EditorApplication.delayCall += Apply;
				return;
			}

			bool spinePresent = IsSpineInstalled();
			var group = EditorUserBuildSettings.selectedBuildTargetGroup;
			if (group == BuildTargetGroup.Unknown) return;

			string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
			var list = symbols.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
			bool hasSymbol = list.Contains(SYMBOL);

			if (spinePresent == hasSymbol) return;

			if (spinePresent) list.Add(SYMBOL);
			else list.Remove(SYMBOL); // Spine was removed; leaving the symbol would break compilation.

			PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
			Debug.Log($"[RCore] {(spinePresent ? "Added" : "Removed")} {SYMBOL} for {group}.");
		}

		private static bool IsSpineInstalled()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				// Runtime type, not an editor one: it is what the gated assemblies actually compile against.
				if (assembly.GetType("Spine.Unity.SkeletonGraphic", false) != null) return true;
			}
			return false;
		}
	}
}
#endif
