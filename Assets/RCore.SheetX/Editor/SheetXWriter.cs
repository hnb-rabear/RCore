/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// The single place the handlers emit anything: artifacts, errors, warnings, progress notes. With a
	/// <see cref="SheetXExportContext"/> everything travels through the caller's output and result; with
	/// none it falls back to the filesystem, dialogs, and the console the editor windows have always used.
	/// </summary>
	internal sealed class SheetXWriter
	{
		private readonly SheetXSettings m_settings;
		private readonly SheetXExportContext m_context;
		private readonly ISheetXOutput m_output;

		public SheetXWriter(SheetXSettings settings, SheetXExportContext context)
		{
			m_settings = settings;
			m_context = context;
			m_output = context == null ? new SheetXFileOutput() : null;
		}

		/// <summary>True when this writer routes through an export context instead of the disk.</summary>
		public bool Detached => m_context != null;

		/// <summary>
		/// Emits one finished artifact. <paramref name="pLogMessage"/> is the console line the editor
		/// windows print; an external caller learns the same from the returned file list instead.
		/// </summary>
		public void Write(string pFolder, string pFileName, string pContent, SheetXExportFileType pType, string pLogMessage = null)
		{
			if (m_context != null)
			{
				m_context.Write(pFolder, pFileName, pContent, pType);
				return;
			}
			m_output.Write(System.IO.Path.Combine(pFolder ?? "", pFileName ?? ""), pContent);
			Debug.Log(pLogMessage ?? $"Exported {pFileName}!");
		}

		/// <summary>Reports a failure that leaves the output incomplete or wrong.</summary>
		public void Error(string pMessage)
		{
			if (m_context != null)
				m_context.Error(pMessage);
			else
				Debug.LogError(pMessage);
		}

		/// <summary>Reports a problem that did not stop the export.</summary>
		public void Warn(string pMessage)
		{
			if (m_context != null)
				m_context.Warn(pMessage);
			else
				Debug.LogWarning(pMessage);
		}

		/// <summary>
		/// Reports a data defect the editor windows raise as a modal. A dialog would hang a batch or CI
		/// caller with nobody to click OK, so a detached export gets the same text as a result error.
		/// </summary>
		public void Blocking(string pTitle, string pMessage)
		{
			if (m_context != null)
				m_context.Error(pMessage);
			else
				EditorUtility.DisplayDialog(pTitle, pMessage, "OK");
		}

		/// <summary>A progress note. Dropped when detached — the result carries the outcome.</summary>
		public void Info(string pMessage)
		{
			if (m_context == null)
				Debug.Log(pMessage);
		}

		/// <summary>
		/// Loads one code template out of Resources. A template missing from the consuming project used to
		/// surface as a NullReferenceException inside the generator; here it is a named error instead.
		/// </summary>
		public bool TryLoadTemplate(string pTemplateName, out string pContent)
		{
			var asset = Resources.Load<TextAsset>(pTemplateName);
			if (asset == null)
			{
				pContent = null;
				Error($"Template '{pTemplateName}' was not found in any Resources folder. "
					+ "Reinstall SheetX or restore its Editor/Resources templates.");
				return false;
			}
			pContent = asset.text;
			return true;
		}

		/// <summary>Generates the IDs C# file from its template and emits it.</summary>
		public void CreateFileIDs(string pFileName, string pContent)
		{
			if (string.IsNullOrEmpty(pContent))
				return;
			if (!TryLoadTemplate(SheetXConstants.IDS_CS_TEMPLATE, out string fileContent))
				return;
			fileContent = fileContent.Replace("_IDS_CLASS_NAME_", pFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", pContent);
			fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
			Write(m_settings.constantsOutputFolder, $"{pFileName}.cs", fileContent, SheetXExportFileType.Ids, $"Exported {pFileName}.cs!");
		}

		/// <summary>Generates the constants C# file from its template and emits it.</summary>
		public void CreateFileConstants(string pContent, string pFileName)
		{
			if (string.IsNullOrEmpty(pContent))
				return;
			if (!TryLoadTemplate(SheetXConstants.CONSTANTS_CS_TEMPLATE, out string fileContent))
				return;
			fileContent = fileContent.Replace("_CONST_CLASS_NAME_", pFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", pContent);
			fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
			Write(m_settings.constantsOutputFolder, $"{pFileName}.cs", fileContent, SheetXExportFileType.Constants, $"Exported {pFileName}.cs!");
		}
	}
}
