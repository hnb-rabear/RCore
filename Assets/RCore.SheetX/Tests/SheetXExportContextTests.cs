using System;
using System.Collections.Generic;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXExportContextTests
	{
		private sealed class RecordingOutput : ISheetXOutput
		{
			public readonly List<string> WriteOrder = new List<string>();
			public readonly Dictionary<string, string> Writes =
				new Dictionary<string, string>(StringComparer.Ordinal);
			public string FailOnPath;

			public void Write(string relativePath, string content)
			{
				if (FailOnPath == relativePath)
					throw new InvalidOperationException("sink refused");

				WriteOrder.Add(relativePath);
				Writes.Add(relativePath, content);
			}
		}

		[Test]
		public void write_stages_and_does_not_reach_output_before_flush()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: false);

			context.Write("Generated", "IDs.cs", "a", SheetXExportFileType.Ids);

			Assert.That(output.WriteOrder, Is.Empty);
			Assert.That(context.ToResult().Files, Is.Empty);
		}

		[Test]
		public void flush_writes_staged_artifacts_in_stage_order()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: false);

			context.Write("Generated", "B.cs", "b", SheetXExportFileType.Constants);
			context.Write("Generated", "A.cs", "a", SheetXExportFileType.Ids);
			context.Flush();

			Assert.That(
				output.WriteOrder,
				Is.EqualTo(new[] { "Generated/B.cs", "Generated/A.cs" }));

			var result = context.ToResult();
			Assert.That(result.Files.Count, Is.EqualTo(2));
			Assert.That(result.Files[0].RelativePath, Is.EqualTo("Generated/B.cs"));
			Assert.That(result.Errors, Is.Empty);
		}

		[Test]
		public void flush_stops_after_sink_failure_and_keeps_earlier_files()
		{
			var output = new RecordingOutput { FailOnPath = "Generated/B.cs" };
			var context = new SheetXExportContext(output, discardStagedOnError: false);

			context.Write("Generated", "A.cs", "a", SheetXExportFileType.Ids);
			context.Write("Generated", "B.cs", "b", SheetXExportFileType.Constants);
			context.Write("Generated", "C.cs", "c", SheetXExportFileType.Constants);
			context.Flush();

			Assert.That(output.WriteOrder, Is.EqualTo(new[] { "Generated/A.cs" }));

			var result = context.ToResult();
			Assert.That(result.Files.Count, Is.EqualTo(1));
			Assert.That(result.Files[0].RelativePath, Is.EqualTo("Generated/A.cs"));
			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo("Writing 'Generated/B.cs' failed: sink refused"));
		}

		[Test]
		public void staged_path_collision_names_both_origins()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: true);

			context.SetOrigin("a.xlsx", "Data");
			context.Write("Generated", "Data.txt", "first", SheetXExportFileType.Json);
			context.SetOrigin("b.xlsx", "Data");
			context.Write("Generated", "Data.txt", "second", SheetXExportFileType.Json);

			Assert.That(
				context.ToResult().Errors,
				Has.Exactly(1).EqualTo(
					"Artifact 'Generated/Data.txt' collision: "
					+ "first 'a.xlsx' sheet 'Data'; second 'b.xlsx' sheet 'Data'."));
		}

		[Test]
		public void collision_without_origin_keeps_legacy_message()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: false);

			context.Write("Generated", "IDs.cs", "a", SheetXExportFileType.Ids);
			context.Write("Generated", "IDs.cs", "b", SheetXExportFileType.Ids);

			Assert.That(
				context.ToResult().Errors,
				Has.Exactly(1).EqualTo(
					"Artifact 'Generated/IDs.cs' was produced more than once."));
		}

		[Test]
		public void discarding_context_writes_nothing_when_an_error_stands()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: true);

			context.Write("Generated", "IDs.cs", "a", SheetXExportFileType.Ids);
			context.Error("something failed");
			context.Flush();

			Assert.That(output.WriteOrder, Is.Empty);
			Assert.That(context.ToResult().Files, Is.Empty);
		}

		[Test]
		public void non_discarding_context_flushes_despite_an_error()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: false);

			context.Write("Generated", "IDs.cs", "a", SheetXExportFileType.Ids);
			context.Error("ID HERO_1 is duplicated in sheet HeroIDs");
			context.Flush();

			Assert.That(output.Writes["Generated/IDs.cs"], Is.EqualTo("a"));
			Assert.That(context.ToResult().Files.Count, Is.EqualTo(1));
		}

		[Test]
		public void warning_does_not_block_flush()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: true);

			context.Write("Generated", "IDs.cs", "a", SheetXExportFileType.Ids);
			context.Warn("using the default encryption key");
			context.Flush();

			Assert.That(output.Writes.ContainsKey("Generated/IDs.cs"), Is.True);
			Assert.That(context.ToResult().Warnings.Count, Is.EqualTo(1));
		}

		[Test]
		public void reserved_path_stays_reserved_after_a_sink_failure()
		{
			var output = new RecordingOutput { FailOnPath = "Generated/A.cs" };
			var context = new SheetXExportContext(output, discardStagedOnError: false);

			context.Write("Generated", "A.cs", "a", SheetXExportFileType.Ids);
			context.Flush();
			context.Write("Generated", "A.cs", "retry", SheetXExportFileType.Ids);

			Assert.That(context.ToResult().Files, Is.Empty);
			Assert.That(
				context.ToResult().Errors,
				Is.EqualTo(new[]
				{
					"Writing 'Generated/A.cs' failed: sink refused",
					"Artifact 'Generated/A.cs' was produced more than once.",
				}));
		}

		[Test]
		public void flush_is_idempotent()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: false);

			context.Write("Generated", "A.cs", "a", SheetXExportFileType.Ids);
			context.Flush();
			context.Flush();

			Assert.That(output.WriteOrder.Count, Is.EqualTo(1));
			Assert.That(context.ToResult().Files.Count, Is.EqualTo(1));
		}
	}
}
