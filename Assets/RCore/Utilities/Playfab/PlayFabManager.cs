#if PLAYFAB

using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using LoginResult = PlayFab.ClientModels.LoginResult;
using System;
using System.Collections.Generic;
using PlayFab.CloudScriptModels;
using ExecuteCloudScriptResult = PlayFab.ClientModels.ExecuteCloudScriptResult;
using ElpisBattle.Server.Handlers;

namespace RCore.PlayFab
{
    /// <summary>
    /// Supported Authentication types
    /// See - https://api.playfab.com/documentation/client#Authentication
    /// </summary>
    public enum Authtypes
    {
        Silent,
        UsernameAndPassword,
        EmailAndPassword,
        RegisterPlayFabAccount
    }

    public class PlayFabManager
    {
        public delegate void LoginSuccessEvent(LoginResult success);
        public static event LoginSuccessEvent OnLoginSuccess;

        public delegate void PlayFabErrorEvent(PlayFabError error);
        public static event PlayFabErrorEvent OnPlayFabError;

        private static PlayFabManager _Instance;
        public static PlayFabManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new PlayFabManager();
                return _Instance;
            }
        }

        // Accessbility for PlayFab ID & Session Tickets
        public static string PlayFabId => _playFabId;
        private static string _playFabId;

        public static string SessionTicket => _SessionTicket;
        private static string _SessionTicket;

        private const string _LoginRememberKey = "PlayFabLoginRemember";
        private const string _PlayFabRememberMeIdKey = "PlayFabIdPassGuid";
        private const string _PlayFabAuthTypeKey = "PlayFabAuthType";

        public PlayerInfo playerInfo;
        public string email;
        public string username;
        public string password;
        public string authTicket;
        public GetPlayerCombinedInfoRequestParams infoRequestParams;
        public string displayName = null;

        // This is a force link flag for custom ids for demoing
        public bool forceLink = false;

        private Dictionary<string, List<PlayerLeaderboardEntry>> m_LeaderboardData;

        public PlayFabManager()
        {
            _Instance = this;
        }

        /// <summary>
        /// Remember the user next time they log in
        /// This is used for Auto-Login purpose.
        /// </summary>
        public bool RememberMe
        {
            get { return PlayerPrefs.GetInt(_LoginRememberKey, 0) == 0 ? false : true; }
            set { PlayerPrefs.SetInt(_LoginRememberKey, value ? 1 : 0); }
        }

        /// <summary>
        /// Remember the type of authenticate for the user
        /// </summary>
        public Authtypes AuthType
        {
            get { return (Authtypes)PlayerPrefs.GetInt(_PlayFabAuthTypeKey, 0); }
            set { PlayerPrefs.SetInt(_PlayFabAuthTypeKey, (int)value); }
        }

        /// <summary>
        /// Generated Remember Me ID
        /// Pass Null for a value to have one auto-generated.
        /// </summary>
        private string RememberMeId
        {
            get { return PlayerPrefs.GetString(_PlayFabRememberMeIdKey, ""); }
            set { PlayerPrefs.SetString(_PlayFabRememberMeIdKey, value ?? Guid.NewGuid().ToString()); }
        }

        public void ClearRememberMe()
        {
            PlayerPrefs.DeleteKey(_LoginRememberKey);
            PlayerPrefs.DeleteKey(_PlayFabRememberMeIdKey);
            PlayerPrefs.DeleteKey(_PlayFabAuthTypeKey);
        }

        public void ForgetAllCredentials()
        {
            PlayFabClientAPI.ForgetAllCredentials();
            email = string.Empty;
            password = string.Empty;
            authTicket = string.Empty;
            Authenticate(Authtypes.Silent);
        }

        private void SetPlayerInfo(LoginResult result)
        {
            playerInfo = new PlayerInfo
            {
                EntityToken = result.EntityToken.EntityToken,
                PlayFabId = result.PlayFabId,
                SessionTicket = result.SessionTicket,
                Entity = new global::PlayFab.AuthenticationModels.EntityKey
                {
                    Id = result.EntityToken.Entity.Id,
                    Type = result.EntityToken.Entity.Type
                }
            };
        }

        /// <summary>
        /// Kick off the authentication process by specific authtype.
        /// </summary>
        /// <param name="authType"></param>
        public void Authenticate(Authtypes authType, Action<LoginResult> pCallback = null)
        {
            AuthType = authType;
            switch (AuthType)
            {
                case Authtypes.Silent:
                    AuthenticateSilently(pCallback);
                    break;

                case Authtypes.EmailAndPassword:
                    AuthenticateEmailPassword(pCallback);
                    break;

                case Authtypes.RegisterPlayFabAccount:
                    AddAccountAndPassword(pCallback);
                    break;
            }
        }

        /// <summary>
        /// Authenticate a user in PlayFab using an Email & Password combo
        /// </summary>
        public void AuthenticateEmailPassword(Action<LoginResult> pCallback = null)
        {
            //Check if the users has opted to be remembered.
            if (RememberMe && !string.IsNullOrEmpty(RememberMeId))
            {
                var request = new LoginWithCustomIDRequest()
                {
                    TitleId = PlayFabSettings.TitleId,
                    CustomId = RememberMeId,
                    CreateAccount = true,
                    InfoRequestParameters = infoRequestParams
                };
                // If the user is being remembered, then log them in with a customid that was 
                // generated by the RememberMeId property
                PlayFabClientAPI.LoginWithCustomID(request,
                    // Success
                    (LoginResult result) =>
                    {
                        Debug.Log($"{result.ToJson()}");

                        SetPlayerInfo(result);

                        //Store identity and session
                        _playFabId = result.PlayFabId;
                        _SessionTicket = result.SessionTicket;

                        //report login result back to subscriber
                        OnLoginSuccess?.Invoke(result);
                        pCallback?.Invoke(result);
                    },

                    // Failure
                    (PlayFabError error) =>
                    {
                        //report error back to subscriber
                        OnPlayFabError?.Invoke(error);
                        pCallback?.Invoke(null);
                    });
                return;
            }

            // If username & password is empty, then do not continue, and Call back to Authentication UI Display 
            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(password))
            {
                Debug.LogError("Email and Password can't be empty");
                pCallback?.Invoke(null);
                return;
            }

            // We have not opted for remember me in a previous session, so now we have to login the user with email & password.
            PlayFabClientAPI.LoginWithEmailAddress(
                new LoginWithEmailAddressRequest()
                {
                    TitleId = PlayFabSettings.TitleId,
                    Email = email,
                    Password = password,
                    InfoRequestParameters = infoRequestParams
                },
                // Success
                (LoginResult result) =>
                {
                    Debug.Log($"{result.ToJson()}");

                    SetPlayerInfo(result);

                    // Store identity and session
                    _playFabId = result.PlayFabId;
                    _SessionTicket = result.SessionTicket;

                    // Note: At this point, they already have an account with PlayFab using a Username (email) & Password
                    // If RememberMe is checked, then generate a new Guid for Login with CustomId.
                    if (RememberMe)
                    {
                        RememberMeId = Guid.NewGuid().ToString();
                        AuthType = Authtypes.EmailAndPassword;

                        // Fire and forget, but link a custom ID to this PlayFab Account.
                        PlayFabClientAPI.LinkCustomID(
                                new LinkCustomIDRequest
                                {
                                    CustomId = RememberMeId,
                                    ForceLink = forceLink
                                },
                                null,   // Success callback
                                null    // Failure callback
                                );
                    }

                    //report login result back to subscriber
                    OnLoginSuccess?.Invoke(result);
                    pCallback?.Invoke(result);
                },
                // Failure
                (PlayFabError error) =>
                {
                    //Report error back to subscriber
                    OnPlayFabError?.Invoke(error);
                    pCallback?.Invoke(null);
                });
        }

        /// <summary>
        /// Register a user with an Email & Password
        /// Note: We are not using the RegisterPlayFab API
        /// </summary>
        public void AddAccountAndPassword(Action<LoginResult> pCallback = null)
        {
            // Any time we attempt to register a player, first silently authenticate the player.
            // This will retain the players True Origination (Android, iOS, Desktop)
            AuthenticateSilently(
                (LoginResult result) =>
                {
                    if (result == null)
                    {
                        //something went wrong with Silent Authentication, Check the debug console.
                        OnPlayFabError.Invoke(new PlayFabError()
                        {
                            Error = PlayFabErrorCode.UnknownError,
                            ErrorMessage = "Silent Authentication by Device failed"
                        });
                    }

                    // Note: If silent auth is success, which is should always be and the following 
                    // below code fails because of some error returned by the server ( like invalid email or bad password )
                    // this is okay, because the next attempt will still use the same silent account that was already created.

                    var request = new AddUsernamePasswordRequest()
                    {
                        Username = username ?? result.PlayFabId, // Because it is required & Unique and not supplied by User.
                        Email = email,
                        Password = password,
                    };
                    // Now add our username & password.
                    PlayFabClientAPI.AddUsernamePassword(request,
                            // Success
                            (AddUsernamePasswordResult addResult) =>
                            {
                                Debug.Log($"{addResult.ToJson()}");

                                SetPlayerInfo(result);

                                // Store identity and session
                                _playFabId = result.PlayFabId;
                                _SessionTicket = result.SessionTicket;

                                // If they opted to be remembered on next login.
                                if (RememberMe)
                                {
                                    // Generate a new Guid 
                                    RememberMeId = Guid.NewGuid().ToString();

                                    // Fire and forget, but link the custom ID to this PlayFab Account.
                                    PlayFabClientAPI.LinkCustomID(
                                            new LinkCustomIDRequest()
                                            {
                                                CustomId = RememberMeId,
                                                ForceLink = forceLink
                                            },
                                            null,
                                            null);
                                }

                                // Override the auth type to ensure next login is using this auth type.
                                AuthType = Authtypes.EmailAndPassword;

                                // Report login result back to subscriber.
                                OnLoginSuccess?.Invoke(result);
                                pCallback?.Invoke(result);
                            },

                            // Failure
                            (PlayFabError error) =>
                            {
                                //Report error result back to subscriber
                                OnPlayFabError?.Invoke(error);
                                pCallback?.Invoke(null);
                            });
                });
        }

        public void AuthenticateSilently(Action<LoginResult> pCallback = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        //Get the device id from native android
        AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
        AndroidJavaClass secure = new AndroidJavaClass("android.provider.Settings$Secure");
        string deviceId = secure.CallStatic<string>("getString", contentResolver, "android_id");

        //Login with the android device ID
        PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            AndroidDevice = SystemInfo.deviceModel,
            OS = SystemInfo.operatingSystem,
            AndroidDeviceId = deviceId,
            CreateAccount = true,
            InfoRequestParameters = infoRequestParams
        }, (result) =>
        {
            SetPlayerInfo(result);

            //Store Identity and session
            _playFabId = result.PlayFabId;
            _SessionTicket = result.SessionTicket;

            //report login result back to the subscriber
            OnLoginSuccess?.Invoke(result);
            //report login result back to the caller
            pCallback?.Invoke(result);
            Debug.Log($"{result.ToJson()}");
        }, (error) =>
        {
            //report errro back to the subscriber
            OnPlayFabError?.Invoke(error);
            //make sure the loop completes, callback with null
            pCallback?.Invoke(null);
            //Output what went wrong to the console.
            Debug.LogError(error.GenerateErrorReport());
        });

#elif UNITY_IPHONE || UNITY_IOS && !UNITY_EDITOR
        PlayFabClientAPI.LoginWithIOSDeviceID(new LoginWithIOSDeviceIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            DeviceModel = SystemInfo.deviceModel,
            OS = SystemInfo.operatingSystem,
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            InfoRequestParameters = infoRequestParams
        }, (result) =>
        {
            SetPlayerInfo(result);

            //Store Identity and session
            _playFabId = result.PlayFabId;
            _SessionTicket = result.SessionTicket;

            //report login result back to the subscriber
            OnLoginSuccess?.Invoke(result);
            //report login result back to the caller
            pCallback.Invoke(result);
            Debug.Log($"{result.ToJson()}");
        }, (error) =>
        {
            //report errro back to the subscriber
            OnPlayFabError?.Invoke(error);
            //make sure the loop completes, callback with null
            pCallback?.Invoke(null);
            //Output what went wrong to the console.
            Debug.LogError(error.GenerateErrorReport());
        });
#else
            PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true,
                InfoRequestParameters = infoRequestParams
            }, (result) =>
            {
                SetPlayerInfo(result);

                //Store Identity and session
                _playFabId = result.PlayFabId;
                _SessionTicket = result.SessionTicket;

                //report login result back to the subscriber
                OnLoginSuccess?.Invoke(result);
                //report login result back to the caller
                pCallback?.Invoke(result);
                Debug.Log($"{result.ToJson()}");
            }, (error) =>
            {
                //report errro back to the subscriber
                OnPlayFabError?.Invoke(error);
                //make sure the loop completes, callback with null
                pCallback?.Invoke(null);
                //Output what went wrong to the console.
                Debug.LogError(error.GenerateErrorReport());
            });
#endif
        }

        public void UnlinkSilentAuth()
        {
            AuthenticateSilently((result) =>
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            //Get the device id from native android
            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
            AndroidJavaClass secure = new AndroidJavaClass("android.provider.Settings$Secure");
            string deviceId = secure.CallStatic<string>("getString", contentResolver, "android_id");

            //Fire and forget, unlink this android device.
            PlayFabClientAPI.UnlinkAndroidDeviceID(new UnlinkAndroidDeviceIDRequest() {
                AndroidDeviceId = deviceId
            }, null, null);

#elif UNITY_IPHONE || UNITY_IOS && !UNITY_EDITOR
            PlayFabClientAPI.UnlinkIOSDeviceID(new UnlinkIOSDeviceIDRequest()
            {
                DeviceId = SystemInfo.deviceUniqueIdentifier
            }, null, null);
#else
                PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest()
                {
                    CustomId = SystemInfo.deviceUniqueIdentifier
                }, null, null);
#endif
            });
        }

        /// <summary>
        /// Load the user's account info to get their DisplayName
        /// </summary>
        public void GetAccountInfo(Action<UserTitleInfo> pCallback = null)
        {
            PlayFabClientAPI.GetAccountInfo(
                // Request
                new GetAccountInfoRequest
                {
                    // No properties means get the calling user's info
                },
                // Success
                (GetAccountInfoResult result) =>
                {
                    Debug.Log($"GetAccountInfo completed.");
                    Debug.Log($"{result.ToJson()}");
                    displayName = result.AccountInfo.TitleInfo.DisplayName;
                    pCallback?.Invoke(result.AccountInfo.TitleInfo);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("GetAccountInfo failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(null);
                });
        }

        /// <summary>
        /// Update the user's per-title DisplayName
        /// </summary>
        public void UpdateUserTitleDisplayName(string pName, Action<UpdateUserTitleDisplayNameResult> pCallback = null)
        {
            PlayFabClientAPI.UpdateUserTitleDisplayName(
                // Request
                new UpdateUserTitleDisplayNameRequest
                {
                    DisplayName = pName
                },
                // Success
                (UpdateUserTitleDisplayNameResult result) =>
                {
                    Debug.Log("UpdateUserTitleDisplayName completed.");
                    Debug.Log($"{result.ToJson()}");
                    displayName = pName;
                    pCallback?.Invoke(result);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("UpdateUserTitleDisplayName failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(null);
                });
        }

        /// <summary>
        /// Update a user's individual game stat
        /// This uses a custom event to trigger cloudscript which performs the stat updates
        /// </summary>
        public void Update(string pEventName, Dictionary<string, object> pBody, Action<bool> pCallback)
        {
            PlayFabClientAPI.WritePlayerEvent(
                // Request
                new WriteClientPlayerEventRequest
                {
                    EventName = pEventName,
                    Body = pBody
                },
                // Success
                (WriteEventResponse response) =>
                {
                    Debug.Log($"WritePlayerEvent {pEventName} completed.");
                    Debug.Log($"{response.ToJson()}");
                    pCallback?.Invoke(true);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("WritePlayerEvent failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(false);
                });
        }

        /// <summary>
        /// Update the user's game stats in bulk
        /// This uses a custom even to trigger cloudscript which performs the stat updates
        /// </summary>
        public void UpdateStatistics(Dictionary<string, object> pValues, Action<bool> pCallback)
        {
            PlayFabClientAPI.WritePlayerEvent(
                // Request
                new WriteClientPlayerEventRequest
                {
                    EventName = "update_statistics",
                    Body = new Dictionary<string, object>
                    {
                    { "stats", pValues }
                    }
                },
                // Success
                (WriteEventResponse response) =>
                {
                    Debug.Log($"WritePlayerEvent (UpdateStatistics) completed.");
                    Debug.Log($"{response.ToJson()}");
                    pCallback?.Invoke(true);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("WritePlayerEvent failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(false);
                });
        }

        /// <summary>
        /// Update a user's individual game stat
        /// This uses a custom even to trigger cloudscript which performs the stat updates
        /// </summary>
        public void UpdateStatistic(string stat, int value, Action<bool> pCallback = null)
        {
            PlayFabClientAPI.WritePlayerEvent(
                // Request
                new WriteClientPlayerEventRequest
                {
                    EventName = "update_statistic",
                    Body = new Dictionary<string, object>
                    {
                    { "stat_name", stat },
                    { "value", value }
                    }
                },
                // Success
                (WriteEventResponse response) =>
                {
                    Debug.Log($"WritePlayerEvent (UpdateStatistic) completed.");
                    Debug.Log($"{response.ToJson()}");
                    pCallback?.Invoke(true);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("WritePlayerEvent failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(false);
                });
        }

        /// <summary>
        /// Load the game's server configured data
        /// </summary>
        public void LoadTitleData(string[] pKeys, Action<GetTitleDataResult> pCallback = null)
        {
            var request = new GetTitleDataRequest();
            if (pKeys != null && pKeys.Length > 0)
            {
                request.Keys = new List<string>();
                foreach (var key in pKeys)
                    request.Keys.Add(key);
            }
            PlayFabClientAPI.GetTitleData(request,
                // Success
                (GetTitleDataResult result) =>
                {
                    Debug.Log($"GetTitleData completed.");
                    Debug.Log($"{result.ToJson()}");
                    pCallback?.Invoke(result);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("GetTitleData failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(null);
                });
        }

        public void LoadTitleData(List<string> pKeys, Action<GetTitleDataResult> pCallback = null)
        {
            LoadTitleData(pKeys.ToArray(), pCallback);
        }

        public void LoadTitleData(string pKey, Action<GetTitleDataResult> pCallback = null)
        {
            LoadTitleData(new string[1] { pKey }, pCallback);
        }

        public void SetTitleData(string pKey, string pValue, Action<bool> pCallback = null)
        {
            PlayFabServerAPI.SetTitleData(
                new global::PlayFab.ServerModels.SetTitleDataRequest
                {
                    Key = pKey,
                    Value = pValue
                },
                result =>
                {
                    Debug.Log($"Set titleData {pKey} successful");
                    pCallback?.Invoke(true);
                },
                error =>
                {
                    Debug.LogError("Got error setting titleData:");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(false);
                }
            );
        }

        /// <summary>
        /// Load the user's server data
        /// </summary>
        public void GetUserData(string[] pKeys, Action<GetUserDataResult> pCallback = null)
        {
            var request = new GetUserDataRequest();
            if (pKeys != null && pKeys.Length > 0)
            {
                request.Keys = new List<string>();
                foreach (var key in pKeys)
                    request.Keys.Add(key);
            }
            PlayFabClientAPI.GetUserData(request,
                // Success
                (GetUserDataResult result) =>
                {
                    Debug.Log("GetUserData completed.");
                    Debug.Log($"{result.ToJson()}");
                    pCallback?.Invoke(result);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("GetUserData failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(null);
                });
        }

        public void UpdateUserData(Dictionary<string, string> pData, Action<UpdateUserDataResult> pCallback = null)
        {
            if (pData == null)
                return;

            var request = new UpdateUserDataRequest();
            request.Data = pData;
            PlayFabClientAPI.UpdateUserData(request,
                // Success
                (UpdateUserDataResult result) =>
                {
                    Debug.Log($"UpdateUserData completed.");
                    Debug.Log($"{result.ToJson()}");
                    pCallback?.Invoke(result);
                },
                // Failure
                (PlayFabError error) =>
                {
                    Debug.LogError("UpdateUserData failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(null);
                });
        }

        /// <summary>
        /// Load the leaderboard data
        /// </summary>
        public void LoadLeaderboards(string[] pLeaderboards, int pStartPosition, int pMaxEntries, Action<GetLeaderboardResult> onSuccess = null)
        {
            m_LeaderboardData = new Dictionary<string, List<PlayerLeaderboardEntry>>();

            foreach (string board in pLeaderboards)
            {
                PlayFabClientAPI.GetLeaderboard(
                    // Request
                    new GetLeaderboardRequest
                    {
                        StatisticName = board,
                        StartPosition = pStartPosition,
                        MaxResultsCount = pMaxEntries
                    },
                    // Success
                    (GetLeaderboardResult result) =>
                    {
                        var boardName = (result.Request as GetLeaderboardRequest).StatisticName;
                        m_LeaderboardData[boardName] = result.Leaderboard;
                        Debug.Log($"GetLeaderboard completed: {boardName}");
                        Debug.Log($"{result.ToJson()}");
                        onSuccess(result);
                    },
                    // Failure
                    (PlayFabError error) =>
                    {
                        Debug.LogError("GetLeaderboard failed.");
                        Debug.LogError(error.GenerateErrorReport());
                        onSuccess(null);
                    });
            }
        }

        /// <summary>
        /// Access for leaderboards
        /// </summary>
        public List<PlayerLeaderboardEntry> GetLeaderboard(string board)
        {
            return m_LeaderboardData[board];
        }

        public void AddOrUpdateContactEmail(string pEmail, Action<AddOrUpdateContactEmailResult> pCallback)
        {
            var request = new AddOrUpdateContactEmailRequest();
            request.EmailAddress = pEmail;
            PlayFabClientAPI.AddOrUpdateContactEmail(request,
                (AddOrUpdateContactEmailResult result) =>
                {
                    Debug.Log($"AddOrUpdateContactEmail completed.");
                    Debug.Log($"{result.ToJson()}");
                    email = pEmail;
                    pCallback?.Invoke(result);
                }, (PlayFabError error) =>
                {
                    Debug.LogError("AddOrUpdateContactEmail failed.");
                    Debug.LogError(error.GenerateErrorReport());
                    pCallback?.Invoke(null);
                });
        }

        public void ExecuteCloudScript(string pFunctionName, string pParam, Action<ExecuteCloudScriptResult> pCallback = null)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                FunctionName = pFunctionName,
                FunctionParameter = pParam
            };
            PlayFabClientAPI.ExecuteCloudScript(request, (result) =>
            {
                Debug.Log($"{result.ToJson()}");
                pCallback?.Invoke(result);
            }, (error) =>
            {
                pCallback?.Invoke(null);
            });
        }

#region Cloud Function / Cloud Script

        public void ExecuteCloudFunction(PlayFabAuthenticationContext playFabAuthenticationContext, string functionName, object functionParameter, Action<string> pCallback)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = functionName,
                FunctionParameter = functionParameter,
                AuthenticationContext = playFabAuthenticationContext
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request,
                (result) =>
                {
                    var json = result?.FunctionResult != null ? result.FunctionResult.ToString() : null;
                    pCallback?.Invoke(json);
                },
                (error) =>
                {
                    Debug.Log($"MatchLobby join request failed. Message: {error.ErrorMessage}, Code: {error.HttpCode}");
                    pCallback?.Invoke(null);
                }
            );
        }

#endregion


#region Matchmaking

#endregion
    }
}

#endif