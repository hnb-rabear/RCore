using System.Collections.Generic;
using NUnit.Framework;

namespace RCore.Editor.Tests
{
	public class ReplaceInTextTests
	{
		private const string GUID_A = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
		private const string GUID_B = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

		private static string Ref(long pFileId, string pGuid) => $"fileID: {pFileId}, guid: {pGuid}";

		[Test]
		public void ReplacesMatchingPairAndCounts()
		{
			var content = $"m_Sprite: {{{Ref(21300000, GUID_A)}, type: 3}}\nm_Icon: {{{Ref(21300000, GUID_A)}, type: 3}}";
			var refs = new Dictionary<string, string> { [Ref(21300000, GUID_A)] = Ref(21300000, GUID_B) };

			int count = EditorAssetUtil.ReplaceInText(ref content, refs);

			Assert.AreEqual(2, count);
			Assert.IsFalse(content.Contains(GUID_A));
		}

		[Test]
		public void LeavesOtherFileIdWithSameGuid()
		{
			var content = $"a: {{{Ref(111, GUID_A)}}}\nb: {{{Ref(222, GUID_A)}}}";
			var refs = new Dictionary<string, string> { [Ref(111, GUID_A)] = Ref(111, GUID_B) };

			Assert.AreEqual(1, EditorAssetUtil.ReplaceInText(ref content, refs));
			StringAssert.Contains(Ref(222, GUID_A), content);
		}

		[Test]
		public void SwappedMappingsDoNotChain()
		{
			var content = $"a: {{{Ref(-1, GUID_A)}}}\nb: {{{Ref(-1, GUID_B)}}}";
			var refs = new Dictionary<string, string>
			{
				[Ref(-1, GUID_A)] = Ref(-1, GUID_B),
				[Ref(-1, GUID_B)] = Ref(-1, GUID_A),
			};

			Assert.AreEqual(2, EditorAssetUtil.ReplaceInText(ref content, refs));
			Assert.AreEqual($"a: {{{Ref(-1, GUID_B)}}}\nb: {{{Ref(-1, GUID_A)}}}", content);
		}
	}
}
