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
    public class SavedGame
    {
#if UNITY_ANDROID && GPGS
        public bool IsOpen => GPGSSavedGameMetadata.IsOpen;
        public string Filename => GPGSSavedGameMetadata.Filename;
        public DateTime ModificationDate => GPGSSavedGameMetadata.LastModifiedTimestamp;
        public string Description => GPGSSavedGameMetadata.Description;
        public string CoverImageURL => GPGSSavedGameMetadata.CoverImageURL;
        public TimeSpan TotalTimePlayed => GPGSSavedGameMetadata.TotalTimePlayed;
        public ISavedGameMetadata GPGSSavedGameMetadata { get; private set; }
        public SavedGame(ISavedGameMetadata metadata)
        {
            GPGSSavedGameMetadata = metadata;
        }
#endif
    }

    public class AndroidSaveGameServices
    {
#if UNITY_ANDROID && GPGS
        public delegate ConflictResolutionStrategy SavedGameConflictResolver(SavedGame baseVersion, byte[] baseVersionData, SavedGame remoteVersion, byte[] remoteVersionData);

        public static void OpenWithAutomaticConflictResolution(string name, DataSource dataSource, ConflictResolutionStrategy conflictResolutionStrategy, Action<SavedGame, SavedGameRequestStatus> callback)
        {
            Util.NullArgumentTest(name);
            Util.NullArgumentTest(callback);

            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
                name,
                dataSource,
                conflictResolutionStrategy,
                (SavedGameRequestStatus status, ISavedGameMetadata game) =>
                {
                    callback(game != null ? new SavedGame(game) : null, status);
                });
        }

        public static void OpenWithManualConflictResolution(string name, bool prefetchDataOnConflict, DataSource dataSource, SavedGameConflictResolver resolverFunction, Action<SavedGame, SavedGameRequestStatus> completedCallback)
        {
            Util.NullArgumentTest(name);
            Util.NullArgumentTest(resolverFunction);
            Util.NullArgumentTest(completedCallback);

            PlayGamesPlatform.Instance.SavedGame.OpenWithManualConflictResolution(name, dataSource, prefetchDataOnConflict,
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
            Util.NullArgumentTest(savedGame);
            Util.NullArgumentTest(callback);

            PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(
                savedGame.GPGSSavedGameMetadata,
                (SavedGameRequestStatus status, byte[] data) =>
                {
                    callback(savedGame, data, status);
                }
            );
        }

        public static void WriteSavedGameData(SavedGame savedGame, byte[] data, TimeSpan totalPlaytime, Action<SavedGame, SavedGameRequestStatus> callback)
        {
            Util.NullArgumentTest(savedGame);
            Util.NullArgumentTest(data);
            Util.NullArgumentTest(callback);

            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            builder = builder
                .WithUpdatedPlayedTime(totalPlaytime)
                .WithUpdatedDescription("Saved game at " + DateTime.Now);

            SavedGameMetadataUpdate updatedMetadata = builder.Build();

            PlayGamesPlatform.Instance.SavedGame.CommitUpdate(
                savedGame.GPGSSavedGameMetadata,
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
            Util.NullArgumentTest(callback);

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
            Util.NullArgumentTest(savedGame);
            PlayGamesPlatform.Instance.SavedGame.Delete(savedGame.GPGSSavedGameMetadata);
        }

        public static void ShowSelectSavedGameUI(string uiTitle, uint maxDisplayedSavedGames, bool showCreateSaveUI, bool showDeleteSaveUI, Action<SavedGame, SelectUIStatus> callback)
        {
            Util.NullArgumentTest(uiTitle);
            Util.NullArgumentTest(callback);
            PlayGamesPlatform.Instance.SavedGame.ShowSelectSavedGameUI(uiTitle,
                maxDisplayedSavedGames,
                showCreateSaveUI,
                showDeleteSaveUI,
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
#endif
    }
}