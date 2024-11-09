/***
 * Author RaBear - HNB - 2019
 **/

#pragma warning disable 0649
using Cysharp.Threading.Tasks;
#if UNITY_ANDROID && IN_APP_UPDATE
using Google.Play.AppUpdate;
#endif
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif
#if UNITY_ANDROID && GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
#if UNITY_ANDROID && IN_APP_REVIEW
using Google.Play.Review;
#endif

namespace RCore.Service
{
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
	
	public static class GameServices
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

		private static bool IsLoadingScore;
		private static readonly List<LoadScoreRequest> LoadScoreRequests = new List<LoadScoreRequest>();

		public static void Init(Action<bool> pOnAuthenticated)
		{
			if (IsInitialized())
			{
				pOnAuthenticated?.Invoke(true);
				return;
			}

			m_OnUserLoginSucceeded = pOnAuthenticated;
#if UNITY_ANDROID && GPGS
			PlayGamesPlatform.Activate();
			Social.localUser.Authenticate(ProcessAuthentication);
#elif UNITY_IOS
			Social.localUser.Authenticate(ProcessAuthentication);
#else
			Debug.LogError("Failed to initialize Game Services");
			pOnAuthenticated?.Invoke(false);
#endif
		}

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

		public static void ShowLeaderboardUI(string leaderboardId, TimeScope timeScope = TimeScope.AllTime)
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
				callback?.Invoke(Array.Empty<IUserProfile>());
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
					callback?.Invoke(success ? Social.localUser.friends : Array.Empty<IUserProfile>());
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
				callback?.Invoke(Array.Empty<IUserProfile>());
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
				callback?.Invoke(leaderboardId, Array.Empty<IScore>());
				return;
			}

			// Create new request
			var request = new LoadScoreRequest
			{
				leaderboardId = leaderboardId,
				callback = callback,
				useLeaderboardDefault = true,
				loadLocalUserScore = false
			};

			// Add request to the queue
			LoadScoreRequests.Add(request);

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
				callback?.Invoke(leaderboardId, Array.Empty<IScore>());
				return;
			}

			// Create new request
			var request = new LoadScoreRequest
			{
				leaderboardId = leaderboardId,
				callback = callback,
				useLeaderboardDefault = false,
				loadLocalUserScore = false,
				fromRank = fromRank,
				scoreCount = scoreCount,
				timeScope = timeScope,
				userScope = userScope
			};

			// Add request to the queue
			LoadScoreRequests.Add(request);

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
			var request = new LoadScoreRequest
			{
				leaderboardId = leaderboardId,
				callback = delegate(string ldbName, IScore[] scores)
				{
					callback?.Invoke(ldbName, scores?[0]);
				},
				useLeaderboardDefault = false,
				loadLocalUserScore = true,
				fromRank = -1,
				scoreCount = -1,
				timeScope = TimeScope.AllTime,
				userScope = UserScope.Global
			};

			// Add request to the queue
			LoadScoreRequests.Add(request);

			DoNextLoadScoreRequest();
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

		public static void DoReportScore(long score, string leaderboardId, Action<bool> callback)
		{
            try
            {
                if (!IsInitialized())
                {
                    Debug.Log($"Failed to report score to leaderboard {leaderboardId}: user is not logged in.");
                    callback?.Invoke(false);
                    return;
                }
                Social.ReportScore(score, leaderboardId, callback);
            }
            catch
            {
                callback?.Invoke(false);
            }
        }

		// Progress of 0.0% means reveal the achievement.
		// Progress of 100.0% means unlock the achievement.
		public static void DoReportAchievementProgress(string achievementId, double progress, Action<bool> callback)
		{
            try
            {
                if (!IsInitialized())
                {
                    Debug.Log($"Failed to report progress for achievement {achievementId}: user is not logged in.");
                    callback?.Invoke(false);
                    return;
                }
                Social.ReportProgress(achievementId, progress, callback);
            }
            catch
            {
                callback?.Invoke(false);
            }
        }

		private static void DoNextLoadScoreRequest()
		{
			if (IsLoadingScore)
				return;

			if (LoadScoreRequests.Count == 0)
				return;

			IsLoadingScore = true;
			var request = LoadScoreRequests[0]; // fetch the next request
			LoadScoreRequests.RemoveAt(0); // then remove it from the queue

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
					IsLoadingScore = false;
					DoNextLoadScoreRequest();
				});
#elif UNITY_IOS
                // On iOS, we use LoadScores from ILeaderboard with default parameters.
                ldb.LoadScores((bool success) =>
                    {
						request.callback?.Invoke(request.leaderboardId, ldb.scores);

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
					IsLoadingScore = false;
					DoNextLoadScoreRequest();
				});
			}
		}

#if UNITY_ANDROID && GPGS
		// This function gets called when Authenticate completes
		// Note that if the operation is successful, Social.localUser will contain data from the server.
		private static void ProcessAuthentication(bool pSuccess)
		{
			Debug.Log($"Init GameServices {pSuccess}");

			m_OnUserLoginSucceeded?.Invoke(pSuccess);
		}

		private static void ProcessLoadedAchievements(IAchievement[] achievements)
		{
			if (achievements.Length == 0)
			{
				Debug.Log("No achievements found.");
			}
			else
			{
				Debug.Log("Got " + achievements.Length + " achievements.");
			}
		}
		
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

		public static async void ShowInAppUpdate(bool forceUpdate)
		{
#if UNITY_EDITOR
			return;
#endif
#if UNITY_ANDROID && IN_APP_UPDATE
			var updateManager = new AppUpdateManager();
			var updateInfoOp = updateManager.GetAppUpdateInfo();
			await updateInfoOp;
			if (!updateInfoOp.IsSuccessful)
				return;

			var result = updateInfoOp.GetResult();
			if (result.UpdateAvailability != UpdateAvailability.UpdateAvailable)
				return;

			//If after syncing remote data, user has converted to new map, but the game is still on old map, then force update
			//This case happens when user plays on multi devices, one have new version and the other have old version
			var updateOption = AppUpdateOptions.ImmediateAppUpdateOptions();
            
			if (forceUpdate)
				updateOption = AppUpdateOptions.ImmediateAppUpdateOptions();

			var startUpdateRequest = updateManager.StartUpdate(result, updateOption);
			await startUpdateRequest;
			updateManager.CompleteUpdate();
#endif
		}

        public static async void ShowInAppReview()
        {
#if UNITY_EDITOR
	        Application.OpenURL("market://details?id=" + Application.identifier);
	        return;
#endif
#if UNITY_ANDROID && IN_APP_REVIEW
	        var reviewManager = new ReviewManager();
	        var requestFlowOperation = reviewManager.RequestReviewFlow();
	        await requestFlowOperation;
	        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
	        {
		        Application.OpenURL("market://details?id=" + Application.identifier);
		        UnityEngine.Debug.LogError(requestFlowOperation.Error.ToString());
	        }
	        else
	        {
		        var playReviewInfo = requestFlowOperation.GetResult();
		        var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
		        await launchFlowOperation;
		        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
		        {
			        Application.OpenURL("market://details?id=" + Application.identifier);
			        UnityEngine.Debug.LogError(launchFlowOperation.Error.ToString());
		        }
	        }
#endif
        }
	}
}