using System.Collections.Generic;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace RCore.SheetX
{
	public class GoogleSheetHandler
	{
		private SheetXSettings m_settings;
		private readonly string[] m_scopes = { SheetsService.Scope.SpreadsheetsReadonly };
		
		public GoogleSheetHandler(SheetXSettings settings)
		{
			m_settings = settings;
		}
		
		public void Download()
		{
			string key = m_settings.google.path;
			if (string.IsNullOrEmpty(key))
			{
				UnityEngine.Debug.LogError("Key can not be empty");
				return;
			}

			Authenticate();
		}
		
		private void Authenticate()
		{
			// Create Google Sheets API service.
			var service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = AuthenticateGoogleStore(),
				ApplicationName = SheetXConstants.APPLICATION_NAME,
			});
			
			string googleSheetId = m_settings.google.path;
			
			// Fetch metadata for the entire spreadsheet.
			Spreadsheet spreadsheet;
			try
			{
				spreadsheet = service.Spreadsheets.Get(googleSheetId).Execute();
			}
			catch
			{
				return;
			}
			googleSheetName = spreadsheet.Properties.Title;
			TxtGoogleSheetName.Text = spreadsheet.Properties.Title;
			sheets = new List<GoogleSheetsPath.Sheet>();
			foreach (var sheet in spreadsheet.Sheets)
			{
				var sheetName = sheet.Properties.Title;
				sheets.Add(new GoogleSheetsPath.Sheet()
				{
					name = sheetName,
					selected = true,
				});
			}

			// Sync with current save
			// var savedSheets = m_settings.googleSheetsPaths.Find(x => x.id == googleSheetId);
			// if (savedSheets != null && savedSheets.sheets != null)
			// {
			// 	foreach (var sheet in sheets)
			// 	{
			// 		var existedSheet = savedSheets.sheets.Find(x => x.name == sheet.name);
			// 		if (existedSheet != null)
			// 			sheet.selected = existedSheet.selected;
			// 	}
			// }

			// m_bindingSheets = new BindingList<GoogleSheetsPath.Sheet>(sheets);
			// DtgGoogleSheets.DataSource = m_bindingSheets;
		}
		
		public UserCredential AuthenticateGoogleStore()
		{
			var clientSecrets = new ClientSecrets();
			clientSecrets.ClientId = m_settings.googleClientId;
			clientSecrets.ClientSecret = m_settings.googleClientSecret;

			// The file token.json stores the user's access and refresh tokens, and is created
			// automatically when the authorization flow completes for the first time.
			var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
				clientSecrets,
				m_scopes,
				"user",
				CancellationToken.None,
				new FileDataStore(m_settings.GetSaveDirectory(), true)).Result;

			UnityEngine.Debug.Log("Credential file saved to: " + m_settings.GetSaveDirectory());
			return credential;
		}
	}
}