using System.IO;
using NUnit.Framework;
using RCore.Editor.Tool;

namespace RCore.Editor.Tests
{
	public class AISkillInstallerTests
	{
		private const string SKILL = "unity-texturepacker-atlas-migration";

		[Test]
		public void install_copies_shipped_skill_into_project_claude_skills()
		{
			string temp = Path.Combine(Path.GetTempPath(), "rcore-ai-skill-" + Path.GetRandomFileName());
			Directory.CreateDirectory(temp);
			try
			{
				int count = AISkillInstaller.Install(temp);

				Assert.GreaterOrEqual(count, 1);
				string installed = Path.Combine(temp, ".claude", "skills", SKILL, "SKILL.md");
				Assert.IsTrue(File.Exists(installed));
				string shipped = Path.Combine("Assets", "RCore.AtlasMigration", "Editor", "AI", SKILL, "SKILL.md");
				Assert.AreEqual(File.ReadAllText(shipped), File.ReadAllText(installed));
			}
			finally
			{
				Directory.Delete(temp, true);
			}
		}
	}
}
