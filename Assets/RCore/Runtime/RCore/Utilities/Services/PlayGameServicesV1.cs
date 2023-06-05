/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

#pragma warning disable 0649
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;
using RCore.Common;
using Debug = RCore.Common.Debug;

#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

#if UNITY_ANDROID && GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GPGSSavedGame = GooglePlayGames.BasicApi.SavedGame;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Service
{
#if UNITY_ANDROID && GPGS
	public enum AskFriendResolutionStatus
	{
		Valid = 1,
		InternalError = -2,
		NotAuthorized = -3,
		VersionUpdateRequired = -4,
		Timeout = -5,
		UserClosedUI = -6,
		UiBusy = -12,
		NetworkError = -20,
		NotInitialized = -30,
	}
#endif

	public static class PlayGameServicesV1
	{
		private static Action<bool> m_OnUserLoginSucceeded;

		public static ILocalUser LocalUser => IsInitialized() ? Social.localUser : null;

		public static string UserId => IsInitialized() ? Social.localUser.id : "";

		private struct LoadScoreRequest
		{
			public bool useLeaderboardDefault;
			public bool loadLocalUserScore;
			public string leaderboardId;
			public int fromRank;
			public int scoreCount;
			public TimeScope timeScope;
			public UserScope userScope;
			public Action<string, IScore[]> callback;
		}

		private static bool isLoadingScore;
		private static readonly List<LoadScoreRequest> loadScoreRequests = new List<LoadScoreRequest>();

		public static void Init(Action<bool> pOnAuthenticated)
		{
			if (IsInitialized())
				return;

			// Authenticate and register a ProcessAuthentication callback
			// This call needs to be made before we can proceed to other calls in the Social API
			m_OnUserLoginSucceeded = pOnAuthenticated;
#if UNITY_IOS
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
            Social.localUser.Authenticate(ProcessAuthentication);
#elif UNITY_ANDROID && GPGS
			var gpgsConfigBuilder = new PlayGamesClientConfiguration.Builder();

			// Enable Saved Games.
			gpgsConfigBuilder.EnableSavedGames();

			// Build the config
			var gpgsConfig = gpgsConfigBuilder.Build();

			// Initialize PlayGamesPlatform
			PlayGamesPlatform.InitializeInstance(gpgsConfig);

			// Set PlayGamesPlatforms as active
			if (Social.Active != PlayGamesPlatform.Instance)
				PlayGamesPlatform.Activate();
			
			// Now authenticate
			Social.localUser.Authenticate(ProcessAuthentication);
#elif UNITY_ANDROID && !GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
#else
            Debug.Log("Failed to initialize Game Services module: platform not supported.");
#endif
		}

		/// <summary>
		/// Determines whether this module is initialized (user is authenticated) and ready to use.
		/// </summary>
		/// <returns><c>true</c> if initialized; otherwise, <c>false</c>.</returns>
		public static bool IsInitialized()
		{
#if UNITY_EDITOR
			return false;
#endif
			return Social.localUser.authenticated;
		}

		/// <summary>
		/// Shows the leaderboard UI.
		/// </summary>
		public static void ShowLeaderboardUI()
		{
			if (IsInitialized())
				Social.ShowLeaderboardUI();
			else
				Debug.Log("Couldn't show leaderboard UI: user is not logged in.");
		}

		/// <summary>
		/// Shows the leaderboard UI for the given leaderboard.
		/// </summary>
		/// <param name="leaderboardId">Leaderboard name.</param>
		public static void ShowLeaderboardUI(string leaderboardId)
		{
			ShowLeaderboardUI(leaderboardId, TimeScope.AllTime);
		}

		/// <summary>
		/// Shows the leaderboard UI for the given leaderboard in the specified time scope.
		/// </summary>
		/// <param name="leaderboardid">Leaderboard name.</param>
		/// <param name="timeScope">Time scope to display scores in the leaderboard.</param>
		public static void ShowLeaderboardUI(string leaderboardId, TimeScope timeScope)
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't show leaderboard UI: user is not logged in.");
				return;
			}

#if UNITY_IOS
            GameCenterPlatform.ShowLeaderboardUI(leaderboardId, timeScope);
#elif UNITY_ANDROID && GPGS
			PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId, ToGpgsLeaderboardTimeSpan(timeScope), null);
#else
            // Fallback
            Social.ShowLeaderboardUI();
#endif
		}

		/// <summary>
		/// Shows the achievements UI.
		/// </summary>
		public static void ShowAchievementsUI()
		{
			if (IsInitialized())
				Social.ShowAchievementsUI();
			else
				Debug.Log("Couldn't show achievements UI: user is not logged in.");
		}

		/// <summary>
		/// Reports the score to the leaderboard with the given name.
		/// </summary>
		/// <param name="score">Score.</param>
		/// <param name="leaderboardId">Leaderboard name.</param>
		/// <param name="callback">Callback receives a <c>true</c> value if the score is reported successfully, otherwise it receives <c>false</c>.</param>
		public static void ReportScore(long score, string leaderboardId, Action<bool> callback = null)
		{
			DoReportScore(score, leaderboardId, callback);
		}

		/// <summary>
		/// Reveals the hidden achievement with the specified name.
		/// </summary>
		/// <param name="achievementId">Achievement name.</param>
		/// <param name="callback">Callback receives a <c>true</c> value if the achievement is revealed successfully, otherwise it receives <c>false</c>.</param>
		public static void RevealAchievement(string achievementId, Action<bool> callback = null)
		{
			DoReportAchievementProgress(achievementId, 0.0f, callback);
		}

		/// <summary>
		/// Unlocks the achievement with the specified name.
		/// </summary>
		/// <param name="achievementId">Achievement name.</param>
		/// <param name="callback">Callback receives a <c>true</c> value if the achievement is unlocked successfully, otherwise it receives <c>false</c>.</param>
		public static void UnlockAchievement(string achievementId, Action<bool> callback = null)
		{
			DoReportAchievementProgress(achievementId, 100.0f, callback);
		}

		/// <summary>
		/// Reports the progress of the incremental achievement with the specified name.
		/// </summary>
		/// <param name="achievementId">Achievement name.</param>
		/// <param name="progress">Progress.</param>
		/// <param name="callback">Callback receives a <c>true</c> value if the achievement progress is reported successfully, otherwise it receives <c>false</c>.</param>
		public static void ReportAchievementProgress(string achievementId, double progress, Action<bool> callback = null)
		{
			DoReportAchievementProgress(achievementId, progress, callback);
		}

		/// <summary>
		/// Loads all friends of the authenticated user.
		/// Internally it will populate the LocalUsers.friends array and invoke the 
		/// callback with this array if the loading succeeded. 
		/// If the loading failed, the callback will be invoked with an empty array.
		/// If the LocalUsers.friends array is already populated then the callback will be invoked immediately
		/// without any loading request being made. 
		/// </summary>
		/// <param name="callback">Callback.</param>
		public static void LoadFriends(Action<IUserProfile[]> callback)
		{
			if (!IsInitialized())
			{
				Debug.Log("Failed to load friends: user is not logged in.");
				callback?.Invoke(new IUserProfile[0]);
				return;
			}

			if (Social.localUser.friends != null && Social.localUser.friends.Length > 0)
			{
				callback?.Invoke(Social.localUser.friends);
			}
			else
			{
				Social.localUser.LoadFriends(success =>
				{
					if (success)
					{
						callback?.Invoke(Social.localUser.friends);
					}
					else
					{
						callback?.Invoke(new IUserProfile[0]);
					}
				});
			}
		}

		/// <summary>
		/// Loads the user profiles associated with the given array of user IDs.
		/// </summary>
		/// <param name="userIds">User identifiers.</param>
		/// <param name="callback">Callback.</param>
		public static void LoadUsers(string[] userIds, Action<IUserProfile[]> callback)
		{
			if (!IsInitialized())
			{
				Debug.Log("Failed to load users: user is not logged in.");
				callback?.Invoke(new IUserProfile[0]);
				return;
			}

			Social.LoadUsers(userIds, callback);
		}

		/// <summary>
		/// Loads a set of scores using the default parameters of the given leaderboard.
		/// This returns the 25 scores that are around the local player's score
		/// in the Global userScope and AllTime timeScope.
		/// Note that each load score request is added into a queue and the
		/// next request is called after the callback of previous request has been invoked.
		/// </summary>
		/// <param name="leaderboardId">Leaderboard name.</param>
		/// <param name="callback">Callback receives the leaderboard name and an array of loaded scores.</param>
		public static void LoadScores(string leaderboardId, Action<string, IScore[]> callback)
		{
			if (!IsInitialized())
			{
				Debug.Log($"Failed to load scores from leaderboard {leaderboardId}: user is not logged in.");
				callback?.Invoke(leaderboardId, new IScore[0]);
				return;
			}

			// Create new request
			var request = new LoadScoreRequest();
			request.leaderboardId = leaderboardId;
			request.callback = callback;
			request.useLeaderboardDefault = true;
			request.loadLocalUserScore = false;

			// Add request to the queue
			loadScoreRequests.Add(request);

			DoNextLoadScoreRequest();
		}

		/// <summary>
		/// Loads the set of scores from the specified leaderboard within the specified timeScope and userScope.
		/// The range is defined by starting position fromRank and the number of scores to retrieve scoreCount.
		/// Note that each load score request is added into a queue and the
		/// next request is called after the callback of previous request has been invoked.
		/// </summary>
		/// <param name="leaderboardId">Leaderboard name.</param>
		/// <param name="fromRank">The rank of the first score to load.</param>
		/// <param name="scoreCount">The total number of scores to load.</param>
		/// <param name="timeScope">Time scope.</param>
		/// <param name="userScope">User scope.</param>
		/// <param name="callback">Callback receives the leaderboard name and an array of loaded scores.</param>
		public static void LoadScores(string leaderboardId, int fromRank, int scoreCount, TimeScope timeScope, UserScope userScope, Action<string, IScore[]> callback)
		{
			// IMPORTANT: On Android, the fromRank argument is ignored and the score range always starts at 1.
			// (This is not the intended behavior according to the SocialPlatform.Range documentation, and may simply be
			// a bug of the current (0.9.34) GooglePlayPlatform implementation).
			if (!IsInitialized())
			{
				Debug.Log($"Failed to load scores from leaderboard {leaderboardId}: user is not logged in.");
				callback?.Invoke(leaderboardId, new IScore[0]);
				return;
			}

			// Create new request
			var request = new LoadScoreRequest();
			request.leaderboardId = leaderboardId;
			request.callback = callback;
			request.useLeaderboardDefault = false;
			request.loadLocalUserScore = false;
			request.fromRank = fromRank;
			request.scoreCount = scoreCount;
			request.timeScope = timeScope;
			request.userScope = userScope;

			// Add request to the queue
			loadScoreRequests.Add(request);

			DoNextLoadScoreRequest();
		}

		/// <summary>
		/// Loads the local user's score from the specified leaderboard.
		/// Note that each load score request is added into a queue and the
		/// next request is called after the callback of previous request has been invoked.
		/// </summary>
		/// <param name="leaderboardId">Leaderboard name.</param>
		/// <param name="callback">Callback receives the leaderboard name and the loaded score.</param>
		public static void LoadLocalUserScore(string leaderboardId, Action<string, IScore> callback)
		{
			if (!IsInitialized())
			{
				Debug.Log($"Failed to load local user's score from leaderboard {leaderboardId}: user is not logged in.");
				callback?.Invoke(leaderboardId, null);
				return;
			}

			// Create new request
			var request = new LoadScoreRequest();
			request.leaderboardId = leaderboardId;
			request.callback = delegate(string ldbName, IScore[] scores)
			{
				if (scores != null)
				{
					callback?.Invoke(ldbName, scores[0]);
				}
				else
				{
					callback?.Invoke(ldbName, null);
				}
			};

			request.useLeaderboardDefault = false;
			request.loadLocalUserScore = true;
			request.fromRank = -1;
			request.scoreCount = -1;
			request.timeScope = TimeScope.AllTime;
			request.userScope = UserScope.Global;

			// Add request to the queue
			loadScoreRequests.Add(request);

			DoNextLoadScoreRequest();
		}

		/// <summary>
		/// [Google Play Games] Gets the server auth code.
		/// </summary>
		/// <returns></returns>
		public static string GetServerAuthCode()
		{
			if (!IsInitialized())
			{
				return string.Empty;
			}

#if UNITY_ANDROID && GPGS
			return PlayGamesPlatform.Instance.GetServerAuthCode();
#elif UNITY_ANDROID && !GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
            return string.Empty;
#else
            Debug.Log("GetServerAuthCode is only available on Google Play Games platform.");
            return string.Empty;
#endif
		}

		/// <summary>
		/// [Google Play Games] Gets another server auth code.
		/// </summary>
		/// <param name="reAuthenticateIfNeeded"></param>
		/// <param name="callback"></param>
		public static void GetAnotherServerAuthCode(bool reAuthenticateIfNeeded, Action<string> callback)
		{
			if (!IsInitialized())
				return;

#if UNITY_ANDROID && GPGS
			PlayGamesPlatform.Instance.GetAnotherServerAuthCode(reAuthenticateIfNeeded, callback);
#elif UNITY_ANDROID && !GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
#else
            Debug.Log("GetAnotherServerAuthCode is only available on Google Play Games platform.");
#endif
		}

		/// <summary>
		/// [Google Play Games] Signs the user out.
		/// </summary>
		public static void SignOut()
		{
			if (!IsInitialized())
			{
				return;
			}

#if UNITY_ANDROID && GPGS
			PlayGamesPlatform.Instance.SignOut();
#elif UNITY_ANDROID && !GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
#else
            Debug.Log("Signing out from script is not available on this platform.");
#endif
		}

#if UNITY_ANDROID && GPGS
		public static void AskForLoadFriendsResolution(Action<AskFriendResolutionStatus> callback)
		{
			if (!IsInitialized())
			{
				callback(AskFriendResolutionStatus.NotInitialized);
			}

			if (!PlayGamesPlatform.Instance.IsAuthenticated())
			{
				callback(AskFriendResolutionStatus.NotInitialized);
			}

			PlayGamesPlatform.Instance.AskForLoadFriendsResolution(status =>
			{
				switch (status)
				{
					case UIStatus.Valid:
						callback(AskFriendResolutionStatus.Valid);
						break;
					case UIStatus.InternalError:
						callback(AskFriendResolutionStatus.InternalError);
						break;
					case UIStatus.NotAuthorized:
						callback(AskFriendResolutionStatus.NotAuthorized);
						break;
					case UIStatus.VersionUpdateRequired:
						callback(AskFriendResolutionStatus.VersionUpdateRequired);
						break;
					case UIStatus.Timeout:
						callback(AskFriendResolutionStatus.Timeout);
						break;
					case UIStatus.UserClosedUI:
						callback(AskFriendResolutionStatus.UserClosedUI);
						break;
					case UIStatus.UiBusy:
						callback(AskFriendResolutionStatus.UiBusy);
						break;
					case UIStatus.NetworkError:
						callback(AskFriendResolutionStatus.NetworkError);
						break;
				}
			});
		}
#endif

#region Private methods

		public static void DoReportScore(long score, string leaderboardId, Action<bool> callback)
		{
			if (!IsInitialized())
			{
				Debug.Log($"Failed to report score to leaderboard {leaderboardId}: user is not logged in.");
				callback?.Invoke(false);
				return;
			}

			Social.ReportScore(score, leaderboardId, callback);
		}

		// Progress of 0.0% means reveal the achievement.
		// Progress of 100.0% means unlock the achievement.
		public static void DoReportAchievementProgress(string achievementId, double progress, Action<bool> callback)
		{
			if (!IsInitialized())
			{
				Debug.Log($"Failed to report progress for achievement {achievementId}: user is not logged in.");
				callback?.Invoke(false);
				return;
			}

			Social.ReportProgress(achievementId, progress, callback);
		}

		static void DoNextLoadScoreRequest()
		{
			LoadScoreRequest request;

			if (isLoadingScore)
				return;

			if (loadScoreRequests.Count == 0)
				return;

			isLoadingScore = true;
			request = loadScoreRequests[0]; // fetch the next request
			loadScoreRequests.RemoveAt(0); // then remove it from the queue

			// Now create a new leaderboard and start loading scores
			var ldb = Social.CreateLeaderboard();
			ldb.id = request.leaderboardId;

			if (request.useLeaderboardDefault)
			{
				// The current iOS implementation of ISocialPlatform behaves weirdly with Social.LoadScores.
				// Experiment showed that only the first score on the leaderboard was returned.
				// On Android scores were returned properly.
				// We'll have different code for the two platforms in an attempt to provide consistent behavior from the outside.
#if UNITY_ANDROID
				// On Android, we'll use LoadScores directly from Social.
				Social.LoadScores(ldb.id, scores =>
				{
					request.callback?.Invoke(request.leaderboardId, scores);

					// Load next request
					isLoadingScore = false;
					DoNextLoadScoreRequest();
				});
#elif UNITY_IOS
                // On iOS, we use LoadScores from ILeaderboard with default parameters.
                ldb.LoadScores((bool success) =>
                    {
                        if (request.callback != null)
                            request.callback(request.leaderboardId, ldb.scores);

                        // Load next request
                        isLoadingScore = false;
                        DoNextLoadScoreRequest();
                    });
#endif
			}
			else
			{
				ldb.timeScope = request.timeScope;
				ldb.userScope = request.userScope;

				if (request.fromRank > 0 && request.scoreCount > 0)
				{
					ldb.range = new UnityEngine.SocialPlatforms.Range(request.fromRank, request.scoreCount);
				}

				ldb.LoadScores(success =>
				{
					if (request.loadLocalUserScore)
					{
						var returnScores = new[] { ldb.localUserScore };

						request.callback?.Invoke(request.leaderboardId, returnScores);
					}
					else
					{
						request.callback?.Invoke(request.leaderboardId, ldb.scores);
					}

					// Load next request
					isLoadingScore = false;
					DoNextLoadScoreRequest();
				});
			}
		}

#endregion

#region Authentication listeners

		// This function gets called when Authenticate completes
		// Note that if the operation is successful, Social.localUser will contain data from the server.
		static void ProcessAuthentication(bool success)
		{
			Debug.Log($"Init GameServices {success}");

			m_OnUserLoginSucceeded?.Invoke(success);
		}

		static void ProcessLoadedAchievements(IAchievement[] achievements)
		{
			Debug.Log(achievements.Length == 0 ? "No achievements found." : $"Got {achievements.Length} achievements.");
		}

#endregion

#region Helpers

#if UNITY_ANDROID && GPGS
		private static LeaderboardTimeSpan ToGpgsLeaderboardTimeSpan(TimeScope timeScope)
		{
			return timeScope switch
			{
				TimeScope.AllTime => LeaderboardTimeSpan.AllTime,
				TimeScope.Week => LeaderboardTimeSpan.Weekly,
				TimeScope.Today => LeaderboardTimeSpan.Daily,
				_ => LeaderboardTimeSpan.AllTime
			};
		}
#endif

#endregion

#if UNITY_EDITOR

#region Editor

		[CustomEditor(typeof(PlayGameServicesV1))]
		public class GameServicesEditor : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				GUILayout.BeginVertical("box");
#if GPGS
				GUI.backgroundColor = Color.red;
				if (GUILayout.Button("Disable Game Services"))
					EditorHelper.RemoveDirective("GPGS");
#else
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Enable Game Services"))
                    EditorHelper.AddDirective("GPGS");
#endif
				GUILayout.EndVertical();
			}
		}

#endregion

#endif
	}
}