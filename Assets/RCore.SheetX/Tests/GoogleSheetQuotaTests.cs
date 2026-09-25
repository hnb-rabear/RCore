/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Google;
using Google.Apis.Sheets.v4.Data;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	/// <summary>
	/// The Sheets API allows 60 read requests per minute per user. An export over a list of
	/// spreadsheets used to spend one request per sheet per pass and trip that limit, so these lock
	/// the two pieces that keep the count down: one batched range list per spreadsheet, and a backoff
	/// that survives a 429 instead of failing the run.
	/// </summary>
	public class GoogleSheetQuotaTests
	{
		private static Spreadsheet Metadata(params (string name, int columns)[] sheets)
			=> new Spreadsheet
			{
				Sheets = sheets.Select(s => new Sheet
				{
					Properties = new SheetProperties
					{
						Title = s.name,
						GridProperties = new GridProperties { ColumnCount = s.columns },
					},
				}).ToList(),
			};

		private static List<SheetPath> Sheets(params (string name, bool selected)[] sheets)
			=> sheets.Select(s => new SheetPath { name = s.name, selected = s.selected }).ToList();

		private static GoogleApiException Quota()
			=> new GoogleApiException("sheets", "Quota exceeded for quota metric 'Read requests'")
			{
				HttpStatusCode = HttpStatusCode.TooManyRequests,
			};

		[Test]
		public void export_ranges_cover_every_selected_sheet_once()
		{
			var metadata = Metadata(
				("ExampleIDs", 4), ("ExampleConstants", 26), ("ExampleData1", 3), ("Unlisted", 2));

			var ranges = GoogleSheetHandler.BuildExportRanges(
				metadata,
				Sheets(("ExampleIDs", true), ("ExampleConstants", true), ("ExampleData1", true),
					("Missing", true), ("ExampleData2", false)),
				includeConfiguration: false);

			Assert.That(ranges, Is.EqualTo(new[]
			{
				// A Constants sheet is read as four columns, exactly as the per-sheet export read it.
				"ExampleIDs!A1:D", "ExampleConstants!A1:D", "ExampleData1!A1:C",
			}));
		}

		[Test]
		public void export_ranges_lead_with_configuration_and_never_repeat_a_sheet()
		{
			var metadata = Metadata(("Configuration", 4), ("ExampleData1", 3));

			var ranges = GoogleSheetHandler.BuildExportRanges(
				metadata,
				Sheets(("Configuration", true), ("ExampleData1", true)),
				includeConfiguration: true);

			// Configuration is read by its own pass and by the Json pass; one range serves both.
			Assert.That(ranges, Is.EqualTo(new[] { "Configuration!A1:D", "ExampleData1!A1:C" }));
		}

		[Test]
		public void export_ranges_skip_configuration_when_the_spreadsheet_has_none()
		{
			var ranges = GoogleSheetHandler.BuildExportRanges(
				Metadata(("ExampleData1", 3)), Sheets(("ExampleData1", true)), includeConfiguration: true);

			Assert.That(ranges, Is.EqualTo(new[] { "ExampleData1!A1:C" }));
		}

		[Test]
		public void range_batches_never_exceed_the_batch_limit()
		{
			var ranges = Enumerable.Range(0, 120).Select(i => $"Sheet{i}!A1:C").ToList();

			var batches = GoogleSheetHandler.SplitRanges(ranges, 50);

			Assert.That(batches.Select(b => b.Count), Is.EqualTo(new[] { 50, 50, 20 }));
			Assert.That(batches.SelectMany(b => b), Is.EqualTo(ranges));
		}

		[Test]
		public void quota_errors_back_off_before_each_retry_and_then_surface()
		{
			var delays = new List<int>();
			var original = GoogleSheetHandler.RetryDelay;
			GoogleSheetHandler.RetryDelay = delays.Add;
			int attempts = 0;
			try
			{
				Assert.Throws<GoogleApiException>(() => GoogleSheetHandler.ExecuteWithRetry<int>(() =>
				{
					attempts++;
					throw Quota();
				}));

				Assert.That(attempts, Is.EqualTo(4));
				Assert.That(delays, Is.EqualTo(new[] { 10000, 20000, 40000 }));
			}
			finally
			{
				GoogleSheetHandler.RetryDelay = original;
			}
		}

		[Test]
		public void a_quota_error_that_clears_returns_the_response()
		{
			var logs = new List<string>();
			var original = GoogleSheetHandler.RetryDelay;
			GoogleSheetHandler.RetryDelay = _ => { };
			int attempts = 0;
			try
			{
				int result = GoogleSheetHandler.ExecuteWithRetry(() =>
				{
					attempts++;
					if (attempts == 1)
						throw Quota();
					return 7;
				}, logs.Add);

				Assert.That(result, Is.EqualTo(7));
				Assert.That(attempts, Is.EqualTo(2));
				// The user sees why the editor paused instead of a frozen window.
				Assert.That(logs, Has.Exactly(1).Contains("10s"));
			}
			finally
			{
				GoogleSheetHandler.RetryDelay = original;
			}
		}

		[Test]
		public void a_non_quota_error_is_not_retried()
		{
			var original = GoogleSheetHandler.RetryDelay;
			GoogleSheetHandler.RetryDelay = _ => Assert.Fail("A non-quota failure must not back off.");
			int attempts = 0;
			try
			{
				Assert.Throws<GoogleApiException>(() => GoogleSheetHandler.ExecuteWithRetry<int>(() =>
				{
					attempts++;
					throw new GoogleApiException("sheets", "Requested entity was not found.")
					{
						HttpStatusCode = HttpStatusCode.NotFound,
					};
				}));

				Assert.That(attempts, Is.EqualTo(1));
			}
			finally
			{
				GoogleSheetHandler.RetryDelay = original;
			}
		}
	}
}
