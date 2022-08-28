#if UNITY_ANDROID && GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif
using RCore.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RCore.Service
{
#if UNITY_ANDROID && GPGS
    public class SavedGame : ISavedGameMetadata
    {
        public bool IsOpen { get; set; }
        public string Filename { get; set; }
        public string Description { get; set; }
        public string CoverImageURL { get; set; }
        public TimeSpan TotalTimePlayed { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }
        public SavedGame(ISavedGameMetadata metadata)
        {
            IsOpen = metadata.IsOpen;
            Filename = metadata.Filename;
            Description = metadata.Description;
            CoverImageURL = metadata.CoverImageURL;
            TotalTimePlayed = metadata.TotalTimePlayed;
            LastModifiedTimestamp = metadata.LastModifiedTimestamp;
        }
    }
#else
    public class SavedGame
    {
        public bool IsOpen { get; set; }
        public string Filename { get; set; }
        public string Description { get; set; }
        public string CoverImageURL { get; set; }
        public TimeSpan TotalTimePlayed { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }
    }
#endif

    public class AndroidSaveGameServices
    {
#if UNITY_ANDROID && GPGS
        public delegate ConflictResolutionStrategy SavedGameConflictResolver(SavedGame baseVersion, byte[] baseVersionData, SavedGame remoteVersion, byte[] remoteVersionData);

        public static void OpenWithAutomaticConflictResolution(string fileName, DataSource dataSource, ConflictResolutionStrategy conflictResolutionStrategy, Action<SavedGame, SavedGameRequestStatus> callback)
        {
            RUtil.NullArgumentTest(fileName);
            RUtil.NullArgumentTest(callback);

            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
                fileName,
                dataSource,
                conflictResolutionStrategy,
                (SavedGameRequestStatus status, ISavedGameMetadata game) =>
                {
                    callback(game != null ? new SavedGame(game) : null, status);
                });
        }

        public static void OpenWithManualConflictResolution(string fileName, bool prefetchDataOnConflict, DataSource dataSource, SavedGameConflictResolver resolverFunction, Action<SavedGame, SavedGameRequestStatus> completedCallback)
        {
            RUtil.NullArgumentTest(fileName);
            RUtil.NullArgumentTest(resolverFunction);
            RUtil.NullArgumentTest(completedCallback);

            PlayGamesPlatform.Instance.SavedGame.OpenWithManualConflictResolution(fileName, dataSource, prefetchDataOnConflict,
                // Internal conflict callback
                (IConflictResolver resolver, ISavedGameMetadata original, byte[] originalData, ISavedGameMetadata unmerged, byte[] unmergedData) =>
                {
                    // Invoke the user's conflict resolving function, get their choice
                    var choice = resolverFunction(new SavedGame(original), originalData, new SavedGame(unmerged), unmergedData);
                    ISavedGameMetadata selectedGame = null;

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
                (SavedGameRequestStatus status, ISavedGameMetadata game) =>
                {
                    completedCallback(game != null ? new SavedGame(game) : null, status);
                });
        }

        public static void ReadSavedGameData(SavedGame savedGame, Action<SavedGame, byte[], SavedGameRequestStatus> callback)
        {
            RUtil.NullArgumentTest(savedGame);
            RUtil.NullArgumentTest(callback);

            PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(
                savedGame,
                (SavedGameRequestStatus status, byte[] data) =>
                {
                    callback(savedGame, data, status);
                }
            );
        }

        public static void WriteSavedGameData(SavedGame savedGame, byte[] data, TimeSpan totalPlaytime, Action<SavedGame, SavedGameRequestStatus> callback)
        {
            RUtil.NullArgumentTest(savedGame);
            RUtil.NullArgumentTest(data);
            RUtil.NullArgumentTest(callback);

            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            builder = builder
                .WithUpdatedPlayedTime(totalPlaytime)
                .WithUpdatedDescription("Saved game at " + DateTime.Now);

            SavedGameMetadataUpdate updatedMetadata = builder.Build();

            PlayGamesPlatform.Instance.SavedGame.CommitUpdate(
                savedGame,
                updatedMetadata,
                data,
                (SavedGameRequestStatus status, ISavedGameMetadata game) =>
                {
                    callback(game != null ? new SavedGame(game) : null, status);
                }
            );
        }

        public static void FetchAllSavedGames(DataSource dataSource, Action<List<SavedGame>, SavedGameRequestStatus> callback)
        {
            RUtil.NullArgumentTest(callback);

            PlayGamesPlatform.Instance.SavedGame.FetchAllSavedGames(dataSource,
                (SavedGameRequestStatus status, List<ISavedGameMetadata> games) =>
                {
                    var savedGames = new List<SavedGame>();

                    if (status == SavedGameRequestStatus.Success)
                    {
                        for (int i = 0; i < games.Count; i++)
                        {
                            savedGames.Add(new SavedGame(games[i]));
                        }
                    }

                    callback(savedGames, status);
                }
            );
        }

        public static void DeleteSavedGame(SavedGame savedGame)
        {
            RUtil.NullArgumentTest(savedGame);
            PlayGamesPlatform.Instance.SavedGame.Delete(savedGame);
        }

        public static void ShowSelectSavedGameUI(string uiTitle, uint maxDisplayedSavedGames, Action<SavedGame, SelectUIStatus> callback)
        {
            RUtil.NullArgumentTest(uiTitle);
            RUtil.NullArgumentTest(callback);
            PlayGamesPlatform.Instance.SavedGame.ShowSelectSavedGameUI(uiTitle,
                maxDisplayedSavedGames,
                true,
                true,
                (SelectUIStatus status, ISavedGameMetadata game) =>
                {
                    if (status == SelectUIStatus.SavedGameSelected)
                    {
                        // Handle saved game selected
                        callback(new SavedGame(game), SelectUIStatus.SavedGameSelected);
                    }
                    else
                    {
                        // Handle cancel or error
                        callback(null, status);
                    }
                }
            );
        }

        public static Texture2D getScreenshot()
        {
            // Create a 2D texture that is 1024x700 pixels from which the PNG will be
            // extracted
            Texture2D screenShot = new Texture2D(1024, 700);

            // Takes the screenshot from top left hand corner of screen and maps to top
            // left hand corner of screenShot texture
            screenShot.ReadPixels(
                new Rect(0, 0, Screen.width, (Screen.width / 1024) * 700), 0, 0);
            return screenShot;
        }

        public static void SaveSimple(string gameData, float totalPlayTime, Action<bool> pCallback)
		{
			OpenWithAutomaticConflictResolution("gameData", DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime, (metaData, status) =>
			{
				//Save
				if (status == SavedGameRequestStatus.Success)
				{
					var byteData = ASCIIEncoding.ASCII.GetBytes(gameData);
					WriteSavedGameData(metaData, byteData, TimeSpan.FromSeconds(totalPlayTime), (metaData2, status2) =>
					{
						Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(metaData2));
						pCallback?.Invoke(status2 == SavedGameRequestStatus.Success);
					});
				}
				else
					pCallback?.Invoke(false);
			});
		}

		public static void LoadSimple(Action<string, SavedGame> pCallback)
		{
			OpenWithAutomaticConflictResolution("gameData", DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime, (metaData, status) =>
			{
				//Load
				if (status == SavedGameRequestStatus.Success)
				{
					ReadSavedGameData(metaData, (metaData2, byteData, status2) =>
					{
						if (status2 == SavedGameRequestStatus.Success)
						{
							string gameData = ASCIIEncoding.ASCII.GetString(byteData);
							pCallback?.Invoke(gameData, metaData2);
						}
						else
							pCallback?.Invoke(null, null);
					});
				}
				else
					pCallback?.Invoke(null, null);
			});
		}
#endif
    }
}