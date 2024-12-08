using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using NPOI.SS.UserModel;
using UnityEditor;
using UnityEngine;

namespace RCore.LXLite.Editor
{
	public static class LXLiteConfig
	{
		public const string APPLICATION_NAME = "LocalizationX - Lite";
		public const string LOCALIZATION_TEMPLATE = "LocalizationTemplateV2";
		public const string LOCALIZATION_SHEET = "Localization";
		public const string LOCALIZATION_TEXT_TEMPLATE = "LocalizationTextTemplate";

		static LXLiteConfig()
		{
			m_LocalizationOutputFolder = EditorPrefs.GetString(nameof(LocalizationOutputFolder) + Application.identifier);
			m_scriptsOutputFolder = EditorPrefs.GetString(nameof(ScriptsOutputFolder) + Application.identifier);
			m_googleSpreadsheetsId = EditorPrefs.GetString(nameof(GoogleSpreadsheetsId) + Application.identifier);
			m_clientId = EditorPrefs.GetString(nameof(ClientId) + Application.identifier);
			m_clientSecret = EditorPrefs.GetString(nameof(ClientSecret) + Application.identifier);
			m_excelFilePath = EditorPrefs.GetString(nameof(ExcelFilePath) + Application.identifier);
			m_langCharSets = EditorPrefs.GetString(nameof(LangCharSets) + Application.identifier);
		}
		private static string m_LocalizationOutputFolder;
		public static string LocalizationOutputFolder
		{
			get => m_LocalizationOutputFolder;
			set
			{
				if (m_LocalizationOutputFolder == value)
					return;
				m_LocalizationOutputFolder = value;
				EditorPrefs.SetString(nameof(LocalizationOutputFolder) + Application.identifier, value);
			}
		}
		private static string m_scriptsOutputFolder;
		public static string ScriptsOutputFolder
		{
			get => m_scriptsOutputFolder;
			set
			{
				if (m_scriptsOutputFolder == value)
					return;
				m_scriptsOutputFolder = value;
				EditorPrefs.SetString(nameof(ScriptsOutputFolder) + Application.identifier, value);
			}
		}
		private static string m_googleSpreadsheetsId;
		public static string GoogleSpreadsheetsId
		{
			get => m_googleSpreadsheetsId;
			set
			{
				if (m_googleSpreadsheetsId == value)
					return;
				m_googleSpreadsheetsId = value;
				EditorPrefs.SetString(nameof(GoogleSpreadsheetsId) + Application.identifier, value);
			}
		}
		private static string m_clientId;
		public static string ClientId
		{
			get => m_clientId;
			set
			{
				if (m_clientId == value)
					return;
				m_clientId = value;
				EditorPrefs.SetString(nameof(ClientId) + Application.identifier, value);
			}
		}
		private static string m_clientSecret;
		public static string ClientSecret
		{
			get => m_clientSecret;
			set
			{
				if (m_clientSecret == value)
					return;
				m_clientSecret = value;
				EditorPrefs.SetString(nameof(ClientSecret) + Application.identifier, value);
			}
		}
		private static string m_excelFilePath;
		public static string ExcelFilePath
		{
			get => m_excelFilePath;
			set
			{
				if (m_excelFilePath == value)
					return;
				m_excelFilePath = value;
				EditorPrefs.SetString(nameof(ExcelFilePath) + Application.identifier, value);
			}
		}
		private static string m_langCharSets;
		public static string LangCharSets
		{
			get => m_langCharSets;
			set
			{
				if (m_langCharSets == value)
					return;
				m_langCharSets = value;
				EditorPrefs.SetString(nameof(LangCharSets) + Application.identifier, value);
			}
		}
	}

	public static class LXLiteHelper
	{
		public static Dictionary<string, string> GenerateCharacterSets(Dictionary<string, string> pContentGroups)
		{
			var output = new Dictionary<string, string>();
			foreach (var map in pContentGroups)
				output.Add(map.Key, GenerateCharacterSet(map.Value));
			return output;
		}
		public static string GenerateCharacterSet(string pContent)
		{
			string charactersSet = "";
			var unique = new HashSet<char>(pContent);
			foreach (char c in unique)
				charactersSet += c;
			charactersSet = string.Concat(charactersSet.OrderBy(c => c));
			return charactersSet;
		}
		public static void WriteFile(string pFolderPath, string pFileName, string pContent)
		{
			if (!Directory.Exists(pFolderPath))
				Directory.CreateDirectory(pFolderPath);

			string filePath = $"{pFolderPath}\\{pFileName}";
			if (!File.Exists(filePath))
				using (File.Create(filePath)) { }

			using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
			sw.WriteLine(pContent);
			sw.Close();
		}
		public static string ToCellString(this ICell cell, string pDefault = "")
		{
			if (cell == null)
				return pDefault;
			string cellStr;
			if (cell.CellType == CellType.Formula)
			{
				switch (cell.CachedFormulaResultType)
				{
					case CellType.Numeric:
						cellStr = cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
						break;

					case CellType.String:
						cellStr = cell.StringCellValue;
						break;

					case CellType.Boolean:
						cellStr = cell.BooleanCellValue.ToString();
						break;

					default:
						cellStr = cell.ToString();
						break;
				}
			}
			else
				cellStr = cell.ToString();
			return cellStr;
		}
		public static string RemoveSpecialCharacters(this string str, string replace = "")
		{
			return Regex.Replace(str, "[^a-zA-Z0-9_.]+", replace, RegexOptions.Compiled);
		}
		public static UserCredential AuthenticateGoogleUser(string googleClientId, string googleClientSecret)
		{
			var clientSecrets = new ClientSecrets();
			clientSecrets.ClientId = googleClientId;
			clientSecrets.ClientSecret = googleClientSecret;

			// The file token.json stores the user's access and refresh tokens, and is created
			// automatically when the authorization flow completes for the first time.
			var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
				clientSecrets,
				new[] { SheetsService.Scope.SpreadsheetsReadonly },
				"user",
				CancellationToken.None,
				new FileDataStore(GetSaveDirectory(), true)).Result;

			UnityEngine.Debug.Log("Credential file saved to: " + GetSaveDirectory());
			return credential;
		}
		private static string GetSaveDirectory()
		{
			var path = Path.Combine(Application.dataPath, nameof(Editor));
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			return path;
		}
	}

	public class LocalizationBuilder
	{
		public List<string> idsString = new();
		public Dictionary<string, List<string>> languageTextDict = new();
	}
}