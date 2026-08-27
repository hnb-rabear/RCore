using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEditor.PackageManager;

namespace RCore.SheetX.Tests
{
	[TestFixture]
	public class SheetXUpdateCheckerTests
	{
		[TestCase("1.2.0", "1.1.0", ExpectedResult = 1)]
		[TestCase("1.1.0", "1.2.0", ExpectedResult = -1)]
		[TestCase("1.1.0", "1.1.0", ExpectedResult = 0)]
		[TestCase("2.0.0", "1.9.9", ExpectedResult = 1)]
		[TestCase("1.10.0", "1.9.0", ExpectedResult = 1)]
		[TestCase("1.0.1", "1.0.0", ExpectedResult = 1)]
		[TestCase("1.0.0", "1.0.0-rc.1", ExpectedResult = 1)]
		[TestCase("1.0.0-rc.2", "1.0.0-rc.10", ExpectedResult = -1)]
		[TestCase("1.0.0+build.2", "1.0.0+build.1", ExpectedResult = 0)]
		public int compare_versions_returns_correct_sign(string a, string b)
		{
			return SheetXUpdateChecker.CompareVersions(a, b);
		}

		[TestCase("1.0.0", "1.1.0", ExpectedResult = true)]
		[TestCase("1.1.0", "1.1.0", ExpectedResult = false)]
		[TestCase("1.2.0", "1.1.0", ExpectedResult = false)]
		[TestCase("1.0.0", null, ExpectedResult = false)]
		[TestCase(null, "1.1.0", ExpectedResult = false)]
		[TestCase(null, null, ExpectedResult = false)]
		[TestCase("", "1.1.0", ExpectedResult = false)]
		[TestCase("1.0.0", "", ExpectedResult = false)]
		public bool has_update_detects_newer_remote(string installed, string remote)
		{
			return SheetXUpdateChecker.HasUpdate(installed, remote);
		}

		[TestCase(PackageSource.Git, ExpectedResult = true)]
		[TestCase(PackageSource.Registry, ExpectedResult = true)]
		[TestCase(PackageSource.Embedded, ExpectedResult = false)]
		[TestCase(PackageSource.Local, ExpectedResult = false)]
		[TestCase(PackageSource.LocalTarball, ExpectedResult = false)]
		public bool can_update_accepts_only_supported_upm_sources(PackageSource source)
		{
			return SheetXUpdateChecker.CanUpdate(source);
		}
	}
}
