using System;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXExportContextTests
	{
		[Test]
		public void write_records_file_only_after_output_returns()
		{
			var output = new RecordingOutput();
			var context = new SheetXExportContext(output);

			context.Write("Data", "Heroes.txt", "[]", SheetXExportFileType.Json);

			Assert.That(output.Calls, Is.EqualTo(1));
			Assert.That(context.ToResult().Files, Has.Count.EqualTo(1));
			Assert.That(context.ToResult().Files[0].RelativePath, Is.EqualTo("Data/Heroes.txt"));
		}

		[Test]
		public void write_exception_returns_error_and_does_not_record_file()
		{
			var context = new SheetXExportContext(new ThrowingOutput());

			context.Write("Data", "Heroes.txt", "[]", SheetXExportFileType.Json);

			var result = context.ToResult();
			Assert.That(result.Success, Is.False);
			Assert.That(result.Files, Is.Empty);
			Assert.That(result.Errors, Has.Count.EqualTo(1));
		}

		[Test]
		public void failed_write_can_retry_same_path()
		{
			var output = new RetryOutput();
			var context = new SheetXExportContext(output);

			context.Write("Data", "Heroes.txt", "[]", SheetXExportFileType.Json);
			context.Write("Data", "Heroes.txt", "[]", SheetXExportFileType.Json);

			Assert.That(output.Calls, Is.EqualTo(2));
			Assert.That(context.ToResult().Files, Has.Count.EqualTo(1));
		}

		[Test]
		public void duplicate_final_path_returns_error()
		{
			var context = new SheetXExportContext(new RecordingOutput());

			context.Write("Data", "Heroes.txt", "[]", SheetXExportFileType.Json);
			context.Write("Data", "Heroes.txt", "[]", SheetXExportFileType.Json);

			var result = context.ToResult();
			Assert.That(result.Success, Is.False);
			Assert.That(result.Files, Has.Count.EqualTo(1));
			Assert.That(result.Errors, Has.Count.EqualTo(1));
		}

		[Test]
		public void null_output_returns_result_error()
		{
			var result = SheetXExporter.ExportExcel(new SheetXExportRequest
			{
				SpreadsheetPath = "ignored.xlsx",
			}, null);

			Assert.That(result.Success, Is.False);
			Assert.That(result.Errors, Has.Count.EqualTo(1));
		}

		private sealed class RecordingOutput : ISheetXOutput
		{
			public int Calls { get; private set; }

			public void Write(string relativePath, string content)
			{
				Calls++;
			}
		}

		private sealed class ThrowingOutput : ISheetXOutput
		{
			public void Write(string relativePath, string content)
			{
				throw new InvalidOperationException("sink failed");
			}
		}

		private sealed class RetryOutput : ISheetXOutput
		{
			public int Calls { get; private set; }

			public void Write(string relativePath, string content)
			{
				Calls++;
				if (Calls == 1)
					throw new InvalidOperationException("first write failed");
			}
		}
	}
}
