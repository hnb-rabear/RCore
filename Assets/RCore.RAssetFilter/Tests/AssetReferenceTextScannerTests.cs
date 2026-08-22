using System;
using System.IO;
using NUnit.Framework;
using RCore.RAssetFilter.Editor;

namespace RCore.RAssetFilter.Tests
{
	public class AssetReferenceTextScannerTests
	{
		private string m_fixtureRoot;

		[SetUp]
		public void SetUp()
		{
			m_fixtureRoot = Path.Combine(Path.GetTempPath(), "RAssetFilterScannerTests_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(m_fixtureRoot);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(m_fixtureRoot))
				Directory.Delete(m_fixtureRoot, true);
		}

		[Test]
		public void scan_all_object_references_matches_exact_and_guid_only_targets()
		{
			const string guid = "0123456789abcdef0123456789abcdef";
			WriteFixture("exact.prefab", "m_Reference: {fileID: 123, guid: " + guid + ", type: 3}");
			WriteFixture("different-id.prefab", "m_Reference: {fileID: 456, guid: " + guid + ", type: 3}");
			WriteFixture("other.prefab", "m_Reference: {fileID: 123, guid: fedcba9876543210fedcba9876543210, type: 3}");

			var result = AssetReferenceTextScanner.ScanAllObjectReferences(
				new[] { "other.prefab", "different-id.prefab", "exact.prefab" },
				new[]
				{
					new AssetReferenceTextScanner.ObjectReferenceTarget("exact", guid, 123, true),
					new AssetReferenceTextScanner.ObjectReferenceTarget("fallback", guid, 0, false),
				},
				1,
				null,
				m_fixtureRoot);

			CollectionAssert.AreEqual(new[] { "exact.prefab" }, result.pathsByTargetId["exact"]);
			CollectionAssert.AreEqual(new[] { "different-id.prefab", "exact.prefab" }, result.pathsByTargetId["fallback"]);
			Assert.That(result.skippedPaths, Is.Empty);
		}

		[Test]
		public void scan_all_object_references_reports_unreadable_paths_and_keeps_valid_matches()
		{
			const string guid = "0123456789abcdef0123456789abcdef";
			WriteFixture("valid.prefab", "m_Reference: {fileID: 123, guid: " + guid + ", type: 3}");

			var result = AssetReferenceTextScanner.ScanAllObjectReferences(
				new[] { "missing.prefab", "valid.prefab" },
				new[] { new AssetReferenceTextScanner.ObjectReferenceTarget("target", guid, 123, true) },
				1,
				null,
				m_fixtureRoot);

			CollectionAssert.AreEqual(new[] { "valid.prefab" }, result.pathsByTargetId["target"]);
			Assert.That(result.skippedPaths, Has.Count.EqualTo(1));
			StringAssert.StartsWith("missing.prefab (", result.skippedPaths[0]);
		}

		private void WriteFixture(string pRelativePath, string pContent)
		{
			File.WriteAllText(Path.Combine(m_fixtureRoot, pRelativePath), pContent);
		}
	}
}
