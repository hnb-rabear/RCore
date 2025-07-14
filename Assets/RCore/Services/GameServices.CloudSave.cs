/***
 * Author HNB-RaBear - 2022
 ***/

#if UNITY_ANDROID && GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RCore.Service
{
	public static partial class GameServices
	{
#if UNITY_ANDROID && GPGS
		private const string SAVE_NAME = "default_save";
		public static bool Authenticated => Social.Active.localUser.authenticated;
		private static bool m_Saving;
		private static string m_GameData = "";
		private static float m_TotalPlayTime;
		private static Action<bool> m_OnSave;
		private static Action<bool, string> m_OnLoad;
		private static ISavedGameMetadata m_SavedGame;
		public static string GameData => m_GameData;

		public delegate ConflictResolutionStrategy SavedGameConflictResolver(ISavedGameMetadata baseVersion, byte[] baseVersionData, ISavedGameMetadata remoteVersion, byte[] remoteVersionData);

		public static void OpenWithAutomaticConflictResolution(string fileName, DataSource dataSource, ConflictResolutionStrategy conflictResolutionStrategy,
			Action<ISavedGameMetadata, SavedGameRequestStatus> callback)
		{
			PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
				fileName,
				dataSource,
				conflictResolutionStrategy,
				(status, game) =>
				{
					Debug.Log($"OpenWithAutomaticConflictResolution {status}");
					callback?.Invoke(game, status);
				});
		}

		public static void OpenWithManualConflictResolution(string fileName, bool prefetchDataOnConflict, DataSource dataSource, SavedGameConflictResolver resolverFunction,
			Action<ISavedGameMetadata, SavedGameRequestStatus> completedCallback)
		{
			PlayGamesPlatform.Instance.SavedGame.OpenWithManualConflictResolution(fileName, dataSource, prefetchDataOnConflict,
				// Internal conflict callback
				(resolver, original, originalData, unmerged, unmergedData) =>
				{
					// Invoke the user's conflict resolving function, get their choice
					var choice = resolverFunction(original, originalData, unmerged, unmergedData);
					ISavedGameMetadata selectedGame = null;
					Debug.Log($"OpenWithManualConflictResolution {choice}");
					switch (choice)
					{
						case ConflictResolutionStrategy.UseOriginal:
							selectedGame = original;
							break;
						case ConflictResolutionStrategy.UseUnmerged:
							selectedGame = unmerged;
							break;
						default:
							Debug.LogError("Unhandled conflict resolution strategy: " + choice.ToString());
							break;
					}

					// Let the internal client know the selected saved game
					resolver.ChooseMetadata(selectedGame);
				},
				// Completed callback
				(status, game) =>
				{
					completedCallback?.Invoke(game, status);
				});
		}

		public static void ReadSavedGameData(ISavedGameMetadata savedGame, Action<ISavedGameMetadata, byte[], SavedGameRequestStatus> callback)
		{
			PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(
				savedGame,
				(status, data) =>
				{
					Debug.Log($"ReadSavedGameData {status}");
					callback?.Invoke(savedGame, data, status);
				}
			);
		}

		public static void WriteSavedGameData(ISavedGameMetadata savedGame, byte[] data, TimeSpan totalPlaytime, Action<ISavedGameMetadata, SavedGameRequestStatus> callback)
		{
			var builder = new SavedGameMetadataUpdate.Builder();
			builder = builder
				.WithUpdatedPlayedTime(totalPlaytime)
				.WithUpdatedDescription("Updated Time " + DateTime.Now);

			var updatedMetadata = builder.Build();

			PlayGamesPlatform.Instance.SavedGame.CommitUpdate(
				savedGame,
				updatedMetadata,
				data,
				(status, game) =>
				{
					Debug.Log($"WriteSavedGameData {status}");
					callback?.Invoke(game, status);
				}
			);
		}

		public static void FetchAllSavedGames(DataSource dataSource, Action<List<ISavedGameMetadata>, SavedGameRequestStatus> callback)
		{
			PlayGamesPlatform.Instance.SavedGame.FetchAllSavedGames(dataSource,
				(status, games) =>
				{
					var savedGames = new List<ISavedGameMetadata>();
					Debug.Log($"FetchAllSavedGames {status}");
					if (status == SavedGameRequestStatus.Success)
					{
						for (int i = 0; i < games.Count; i++)
						{
							savedGames.Add(games[i]);
						}
					}

					callback?.Invoke(savedGames, status);
				}
			);
		}

		public static void DeleteSavedGame(ISavedGameMetadata savedGame)
		{
			PlayGamesPlatform.Instance.SavedGame.Delete(savedGame);
		}

		public static void DeleteSelectedSavedGame()
		{
			PlayGamesPlatform.Instance.SavedGame.Delete(m_SavedGame);
		}

		public static void ShowSelectSavedGameUI(string uiTitle, Action<ISavedGameMetadata, SelectUIStatus> callback)
		{
			uint maxNumToDisplay = 3;
			bool allowCreateNew = true;
			bool allowDelete = true;

			var savedGameClient = PlayGamesPlatform.Instance.SavedGame;
			if (savedGameClient != null)
			{
				savedGameClient.ShowSelectSavedGameUI(Social.localUser.userName + "\u0027s saves", maxNumToDisplay, allowCreateNew, allowDelete,
					(status, saveGame) =>
					{
						// some error occured, just show window again
						if (status == SelectUIStatus.BadInputError
						    || status == SelectUIStatus.InternalError
						    || status == SelectUIStatus.TimeoutError)
						{
							ShowSelectSavedGameUI(uiTitle, callback);
							return;
						}

						m_SavedGame = saveGame;

						Debug.LogError($"Select {saveGame.Filename}");

						callback?.Invoke(saveGame, status);
					});
			}
			else
			{
				Debug.LogError("Save Game client is null...");
			}
		}

		public static Texture2D GetScreenshot()
		{
			// Create a 2D texture that is 1024x700 pixels from which the PNG will be
			// extracted
			var screenShot = new Texture2D(1024, 700);

			// Takes the screenshot from top left hand corner of screen and maps to top
			// left hand corner of screenShot texture
			screenShot.ReadPixels(
				new Rect(0, 0, Screen.width, Screen.width / 1024f * 700), 0, 0);
			return screenShot;
		}

		public static void DownloadSavedGame(Action<bool, string> pCallback = null)
		{
			DownloadSavedGame(SAVE_NAME, pCallback);
		}

		public static void DownloadSavedGame(string pFileName, Action<bool, string> pCallback = null)
		{
			if (!Authenticated)
			{
				Debug.Log("Not authenticated!");
				pCallback?.Invoke(false, null);
				return;
			}

			if (string.IsNullOrEmpty(pFileName))
				pFileName = SAVE_NAME;

			Debug.Log("Loading game progress from the cloud.");
			m_Saving = false;
			m_OnLoad = pCallback;
			((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
				pFileName, //name of file.
				DataSource.ReadCacheOrNetwork,
				ConflictResolutionStrategy.UseLongestPlaytime,
				OnSavedGameOpened);
		}

		public static void UploadSavedGame(string pContent, float pTotalPlayTime, Action<bool> pCallback = null)
		{
			UploadSavedGame(SAVE_NAME, pContent, pTotalPlayTime, pCallback);
		}

		public static void UploadSavedGame(string pFileName, string pContent, float pTotalPlayTime, Action<bool> pCallback = null)
		{
			if (!Authenticated)
			{
				Debug.Log("Not authenticated!");
				pCallback?.Invoke(false);
				return;
			}

			if (string.IsNullOrEmpty(pFileName))
				pFileName = SAVE_NAME;

			Debug.Log("Saving progress to the cloud... filename: " + pFileName);
			m_Saving = true;
			m_GameData = pContent;
			m_OnSave = pCallback;
			m_TotalPlayTime = pTotalPlayTime;
			//save to named file
			((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(
				pFileName, //name of file. If save doesn't exist it will be created with this name
				DataSource.ReadCacheOrNetwork,
				ConflictResolutionStrategy.UseLongestPlaytime,
				OnSavedGameOpened);
		}

		private static void OnSavedGameOpened(SavedGameRequestStatus status, ISavedGameMetadata gameMetaData)
		{
			if (status == SavedGameRequestStatus.Success)
			{
				if (m_Saving)
				{
					byte[] data = Encoding.UTF8.GetBytes(m_GameData);
					m_GameData = null;
					var builder = new SavedGameMetadataUpdate.Builder();
					builder.WithUpdatedPlayedTime(TimeSpan.FromSeconds(m_TotalPlayTime))
						.WithUpdatedDescription("Updated Date " + DateTime.Now);
					var updatedMetadata = builder.Build();
					((PlayGamesPlatform)Social.Active).SavedGame.CommitUpdate(gameMetaData, updatedMetadata, data,
						(status2, metaData2) =>
						{
							if (status2 == SavedGameRequestStatus.Success)
								Debug.Log("Game " + metaData2.Description + " written");
							else
								Debug.LogWarning("Error saving game: " + status2);

							m_OnSave?.Invoke(status2 == SavedGameRequestStatus.Success);
						});
				}
				else
				{
					((PlayGamesPlatform)Social.Active).SavedGame.ReadBinaryData(gameMetaData,
						(status2, byteData) =>
						{
							if (status2 == SavedGameRequestStatus.Success)
							{
								if (byteData == null)
								{
									Debug.Log("No data saved to the cloud yet...");
									return;
								}
								Debug.Log("Decoding cloud data from bytes.");
								//var sByteData = Convert.ToSByte(byteData);
								m_GameData = Encoding.UTF8.GetString(byteData);
							}
							else
								Debug.LogWarning("Error reading game: " + status2);

							m_OnLoad?.Invoke(status2 == SavedGameRequestStatus.Success, m_GameData);
						});
				}
			}
			else
				Debug.LogWarning("Error opening game: " + status);
		}
#elif UNITY_IOS && !UNITY_EDITOR
		public static bool Authenticated => true;
#else
		public static bool Authenticated => false;
		public static void ShowSelectSavedGameUI(string uiTitle, Action<ISavedGameMetadata, SelectUIStatus> p) => p?.Invoke(null, SelectUIStatus.AuthenticationError);
		public static void UploadSavedGame(string jsonData, float totalPlayTime, Action<bool> p = null) => p?.Invoke(false);
		public static void UploadSavedGame(string pFileName, string pContent, float pTotalPlayTime, Action<bool> pCallback = null) => pCallback?.Invoke(false);
		public static void DownloadSavedGame(string pFileName, Action<bool, string> pCallback = null) => pCallback?.Invoke(false, null);
		public static void DownloadSavedGame(Action<bool, string> p) => p?.Invoke(false, null);
		public static void DeleteSavedGame(ISavedGameMetadata data) { }
		public static void DeleteSelectedSavedGame() { }

		public interface ISavedGameMetadata
		{
			bool IsOpen { get; set; }
			string Filename { get; set; }
			string Description { get; set; }
			string CoverImageURL { get; set; }
			TimeSpan TotalTimePlayed { get; set; }
			DateTime LastModifiedTimestamp { get; set; }
		}

		public enum SelectUIStatus
		{
			SavedGameSelected = 1,
			UserClosedUI = 2,
			InternalError = -1,
			TimeoutError = -2,
			AuthenticationError = -3,
			BadInputError = -4,
			UiBusy = -5
		}
#endif
	}
}