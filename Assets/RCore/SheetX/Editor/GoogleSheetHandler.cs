using System.Collections.Generic;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;

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
		
		public void Download(GoogleSheetsPath googleSheetsPath)
		{
			string key = googleSheetsPath.id;
			if (string.IsNullOrEmpty(key))
			{
				UnityEngine.Debug.LogError("Key can not be empty");
				return;
			}

			Authenticate(googleSheetsPath);
		}
		
		private void Authenticate(GoogleSheetsPath googleSheetsPath)
		{
			// Create Google Sheets API service.
			var service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = AuthenticateGoogleStore(),
				ApplicationName = SheetXConstants.APPLICATION_NAME,
			});
			
			string googleSheetId = googleSheetsPath.id;
			
			// Fetch metadata for the entire spreadsheet.
			Spreadsheet spreadsheet;
			try
			{
				spreadsheet = service.Spreadsheets.Get(googleSheetId).Execute();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
				return;
			}
			googleSheetsPath.name = spreadsheet.Properties.Title;
			var sheets = new List<SheetPath>();
			foreach (var sheet in spreadsheet.Sheets)
			{
				var sheetName = sheet.Properties.Title;
				sheets.Add(new SheetPath()
				{
					name = sheetName,
					selected = true,
				});
			}

			// Sync with current save
			foreach (var sheet in sheets)
			{
				var existedSheet = googleSheetsPath.sheets.Find(x => x.name == sheet.name);
				if (existedSheet != null)
					sheet.selected = existedSheet.selected;
				else
					googleSheetsPath.sheets.Add(new SheetPath()
					{
						name = sheet.name,
						selected = true,
					});
			}
		}

		private UserCredential AuthenticateGoogleStore()
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

		public void ExportAll()
		{
		}
		
		//======================================
		
#region Export IDs

		public void ExportIDs()
		{
		}

#endregion
		
#region Export Constants

		public void ExportConstants()
		{
		}

#endregion
		
#region Export Localizations

		public void ExportLocalizations()
		{
		}

#endregion
		
#region Export Json

		public void ExportJson()
		{
		}

#endregion
	}
}