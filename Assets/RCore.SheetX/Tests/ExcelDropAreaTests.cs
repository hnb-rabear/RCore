/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.IO;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class ExcelDropAreaTests
	{
		private string m_root;

		[SetUp]
		public void SetUp()
		{
			m_root = Path.Combine(Path.GetTempPath(), "SheetXDropArea_" + Path.GetRandomFileName());
			Directory.CreateDirectory(m_root);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(m_root))
				Directory.Delete(m_root, true);
		}

		private string Touch(string relativePath)
		{
			string full = Path.Combine(m_root, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(full));
			File.WriteAllText(full, "");
			return full;
		}

		[Test]
		public void collect_excel_paths_keeps_dropped_xlsx_files()
		{
			string a = Touch("A.xlsx");
			string b = Touch("B.xlsx");

			var result = ExcelSheetXWindow.CollectExcelPaths(new[] { a, b });

			Assert.That(result, Is.EquivalentTo(new[] { a, b }));
		}

		[Test]
		public void collect_excel_paths_skips_non_excel_files()
		{
			string xlsx = Touch("A.xlsx");
			string txt = Touch("B.txt");

			var result = ExcelSheetXWindow.CollectExcelPaths(new[] { xlsx, txt });

			Assert.That(result, Is.EquivalentTo(new[] { xlsx }));
		}

		[Test]
		public void collect_excel_paths_expands_a_dropped_folder()
		{
			Touch("Data/A.xlsx");
			Touch("Data/B.xlsx");
			Touch("Data/C.txt");

			var result = ExcelSheetXWindow.CollectExcelPaths(new[] { Path.Combine(m_root, "Data") });

			Assert.That(result.Count, Is.EqualTo(2));
			Assert.That(result.TrueForAll(p => Path.GetExtension(p) == ".xlsx"));
		}

		[Test]
		public void collect_excel_paths_ignores_missing_paths()
		{
			var result = ExcelSheetXWindow.CollectExcelPaths(new[] { Path.Combine(m_root, "Nope.xlsx") });

			Assert.That(result, Is.Empty);
		}
	}
}
