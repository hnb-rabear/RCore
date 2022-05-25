/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

#pragma warning disable 0649

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using RCore.Common;
using Debug = UnityEngine.Debug;

#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

#if UNITY_ANDROID && ACTIVE_GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GPGSSavedGame = GooglePlayGames.BasicApi.SavedGame;
using GooglePlayGames.BasicApi.Multiplayer;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Service.GPGS
{
    public partial class GameServices : MonoBehaviour
    {
        #region Internal Class

        public enum LeaderboardTimeSpan
        {
            Daily = 1,
            Weekly = 2,
            AllTime = 3,
        }

        public enum Gravity
        {
            Top = 48,
            Bottom = 80,
            Left = 3,
            Rgith = 5,
            CenterHorizontal = 1
        }

        struct LoadScoreRequest
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

        #endregion

        //=======================================================

        #region Members

        private static GameServices mIntance;
        public static GameServices Instance
        {
            get
            {
                if (mIntance == null)
                {
                    mIntance = FindObjectOfType<GameServices>();
                    if (mIntance == null)
                    {
                        var obj = new GameObject("GameServices");
                        mIntance = obj.AddComponent<GameServices>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return mIntance;
            }
        }

        public static event Action onUserLoginSucceeded;
        public static event Action onUserLoginFailed;
        /// <summary>
        /// The local or currently logged in user.
        /// Returns null if the user has not logged in.
        /// </summary>
        public static ILocalUser LocalUser
        {
            get
            {
                if (IsInitialized())
                    return Social.localUser;
                else
                    return null;
            }
        }

        private static event Action<bool> mOnUserLogin;
        private static bool mIsLoadingScore = false;
        private static List<LoadScoreRequest> mLoadScoreRequests = new List<LoadScoreRequest>();

#if ACTIVE_GPGS
        [SerializeField] private bool mAutoInit;
        [SerializeField] private bool mEnableSavedGame;
        [SerializeField] private bool mEnableDebugLog;
        [SerializeField] private Gravity mPopupGravity;

        private static ITurnBasedMultiplayerClient sTurnBasedClient;
        private static IRealTimeMultiplayerClient sRealTimeClient;
        private static InvitationReceivedDelegate sInvitationDelegate;
        private static Queue<Action> sGPGSPendingInvitations = null;
#else
        private bool mAutoInit;
        private bool mEnableSavedGame;
        private bool mEnableDebugLog;
        private Gravity mPopupGravity;
#endif

        #endregion

        //=======================================================

        #region MonoBehaviour

        private void Awake()
        {
            if (mIntance == null)
            {
                mIntance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (mIntance != this)
                Destroy(this);
        }

        private IEnumerator Start()
        {
            if (mAutoInit)
            {
                yield return new WaitForSeconds(1f);

                Init(null);
            }
        }

        #endregion

        //=======================================================

        #region Public

        /// <summary>
        /// Initializes the service. This is required before any other actions can be done e.g reporting scores.
        /// During the initialization process, a login popup will show up if the user hasn't logged in, otherwise
        /// the process will carry on silently.
        /// Note that on iOS, the login popup will show up automatically when the app gets focus for the first 3 times
        /// while subsequent authentication calls will be ignored.
        /// </summary>
        public static void Init(Action<bool> pOnUsedLoggin = null)
        {
            mOnUserLogin = pOnUsedLoggin;

            // Authenticate and register a ProcessAuthentication callback
            // This call needs to be made before we can proceed to other calls in the Social API
#if UNITY_IOS
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
            Social.localUser.Authenticate(ProcessAuthentication);
#elif UNITY_ANDROID && ACTIVE_GPGS
            PlayGamesClientConfiguration.Builder gpgsConfigBuilder = new PlayGamesClientConfiguration.Builder();

            if (Instance.mEnableSavedGame)
                gpgsConfigBuilder.EnableSavedGames();

            // Build the config
            PlayGamesClientConfiguration gpgsConfig = gpgsConfigBuilder.Build();

            // Initialize PlayGamesPlatform
            PlayGamesPlatform.InitializeInstance(gpgsConfig);

            // Enable logging if required
            PlayGamesPlatform.DebugLogEnabled = Instance.mEnableDebugLog;

            // Set PlayGamesPlatforms as active
            if (Social.Active != PlayGamesPlatform.Instance)
                PlayGamesPlatform.Activate();

            // Now authenticate
            Social.localUser.Authenticate(ProcessAuthentication);
#elif UNITY_ANDROID && !ACTIVE_GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
#else
            Debug.Log("Failed to initialize Game Services module: platform not supported.");
#endif
        }

        /// <summary>
        /// Determines whether this module is initialized (user is authenticated) and ready to use.
        /// </summary>
        public static bool IsInitialized()
        {
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
        /// Shows the leaderboard UI for the given leaderboard in the specified time scope.
        /// </summary>
        public static void ShowLeaderboardUI(string leaderboardId, LeaderboardTimeSpan timeScope)
        {
            if (!IsInitialized())
            {
                Debug.Log("Couldn't show leaderboard UI: user is not logged in.");
                return;
            }

#if UNITY_IOS
            GameCenterPlatform.ShowLeaderboardUI(leaderboardId, (TimeScope)(timeScope - 1));
#elif UNITY_ANDROID && ACTIVE_GPGS
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId, (GooglePlayGames.BasicApi.LeaderboardTimeSpan)timeScope, null);
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
        public static void ReportScore(long score, string leaderboardId, Action<bool> callback = null)
        {
            if (!IsInitialized())
            {
                Debug.LogFormat("Failed to report score to leaderboard {0}: user is not logged in.", leaderboardId);
                if (callback != null)
                    callback(false);
                return;
            }

            Social.ReportScore(score, leaderboardId,
                (bool success) =>
                {
                    if (callback != null)
                        callback(success);
                }
            );
        }

        /// <summary>
        /// Reveals the hidden achievement with the specified name.
        /// </summary>
        public static void RevealAchievement(string achievementId, Action<bool> callback = null)
        {
            ReportAchievementProgress(achievementId, 0.0f, callback);
        }

        /// <summary>
        /// Unlocks the achievement with the specified name.
        /// </summary>
        public static void UnlockAchievement(string achievementId, Action<bool> callback = null)
        {
            ReportAchievementProgress(achievementId, 100.0f, callback);
        }

        /// <summary>
        /// Reports the progress of the incremental achievement with the specified name.
        /// </summary>
        public static void ReportAchievementProgress(string achievementId, double progress, Action<bool> callback)
        {
            if (!IsInitialized())
            {
                Debug.LogFormat("Failed to report progress for achievement {0}: user is not logged in.", achievementId);
                if (callback != null)
                    callback(false);
                return;
            }

            Social.ReportProgress(achievementId, progress,
                (bool success) =>
                {
                    if (callback != null)
                        callback(success);
                }
            );
        }

        /// <summary>
        /// Loads all friends of the authenticated user.
        /// Internally it will populate the LocalUsers.friends array and invoke the 
        /// callback with this array if the loading succeeded. 
        /// If the loading failed, the callback will be invoked with an empty array.
        /// If the LocalUsers.friends array is already populated then the callback will be invoked immediately
        /// without any loading request being made. 
        /// </summary>
        public static void LoadFriends(Action<IUserProfile[]> callback)
        {
            if (!IsInitialized())
            {
                Debug.Log("Failed to load friends: user is not logged in.");
                if (callback != null)
                    callback(new IUserProfile[0]);
                return;
            }

            if (Social.localUser.friends != null && Social.localUser.friends.Length > 0)
            {
                if (callback != null)
                    callback(Social.localUser.friends);
            }
            else
            {
                Social.localUser.LoadFriends(success =>
                {
                    if (success)
                    {
                        if (callback != null)
                            callback(Social.localUser.friends);
                    }
                    else
                    {
                        if (callback != null)
                            callback(new IUserProfile[0]);
                    }
                });
            }
        }

        /// <summary>
        /// Loads the user profiles associated with the given array of user IDs.
        /// </summary>
        public static void LoadUsers(string[] userIds, Action<IUserProfile[]> callback)
        {
            if (!IsInitialized())
            {
                Debug.Log("Failed to load users: user is not logged in.");
                if (callback != null)
                    callback(new IUserProfile[0]);
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
        public static void LoadScores(string leaderboardId, Action<string, IScore[]> callback)
        {
            if (!IsInitialized())
            {
                Debug.LogFormat("Failed to load scores from leaderboard {0}: user is not logged in.", leaderboardId);
                if (callback != null)
                    callback(leaderboardId, new IScore[0]);
                return;
            }

            // Create new request
            LoadScoreRequest request = new LoadScoreRequest();
            request.leaderboardId = leaderboardId;
            request.callback = callback;
            request.useLeaderboardDefault = true;
            request.loadLocalUserScore = false;

            // Add request to the queue
            mLoadScoreRequests.Add(request);

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
                Debug.LogFormat("Failed to load scores from leaderboard {0}: user is not logged in.", leaderboardId);
                if (callback != null)
                    callback(leaderboardId, new IScore[0]);
                return;
            }

            // Create new request
            LoadScoreRequest request = new LoadScoreRequest();
            request.leaderboardId = leaderboardId;
            request.callback = callback;
            request.useLeaderboardDefault = false;
            request.loadLocalUserScore = false;
            request.fromRank = fromRank;
            request.scoreCount = scoreCount;
            request.timeScope = timeScope;
            request.userScope = userScope;

            // Add request to the queue
            mLoadScoreRequests.Add(request);

            DoNextLoadScoreRequest();
        }

        /// <summary>
        /// Loads the local user's score from the specified leaderboard.
        /// Note that each load score request is added into a queue and the
        /// next request is called after the callback of previous request has been invoked.
        /// </summary>
        public static void LoadLocalUserScore(string leaderboardId, Action<string, IScore> callback)
        {
            if (!IsInitialized())
            {
                Debug.LogFormat("Failed to load local user's score from leaderboard {0}: user is not logged in.", leaderboardId);
                if (callback != null)
                    callback(leaderboardId, null);
                return;
            }

            // Create new request
            LoadScoreRequest request = new LoadScoreRequest();
            request.leaderboardId = leaderboardId;
            request.callback = delegate (string ldbName, IScore[] scores)
            {
                if (scores != null)
                {
                    if (callback != null)
                        callback(ldbName, scores[0]);
                }
                else
                {
                    if (callback != null)
                        callback(ldbName, null);
                }
            };
            request.useLeaderboardDefault = false;
            request.loadLocalUserScore = true;
            request.fromRank = -1;
            request.scoreCount = -1;
            request.timeScope = TimeScope.AllTime;
            request.userScope = UserScope.FriendsOnly;

            // Add request to the queue
            mLoadScoreRequests.Add(request);

            DoNextLoadScoreRequest();
        }

        /// <summary>
        /// Signs the user out. Available on Android only.
        /// </summary>
        public static void SignOut()
        {
            if (!IsInitialized())
            {
                return;
            }

#if UNITY_ANDROID && ACTIVE_GPGS
            PlayGamesPlatform.Instance.SignOut();
#elif UNITY_ANDROID && !ACTIVE_GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
#else
            Debug.Log("Signing out from script is not available on this platform.");
#endif
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

#if UNITY_ANDROID && ACTIVE_GPGS
            return PlayGamesPlatform.Instance.GetServerAuthCode();
#elif UNITY_ANDROID && !ACTIVE_GPGS
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
        public static void GetAnotherServerAuthCode(bool reAuthenticateIfNeeded, Action<string> callback)
        {
            if (!IsInitialized())
            {
                return;
            }

#if UNITY_ANDROID && ACTIVE_GPGS
            PlayGamesPlatform.Instance.GetAnotherServerAuthCode(reAuthenticateIfNeeded, callback);
#elif UNITY_ANDROID && !ACTIVE_GPGS
            Debug.LogError("SDK missing. Please import Google Play Games plugin for Unity.");
#else
            Debug.Log("GetAnotherServerAuthCode is only available on Google Play Games platform.");
#endif
        }

        #endregion

        //=======================================================

        #region Private methods

        private static void DoNextLoadScoreRequest()
        {
            LoadScoreRequest request;

            if (mIsLoadingScore)
                return;

            if (mLoadScoreRequests.Count == 0)
                return;

            mIsLoadingScore = true;
            request = mLoadScoreRequests[0]; // fetch the next request
            mLoadScoreRequests.RemoveAt(0);  // then remove it from the queue

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
                Social.LoadScores(ldb.id, (IScore[] scores) =>
                {
                    if (request.callback != null)
                        request.callback(request.leaderboardId, scores);

                    // Load next request
                    mIsLoadingScore = false;
                    DoNextLoadScoreRequest();
                });
#elif UNITY_IOS
                // On iOS, we use LoadScores from ILeaderboard with default parameters.
                ldb.LoadScores((bool success) =>
                    {
                        if (request.callback != null)
                            request.callback(request.leaderboardId, ldb.scores);

                        // Load next request
                        mIsLoadingScore = false;
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

                ldb.LoadScores((bool success) =>
                {
                    if (request.loadLocalUserScore)
                    {
                        IScore[] returnScores = new IScore[] { ldb.localUserScore };

                        if (request.callback != null)
                            request.callback(request.leaderboardId, returnScores);
                    }
                    else
                    {
                        if (request.callback != null)
                            request.callback(request.leaderboardId, ldb.scores);
                    }

                    // Load next request
                    mIsLoadingScore = false;
                    DoNextLoadScoreRequest();
                });
            }
        }

        // This function gets called when Authenticate completes
        // Note that if the operation is successful, Social.localUser will contain data from the server.
        private static void ProcessAuthentication(bool success)
        {
            if (success)
            {
                if (onUserLoginSucceeded != null)
                    onUserLoginSucceeded();

                if (mOnUserLogin != null)
                {
                    mOnUserLogin(true);
                    mOnUserLogin = null;
                }

                // Set GPGS popup gravity, this needs to be done after authentication.
#if UNITY_ANDROID && ACTIVE_GPGS
                PlayGamesPlatform.Instance.SetGravityForPopups((GooglePlayGames.BasicApi.Gravity)Instance.mPopupGravity);
#endif
            }
            else
            {
                if (onUserLoginFailed != null)
                    onUserLoginFailed();

                if (mOnUserLogin != null)
                {
                    mOnUserLogin(false);
                    mOnUserLogin = null;
                }
            }
        }

        #endregion

        //=======================================================

#if UNITY_EDITOR

        #region Editor

        [CustomEditor(typeof(GameServices))]
        public class GameServicesEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                GUILayout.BeginVertical("box");
#if ACTIVE_GPGS
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Disable Game Services"))
                    EditorHelper.RemoveDirective("ACTIVE_GPGS");
#else
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Enable Game Services"))
                    EditorHelper.AddDirective("ACTIVE_GPGS");
#endif
                GUILayout.EndVertical();
            }
        }

        #endregion

#endif
    }
}
