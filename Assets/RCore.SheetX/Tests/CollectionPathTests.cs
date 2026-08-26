using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class CollectionPathTests
	{
		[Test]
		public void normalize_rewrites_separators_and_trims_trailing_slash()
		{
			Assert.That(SheetXCollectionSettings.NormalizePath(@"Assets\Game\Editor\Data\"), Is.EqualTo("Assets/Game/Editor/Data"));
		}

		[Test]
		public void project_path_requires_assets_root()
		{
			Assert.That(SheetXCollectionSettings.IsProjectPath("Assets/Game/Data"), Is.True);
			Assert.That(SheetXCollectionSettings.IsProjectPath("Packages/Game/Data"), Is.False);
			Assert.That(SheetXCollectionSettings.IsProjectPath("C:/Projects/Assets/Data"), Is.False);
		}

		[Test]
		public void build_included_segment_matches_special_folder_names_only()
		{
			Assert.That(SheetXCollectionSettings.HasBuildIncludedSegment("Assets/Game/Resources/Data"), Is.True);
			Assert.That(SheetXCollectionSettings.HasBuildIncludedSegment("Assets/Game/StreamingAssets/Data"), Is.True);
			Assert.That(SheetXCollectionSettings.HasBuildIncludedSegment("Assets/Game/MyResources/Data"), Is.False);
			Assert.That(SheetXCollectionSettings.HasBuildIncludedSegment("Assets/Game/StreamingAssetsTools/Data"), Is.False);
			Assert.That(SheetXCollectionSettings.HasBuildIncludedSegment("Assets/Game/Editor/Data"), Is.False);
		}

		[Test]
		public void resources_check_requires_final_segment()
		{
			Assert.That(SheetXCollectionSettings.EndsWithResources("Assets/Game/Resources"), Is.True);
			Assert.That(SheetXCollectionSettings.EndsWithResources(@"Assets\Game\Resources\"), Is.True);
			Assert.That(SheetXCollectionSettings.EndsWithResources("Assets/Game/Resources/Config"), Is.False);
			Assert.That(SheetXCollectionSettings.EndsWithResources("Assets/Game/MyResources"), Is.False);
		}

		[Test]
		public void overlap_detects_containment_but_not_sibling_prefixes()
		{
			Assert.That(SheetXCollectionSettings.PathsOverlap("Assets/Game", "Assets/Game/Data"), Is.True);
			Assert.That(SheetXCollectionSettings.PathsOverlap("Assets/Game/Data", "Assets/Game"), Is.True);
			Assert.That(SheetXCollectionSettings.PathsOverlap("Assets/Game", "Assets/Game"), Is.True);
			// Sibling folders that merely share a name prefix are not nested.
			Assert.That(SheetXCollectionSettings.PathsOverlap("Assets/Game", "Assets/GameData"), Is.False);
			Assert.That(SheetXCollectionSettings.PathsOverlap("Assets/A/Code", "Assets/A/Assets"), Is.False);
		}
	}
}
