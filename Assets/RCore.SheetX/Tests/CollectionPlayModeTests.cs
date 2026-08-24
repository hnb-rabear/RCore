using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	public class CollectionPlayModeTests
	{
		[Test]
		public void should_load_before_play_requires_enabled_feature_and_toggle()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			try
			{
				Assert.That(SheetXCollectionPlayModeLoader.ShouldLoadBeforePlay(settings, false), Is.False);

				settings.enableCollections = true;
				settings.autoLoadBeforePlay = false;
				Assert.That(SheetXCollectionPlayModeLoader.ShouldLoadBeforePlay(settings, false), Is.False);

				settings.autoLoadBeforePlay = true;
				Assert.That(SheetXCollectionPlayModeLoader.ShouldLoadBeforePlay(settings, true), Is.False);
				Assert.That(SheetXCollectionPlayModeLoader.ShouldLoadBeforePlay(settings, false), Is.True);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}
	}
}
