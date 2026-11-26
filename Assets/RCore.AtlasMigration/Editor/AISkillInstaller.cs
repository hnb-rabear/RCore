using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace RCore.Editor.Tool
{
	/// <summary>Copies the AI agent skills shipped with this package into the project's .claude/skills folder.</summary>
	public static class AISkillInstaller
	{
		private const string MENU = "RCore/Tools/Install AI Skills";

		[MenuItem(MENU, priority = 120)]
		private static void InstallMenu()
		{
			int count = Install(Path.GetDirectoryName(Application.dataPath));
			EditorUtility.DisplayDialog("Install AI Skills", $"Copied {count} skill(s) to .claude/skills/. Commit them so your team gets them.", "OK");
		}

		/// <summary>Copies every shipped SKILL.md to <paramref name="pProjectRoot"/>/.claude/skills/&lt;name&gt;/SKILL.md. Returns the number copied.</summary>
		internal static int Install(string pProjectRoot)
		{
			// Git packages live in Library/PackageCache, not at their "Packages/..." virtual path; resolvedPath is the real folder.
			// In the RCore repo the package sits under Assets/, where it is not a package, so fall back to the asmdef folder.
			var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AISkillInstaller).Assembly);
			string editorFolder = package != null
				? Path.Combine(package.resolvedPath, "Editor")
				: Path.GetDirectoryName(Path.GetFullPath(CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("RCore.AtlasMigration.Editor")));
			string source = Path.Combine(editorFolder, "AI");
			string target = Path.Combine(pProjectRoot, ".claude", "skills");
			int count = 0;
			foreach (string skill in Directory.GetFiles(source, "SKILL.md", SearchOption.AllDirectories))
			{
				string name = Path.GetFileName(Path.GetDirectoryName(skill));
				string dest = Path.Combine(target, name, "SKILL.md");
				Directory.CreateDirectory(Path.GetDirectoryName(dest));
				File.Copy(skill, dest, true);
				Debug.Log($"[AtlasMigration] Installed AI skill {name} -> {dest}");
				count++;
			}
			return count;
		}
	}
}
