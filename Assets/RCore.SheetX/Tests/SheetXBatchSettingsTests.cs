using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXBatchSettingsTests
	{
		[Test]
		public void batch_transient_settings_map_every_request_field()
		{
			var request = new SheetXBatchExportRequest
			{
				ConstantsOutputPath = "Gen/Constants",
				JsonOutputPath = "Gen/Json",
				LocalizationOutputPath = "Gen/Loc",
				CombineJson = true,
				SeparateIDs = true,
				SeparateConstants = true,
				SeparateLocalizations = true,
				OnlyEnumAsIDs = true,
				Namespace = "Game.Gen",
				PersistentFields = "id,name",
				EncryptJson = true,
				EncryptionKey = "unit-test-key",
			};

			var settings = SheetXSettings.CreateTransient(request);

			Assert.That(settings.constantsOutputFolder, Is.EqualTo("Gen/Constants"));
			Assert.That(settings.jsonOutputFolder, Is.EqualTo("Gen/Json"));
			Assert.That(settings.localizationOutputFolder, Is.EqualTo("Gen/Loc"));
			Assert.That(settings.combineJson, Is.True);
			Assert.That(settings.separateIDs, Is.True);
			Assert.That(settings.separateConstants, Is.True);
			Assert.That(settings.separateLocalizations, Is.True);
			Assert.That(settings.onlyEnumAsIDs, Is.True);
			Assert.That(settings.@namespace, Is.EqualTo("Game.Gen"));
			Assert.That(settings.persistentFields, Is.EqualTo("id,name"));
			Assert.That(settings.encryptJson, Is.True);
			Assert.That(settings.encryptionKey, Is.EqualTo("unit-test-key"));
		}

		[Test]
		public void batch_transient_settings_are_silent()
		{
			var settings = SheetXSettings.CreateTransient(
				new SheetXBatchExportRequest());

			Assert.That(settings.silent, Is.True);
		}

		[Test]
		public void batch_transient_settings_map_null_strings_to_empty()
		{
			var settings = SheetXSettings.CreateTransient(
				new SheetXBatchExportRequest());

			Assert.That(settings.constantsOutputFolder, Is.EqualTo(""));
			Assert.That(settings.jsonOutputFolder, Is.EqualTo(""));
			Assert.That(settings.localizationOutputFolder, Is.EqualTo(""));
			Assert.That(settings.@namespace, Is.EqualTo(""));
			Assert.That(settings.persistentFields, Is.EqualTo(""));
		}

		[Test]
		public void empty_encryption_key_keeps_the_default()
		{
			var expected = SheetXSettings.CreateTransient(
				new SheetXExportRequest()).encryptionKey;

			var settings = SheetXSettings.CreateTransient(
				new SheetXBatchExportRequest { EncryptionKey = "" });

			Assert.That(settings.encryptionKey, Is.EqualTo(expected));
		}

		[Test]
		public void single_source_transient_settings_are_not_silent()
		{
			var settings = SheetXSettings.CreateTransient(new SheetXExportRequest());

			Assert.That(settings.silent, Is.False);
		}

		[Test]
		public void batch_transient_settings_expose_the_default_key_flag()
		{
			var defaulted = SheetXSettings.CreateTransient(
				new SheetXBatchExportRequest { EncryptJson = true });
			var custom = SheetXSettings.CreateTransient(
				new SheetXBatchExportRequest
				{
					EncryptJson = true,
					EncryptionKey = "unit-test-key",
				});

			Assert.That(defaulted.UsesDefaultEncryptionKey, Is.True);
			Assert.That(custom.UsesDefaultEncryptionKey, Is.False);
		}
	}
}
