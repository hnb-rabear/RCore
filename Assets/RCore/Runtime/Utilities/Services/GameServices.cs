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
using GooglePlayGames.BasicApi.SavedGame;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Service
{
#if UNITY_ANDROID && GPGS
    /// <summary> Native response status codes for GPGS UI operations.</summary>
    /// </remarks>
    public enum AskFriendResolutionStatus
    {
        /// <summary>The result is valid.</summary>
        Valid = 1,

        /// <summary>An internal error occurred.</summary>
        InternalError = -2,

        /// <summary>The player is not authorized to perform the operation.</summary>
        NotAuthorized = -3,

        /// <summary>The installed version of Google Play services is out of date.</summary>
        VersionUpdateRequired = -4,

        /// <summary>Timed out while awaiting the result.</summary>
        Timeout = -5,

        /// <summary>UI closed by user.</summary>
        UserClosedUI = -6,
        UiBusy = -12,

        /// <summary>An network error occurred.</summary>
        NetworkError = -20,

        /// <sumary>The service haven't been fully initialized.</sumary>
        NotInitialized = -30,
    }
#endif

    [Serializable]
    public class GameServicesItem
    {
        public string Name;
        public string IOSId;
        public string AndroidId;
        public string Id
        {
            get
            {
#if UNITY_IOS
                return IOSId;
#elif UNITY_ANDROID
                return AndroidId;
#else
                return null;
#endif
            }
        }
    }

    public class GameServices : MonoBehaviour
    {
        [Serializable]
        public class Settings
        {
            /// <summary>
            /// Gets or sets the GPGS popup gravity.
            /// </summary>
            /// <value>The gpgs popup gravity.</value>
            public PopupGravity popupGravity;
            /// <summary>
            /// [Google Play Games] Whether to request a server authentication code during initialization.
            /// </summary>
            public bool shouldRequestServerAuthCode;
            /// <summary>
            /// [Google Play Games] Whether to force refresh while requesting a server authentication code during initialization.
            /// </summary>
            public bool forceRefreshServerAuthCode;
            /// <summary>
            /// [Google Play Games] The OAuth scopes to be added during initialization.
            /// </summary>
            public string[] oauthScopes;
            public GameServicesItem[] Leaderboards;
            public GameServicesItem[] Achievements;

            public enum PopupGravity
            {
                Top,
                Bottom,
                Left,
                Right,
                CenterHorizontal
            }
        }

        private static GameServices m_Instance;
        public static GameServices Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = FindObjectOfType<GameServices>();
                return m_Instance;
            }
        }

        /// <summary>
        /// Occurs when user login succeeded or failed.
        /// </summary>
        private Action<bool> m_OnUserLoginSucceeded;

        /// <summary>
        /// The local or currently logged in user.
        /// Returns null if the user has not logged in.
        /// </summary>
        /// <value>The local user.</value>
        public ILocalUser LocalUser
        {
            get
            {
                if (IsInitialized())
                    return Social.localUser;
                else
                    return null;
            }
        }

        struct LoadScoreRequest
        {
            public bool useLeaderboardDefault;
            public bool loadLocalUserScore;
            public string leaderboardName;
            public string leaderboardId;
            public int fromRank;
            public int scoreCount;
            public TimeScope timeScope;
            public UserScope userScope;
            public Action<string, IScore[]> callback;
        }

        private bool isLoadingScore;
        private List<LoadScoreRequest> loadScoreRequests = new List<LoadScoreRequest>();

        public Settings settings;

        private void Awake()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
        }

        /// <summary>
        /// Initializes the service. This is required before any other actions can be done e.g reporting scores.
        /// During the initialization process, a login popup will show up if the user hasn't logged in, otherwise
        /// the process will carry on silently.
        /// Note that on iOS, the login popup will show up automatically when the app gets focus for the first 3 times
        /// while subsequent authentication calls will be ignored.
        /// </summary>
        public void Init(Action<bool> pOnAuthenticated)
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

            // Add the OAuth scopes if any.
            if (settings.oauthScopes != null && settings.oauthScopes.Length > 0)
            {
                foreach (string scope in settings.oauthScopes)
                    gpgsConfigBuilder.AddOauthScope(scope);
            }

            // Request ServerAuthCode if needed.
            if (settings.shouldRequestServerAuthCode)
                gpgsConfigBuilder.RequestServerAuthCode(settings.forceRefreshServerAuthCode);

            // Build the config
            PlayGamesClientConfiguration gpgsConfig = gpgsConfigBuilder.Build();

            // Initialize PlayGamesPlatform
            PlayGamesPlatform.InitializeInstance(gpgsConfig);

            // Set PlayGamesPlatforms as active
            if (Social.Active != PlayGamesPlatform.Instance)
                PlayGamesPlatform.Activate();

            // Now authenticate
            Social.localUser.Authenticate(ProcessAuthentication);
#elif UNITY_ANDROID && !GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
            pOnAuthenticated?.Invoke(false);
#else
			Debug.Log("Failed to initialize Game Services module: platform not supported.");
			pOnAuthenticated?.Invoke(false);
#endif
        }

        /// <summary>
        /// Determines whether this module is initialized (user is authenticated) and ready to use.
        /// </summary>
        /// <returns><c>true</c> if initialized; otherwise, <c>false</c>.</returns>
        public bool IsInitialized()
        {
            return Social.localUser.authenticated;
        }

        /// <summary>
        /// Shows the leaderboard UI.
        /// </summary>
        public void ShowLeaderboardUI()
        {
            if (IsInitialized())
                Social.ShowLeaderboardUI();
            else
            {
                Debug.Log("Couldn't show leaderboard UI: user is not logged in.");
            }
        }

        /// <summary>
        /// Shows the leaderboard UI for the given leaderboard.
        /// </summary>
        /// <param name="leaderboardName">Leaderboard name.</param>
        public void ShowLeaderboardUI(string leaderboardName)
        {
            ShowLeaderboardUI(leaderboardName, TimeScope.AllTime);
        }

        /// <summary>
        /// Shows the leaderboard UI for the given leaderboard in the specified time scope.
        /// </summary>
        /// <param name="leaderboardName">Leaderboard name.</param>
        /// <param name="timeScope">Time scope to display scores in the leaderboard.</param>
        public void ShowLeaderboardUI(string leaderboardName, TimeScope timeScope)
        {
            if (!IsInitialized())
            {
                Debug.Log("Couldn't show leaderboard UI: user is not logged in.");
                return;
            }

            GameServicesItem ldb = GetLeaderboardByName(leaderboardName);

            if (ldb == null)
            {
                Debug.Log("Couldn't show leaderboard UI: unknown leaderboard name.");
                return;
            }

#if UNITY_IOS
            GameCenterPlatform.ShowLeaderboardUI(ldb.Id, timeScope);
#elif UNITY_ANDROID && GPGS
            PlayGamesPlatform.Instance.ShowLeaderboardUI(ldb.Id, ToGpgsLeaderboardTimeSpan(timeScope), null);
#else
            // Fallback
            Social.ShowLeaderboardUI();
#endif
        }

        /// <summary>
        /// Shows the achievements UI.
        /// </summary>
        public void ShowAchievementsUI()
        {
            if (IsInitialized())
                Social.ShowAchievementsUI();
            else
            {
                Debug.Log("Couldn't show achievements UI: user is not logged in.");
            }
        }

        /// <summary>
        /// Reports the score to the leaderboard with the given name.
        /// </summary>
        /// <param name="score">Score.</param>
        /// <param name="leaderboardName">Leaderboard name.</param>
        /// <param name="callback">Callback receives a <c>true</c> value if the score is reported successfully, otherwise it receives <c>false</c>.</param>
        public void ReportScore(long score, string leaderboardName, Action<bool> callback = null)
        {
            GameServicesItem ldb = GetLeaderboardByName(leaderboardName);

            if (ldb != null)
            {
                DoReportScore(score, ldb.Id, callback);
            }
            else
            {
                Debug.Log("Failed to report score: unknown leaderboard name.");
            }
        }

        /// <summary>
        /// Reveals the hidden achievement with the specified name.
        /// </summary>
        /// <param name="achievementName">Achievement name.</param>
        /// <param name="callback">Callback receives a <c>true</c> value if the achievement is revealed successfully, otherwise it receives <c>false</c>.</param>
        public void RevealAchievement(string achievementName, Action<bool> callback = null)
        {
            GameServicesItem acm = GetAchievementByName(achievementName);

            if (acm != null)
            {
                DoReportAchievementProgress(acm.Id, 0.0f, callback);
            }
            else
            {
                Debug.Log("Failed to reveal achievement: unknown achievement name.");
            }
        }

        /// <summary>
        /// Unlocks the achievement with the specified name.
        /// </summary>
        /// <param name="achievementName">Achievement name.</param>
        /// <param name="callback">Callback receives a <c>true</c> value if the achievement is unlocked successfully, otherwise it receives <c>false</c>.</param>
        public void UnlockAchievement(string achievementName, Action<bool> callback = null)
        {
            GameServicesItem acm = GetAchievementByName(achievementName);

            if (acm != null)
            {
                DoReportAchievementProgress(acm.Id, 100.0f, callback);
            }
            else
            {
                Debug.Log("Failed to unlocked achievement: unknown achievement name.");
            }
        }

        /// <summary>
        /// Reports the progress of the incremental achievement with the specified name.
        /// </summary>
        /// <param name="achievementName">Achievement name.</param>
        /// <param name="progress">Progress.</param>
        /// <param name="callback">Callback receives a <c>true</c> value if the achievement progress is reported successfully, otherwise it receives <c>false</c>.</param>
        public void ReportAchievementProgress(string achievementName, double progress, Action<bool> callback = null)
        {
            GameServicesItem acm = GetAchievementByName(achievementName);

            if (acm != null)
            {
                DoReportAchievementProgress(acm.Id, progress, callback);
            }
            else
            {
                Debug.Log("Failed to report incremental achievement progress: unknown achievement name.");
            }
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
        public void LoadFriends(Action<IUserProfile[]> callback)
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
					callback?.Invoke(success ? Social.localUser.friends : Array.Empty<IUserProfile>());
				});
            }
        }

        /// <summary>
        /// Loads the user profiles associated with the given array of user IDs.
        /// </summary>
        /// <param name="userIds">User identifiers.</param>
        /// <param name="callback">Callback.</param>
        public void LoadUsers(string[] userIds, Action<IUserProfile[]> callback)
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
        /// <param name="leaderboardName">Leaderboard name.</param>
        /// <param name="callback">Callback receives the leaderboard name and an array of loaded scores.</param>
        public void LoadScores(string leaderboardName, Action<string, IScore[]> callback)
        {
            if (!IsInitialized())
            {
                Debug.Log($"Failed to load scores from leaderboard {leaderboardName}: user is not logged in.");
                callback?.Invoke(leaderboardName, new IScore[0]);
                return;
            }

            GameServicesItem ldb = GetLeaderboardByName(leaderboardName);

            if (ldb == null)
            {
                Debug.Log($"Failed to load scores: unknown leaderboard name {leaderboardName}");
                callback?.Invoke(leaderboardName, new IScore[0]);
                return;
            }

            // Create new request
            LoadScoreRequest request = new LoadScoreRequest();
            request.leaderboardName = ldb.Name;
            request.leaderboardId = ldb.Id;
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
        /// <param name="leaderboardName">Leaderboard name.</param>
        /// <param name="fromRank">The rank of the first score to load.</param>
        /// <param name="scoreCount">The total number of scores to load.</param>
        /// <param name="timeScope">Time scope.</param>
        /// <param name="userScope">User scope.</param>
        /// <param name="callback">Callback receives the leaderboard name and an array of loaded scores.</param>
        public void LoadScores(string leaderboardName, int fromRank, int scoreCount, TimeScope timeScope, UserScope userScope, Action<string, IScore[]> callback)
        {
            // IMPORTANT: On Android, the fromRank argument is ignored and the score range always starts at 1.
            // (This is not the intended behavior according to the SocialPlatform.Range documentation, and may simply be
            // a bug of the current (0.9.34) GooglePlayPlatform implementation).
            if (!IsInitialized())
            {
                Debug.Log($"Failed to load scores from leaderboard {leaderboardName}: user is not logged in.");
                callback?.Invoke(leaderboardName, new IScore[0]);
                return;
            }

            GameServicesItem ldb = GetLeaderboardByName(leaderboardName);

            if (ldb == null)
            {
                Debug.Log($"Failed to load scores: unknown leaderboard name {leaderboardName}.");
                callback?.Invoke(leaderboardName, new IScore[0]);
                return;
            }

            // Create new request
            LoadScoreRequest request = new LoadScoreRequest();
            request.leaderboardName = ldb.Name;
            request.leaderboardId = ldb.Id;
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
        /// <param name="leaderboardName">Leaderboard name.</param>
        /// <param name="callback">Callback receives the leaderboard name and the loaded score.</param>
        public void LoadLocalUserScore(string leaderboardName, Action<string, IScore> callback)
        {
            if (!IsInitialized())
            {
                Debug.Log($"Failed to load local user's score from leaderboard {leaderboardName}: user is not logged in.");
                callback?.Invoke(leaderboardName, null);
                return;
            }

            GameServicesItem ldb = GetLeaderboardByName(leaderboardName);

            if (ldb == null)
            {
                Debug.Log($"Failed to load local user's score: unknown leaderboard name {leaderboardName}.");
                callback?.Invoke(leaderboardName, null);
                return;
            }

            // Create new request
            LoadScoreRequest request = new LoadScoreRequest();
            request.leaderboardName = ldb.Name;
            request.leaderboardId = ldb.Id;
            request.callback = delegate (string ldbName, IScore[] scores)
			{
				callback?.Invoke(ldbName, scores?[0]);
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
        /// Returns a leaderboard it one with a leaderboardName was declared before within leaderboards array.
        /// </summary>
        /// <returns>The leaderboard by name.</returns>
        /// <param name="leaderboardName">Leaderboard name.</param>
        public GameServicesItem GetLeaderboardByName(string leaderboardName)
        {
            foreach (GameServicesItem ldb in settings.Leaderboards)
            {
                if (ldb.Name.Equals(leaderboardName))
                    return ldb;
            }

            return null;
        }

        /// <summary>
        /// Returns an achievement it one with an achievementName was declared before within achievements array.
        /// </summary>
        /// <returns>The achievement by name.</returns>
        /// <param name="achievementName">Achievement name.</param>
        public GameServicesItem GetAchievementByName(string achievementName)
        {
            foreach (GameServicesItem acm in settings.Achievements)
            {
                if (acm.Name.Equals(achievementName))
                    return acm;
            }

            return null;
        }

        /// <summary>
        /// [Google Play Games] Gets the server auth code.
        /// </summary>
        /// <returns></returns>
        public string GetServerAuthCode()
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
        public void GetAnotherServerAuthCode(bool reAuthenticateIfNeeded, Action<string> callback)
        {
            if (!IsInitialized())
            {
                return;
            }

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
        public void SignOut()
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
        public void AskForLoadFriendsResolution(Action<AskFriendResolutionStatus> callback)
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
                };
            });
        }
#endif

        #region Private methods

        void DoReportScore(long score, string leaderboardId, Action<bool> callback)
        {
            if (!IsInitialized())
            {
                Debug.Log($"Failed to report score to leaderboard {leaderboardId}: user is not logged in.");
                callback?.Invoke(false);
                return;
            }

            Social.ReportScore(
                score,
                leaderboardId,
                success =>
                {
                    callback?.Invoke(success);
                }
            );
        }

        // Progress of 0.0% means reveal the achievement.
        // Progress of 100.0% means unlock the achievement.
        void DoReportAchievementProgress(string achievementId, double progress, Action<bool> callback)
        {
            if (!IsInitialized())
            {
                Debug.Log($"Failed to report progress for achievement {achievementId}: user is not logged in.");
                callback?.Invoke(false);
                return;
            }

            Social.ReportProgress(
                achievementId,
                progress,
                success =>
                {
                    callback?.Invoke(success);
                }
            );
        }

        void DoNextLoadScoreRequest()
        {
            LoadScoreRequest request;

            if (isLoadingScore)
                return;

            if (loadScoreRequests.Count == 0)
                return;

            isLoadingScore = true;
            request = loadScoreRequests[0]; // fetch the next request
            loadScoreRequests.RemoveAt(0);  // then remove it from the queue

            // Now create a new leaderboard and start loading scores
            ILeaderboard ldb = Social.CreateLeaderboard();
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
                    request.callback?.Invoke(request.leaderboardName, scores);

                    // Load next request
                    isLoadingScore = false;
                    DoNextLoadScoreRequest();
                });
#elif UNITY_IOS
                // On iOS, we use LoadScores from ILeaderboard with default parameters.
                ldb.LoadScores((bool success) =>
                    {
                        if (request.callback != null)
                            request.callback(request.leaderboardName, ldb.scores);

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

                ldb.LoadScores(_ =>
                {
                    if (request.loadLocalUserScore)
                    {
                        IScore[] returnScores = { ldb.localUserScore };

                        request.callback?.Invoke(request.leaderboardName, returnScores);
                    }
                    else
                    {
                        request.callback?.Invoke(request.leaderboardName, ldb.scores);
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
        void ProcessAuthentication(bool success)
        {
            Debug.Log($"Init GameServices {success}");

            if (success)
            {
                // Set GPGS popup gravity, this needs to be done after authentication.
#if UNITY_ANDROID && GPGS
                PlayGamesPlatform.Instance.SetGravityForPopups(ToGpgsGravity(settings.popupGravity));
#endif
            }

            m_OnUserLoginSucceeded?.Invoke(success);
        }

        void ProcessLoadedAchievements(IAchievement[] achievements)
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

        #endregion

        #region Helpers

#if UNITY_ANDROID && GPGS
        private Gravity ToGpgsGravity(Settings.PopupGravity gravity)
        {
            switch (gravity)
            {
                case Settings.PopupGravity.Top:
                    return Gravity.TOP;
                case Settings.PopupGravity.Bottom:
                    return Gravity.BOTTOM;
                case Settings.PopupGravity.Left:
                    return Gravity.LEFT;
                case Settings.PopupGravity.Right:
                    return Gravity.RIGHT;
                case Settings.PopupGravity.CenterHorizontal:
                    return Gravity.CENTER_HORIZONTAL;
                default:
                    return Gravity.TOP;
            }
        }

        private LeaderboardTimeSpan ToGpgsLeaderboardTimeSpan(TimeScope timeScope)
        {
            switch (timeScope)
            {
                case TimeScope.AllTime:
                    return LeaderboardTimeSpan.AllTime;
                case TimeScope.Week:
                    return LeaderboardTimeSpan.Weekly;
                case TimeScope.Today:
                    return LeaderboardTimeSpan.Daily;
                default:
                    return LeaderboardTimeSpan.AllTime;
            }
        }
#endif

        #endregion

#if UNITY_EDITOR

        #region Editor

        [CustomEditor(typeof(GameServices))]
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
