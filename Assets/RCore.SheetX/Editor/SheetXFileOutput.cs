/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.IO;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// The output the SheetX windows use: writes each artifact to the folder the settings asset points at.
	/// Exists so the disk is one <see cref="ISheetXOutput"/> among others rather than a special case inside
	/// the generators — every artifact leaves through <see cref="ISheetXOutput.Write"/>, legacy path included.
	/// </summary>
	internal sealed class SheetXFileOutput : ISheetXOutput
	{
		public void Write(string relativePath, string content)
		{
			SheetXHelper.WriteFile(Path.GetDirectoryName(relativePath) ?? "", Path.GetFileName(relativePath), content);
		}
	}
}
