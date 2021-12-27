/**
 * Author NBear - nbhung71711 @gmail.com - 2019
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using System.Threading.Tasks;
using RCore.Service.RFirebase.Analytics;
using RCore.Service.RFirebase.Storage;
using RCore.Service.RFirebase.Database;
using RCore.Service.RFirebase.Auth;
using RCore.Service.RFirebase.Crashlytics;
using Debug = UnityEngine.Debug;
#if ACTIVE_FIREBASE
using Firebase;
using Firebase.Extensions;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Service.RFirebase
{
    public class WaitForTask : CustomYieldInstruction
    {
        Task task;

        public WaitForTask(Task task)
        {
            this.task = task;
        }

        public override bool keepWaiting
        {
            get
            {
                if (task.IsCompleted)
                {
                    if (task.IsFaulted)
                        LogException(task.Exception);

                    return false;
                }
                return true;
            }
        }

        protected virtual void LogException(Exception exception)
        {
            Debug.LogError(exception.ToString());
        }
    }

    public class RFirebaseManager : MonoBehaviour
    {
        #region Members

        private const string FAILED_EVENTS = "FailedEvents";

        private static RFirebaseManager mInstance;
        public static RFirebaseManager Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = FindObjectOfType<RFirebaseManager>();
                    if (mInstance != null)
                        DontDestroyOnLoad(mInstance);
                    else
                        mInstance = new GameObject("RFirebaseManager").AddComponent<RFirebaseManager>();
                }
                return mInstance;

            }
        }

        public bool dontDestroy;

        public static RFirebaseAuth auth { get { return RFirebaseAuth.Instance; } }
        public static RFirebaseAnalytics analytics { get { return RFirebaseAnalytics.Instance; } }
        public static RFirebaseStorage storage { get { return RFirebaseStorage.Instance; } }
        public static RFirebaseDatabase database { get { return RFirebaseDatabase.Instance; } }
        public static RFirebaseMessaging messaging { get { return RFirebaseMessaging.Instance; } }
        public static RFirebaseRemote remote { get { return RFirebaseRemote.Instance; } }

        public static bool initialized { get; private set; }
        public static bool authenticated { get; private set; }
        public static string userId { get; private set; }
        public static string userName { get; private set; }

        private static List<AnalyticEvent> mFailedEvents = new List<AnalyticEvent>();

        #endregion

        //=============================================

        #region MonoBehaviour

        private void Awake()
        {
            if (mInstance == null)
            {
                mInstance = this;

                if (dontDestroy)
                    DontDestroyOnLoad(mInstance);
            }
            else if (mInstance != this)
                Destroy(gameObject);

        }

        private void OnApplicationPause(bool pause)
        {
            string failedEvents = JsonHelper.ToJson(mFailedEvents);
            PlayerPrefs.SetString(FAILED_EVENTS, failedEvents);
        }

        private void OnApplicationQuit()
        {
            string failedEvents = JsonHelper.ToJson(mFailedEvents);
            PlayerPrefs.SetString(FAILED_EVENTS, failedEvents);
        }

        #endregion

        //=============================================

        #region Public

        public static void Init(Action<bool> pOnfinished)
        {
            if (initialized)
                return;

#if ACTIVE_FIREBASE_ANALYTICS || ACTIVE_FIREBASE_STORAGE || ACTIVE_FIREBASE_DATABASE || ACTIVE_FIREBASE_AUTH || ACTIVE_FIREBASE_REMOTE
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                Debug.Log("Firebase Status " + task.Result.ToString());
                if (task.Result == DependencyStatus.Available)
                {
                    analytics.Initialize();
                    auth.Initialize();
                    messaging.Initialize();
                    pOnfinished.Raise(true);
                    LoadFailedEvents();
                    initialized = true;
                }
                else
                {
                    initialized = false;
                    pOnfinished.Raise(false);
                }
            });
#else
            authenticated = false;
            pOnfinished.Raise(false);
#endif

            initialized = true;
        }

        public static void InitRemote(Dictionary<string, object> pDefaultConfig, Action<bool> pOnFetched) => remote.Initialize(pDefaultConfig, pOnFetched);

        private static void LoadFailedEvents()
        {
            if (!analytics.initialized)
                return;

            var allEventsStr = PlayerPrefs.GetString(FAILED_EVENTS);
            mFailedEvents = JsonHelper.ToList<AnalyticEvent>(allEventsStr);

            if (mFailedEvents == null)
                mFailedEvents = new List<AnalyticEvent>();

            if (mFailedEvents.Count > 0)
            {
                for (int i = mFailedEvents.Count - 1; i >= 0; i--)
                {
                    string name = mFailedEvents[i].name;
                    string[] paramNames = mFailedEvents[i].paramNames;
                    string[] paramValuesStr = mFailedEvents[i].paramValuesStr;
                    float[] paramValuesNum = mFailedEvents[i].paramValuesNum;

                    if (paramValuesStr != null && paramValuesStr.Length > 0)
                        analytics.LogEvent(name, paramNames, paramValuesStr);
                    else if (paramValuesNum != null && paramValuesNum.Length > 0)
                        analytics.LogEvent(name, paramNames, paramValuesNum);
                    else
                        analytics.LogEvent(name);

                    mFailedEvents.RemoveAt(i);
                }
            }

            allEventsStr = JsonHelper.ToJson(mFailedEvents);
            PlayerPrefs.SetString(FAILED_EVENTS, allEventsStr);
            PlayerPrefs.Save();
        }

        public static void SetUser(string pUserId, string pUserName)
        {
            if (!initialized)
                return;

            userId = pUserId;
            userName = pUserName;

            analytics.SetUserId(pUserId);
            analytics.SetUserProperty("username", pUserName);
            RCrashlytics.SetUserId(pUserId);
        }

        //=== Firebase Auth

        /// <summary>
        /// Database and storage need authentication to Init
        /// </summary>
        public static void SigninAnonymously(Action<bool> pOnSuccess)
        {
            var task = auth.SigninAnonymouslyAsync();
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsCanceled && !task.IsFaulted;
                if (success)
                {
                    database.Initialize();
                    storage.Initialize();
                }
                pOnSuccess.Raise(success);
            });
        }

        public static void SignInWithCustomToken(string pToken, Action<bool> pOnSuccess)
        {
            var task = auth.SignInWithCustomTokenAsync(pToken);
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsCanceled && !task.IsFaulted;
                if (success)
                {
                    database.Initialize();
                    storage.Initialize();
                }
                pOnSuccess.Raise(success);
            });
        }

        //=== Firebase Analytics

        public static bool LogEvent(string pEventName)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName);
                return true;
            }

            mFailedEvents.Add(new AnalyticEvent(pEventName));

            return false;
        }

        public static bool LogEvent(string pEventName, string pParamName, string pParamValue)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName, pParamName, pParamValue);
                return true;
            }
            mFailedEvents.Add(new AnalyticEvent(pEventName, pParamName, pParamValue));

            return false;
        }

        public static bool LogEvent(string pEventName, string pParamName, long pParamValue)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName, pParamName, pParamValue);
                return true;
            }

            mFailedEvents.Add(new AnalyticEvent(pEventName, pParamName, pParamValue));

            return false;
        }

        public static bool LogEvent(string pEventName, string pParamName, int pParamValue)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName, pParamName, pParamValue);
                return true;
            }

            mFailedEvents.Add(new AnalyticEvent(pEventName, pParamName, pParamValue));

            return false;
        }

        public static bool LogEvent(string pEventName, string pParamName, float pParamValue)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName, pParamName, pParamValue);
                return true;
            }

            mFailedEvents.Add(new AnalyticEvent(pEventName, pParamName, pParamValue));

            return false;
        }

        public static bool LogEvent(string pEventName, string[] pParamNames, int[] pParamValues)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName, pParamNames, pParamValues);
                return true;
            }

            mFailedEvents.Add(new AnalyticEvent(pEventName, pParamNames, pParamValues));

            return false;
        }

        public static bool LogEvent(string pEventName, string[] pParamNames, float[] pParamValues)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName, pParamNames, pParamValues);
                return true;
            }

            mFailedEvents.Add(new AnalyticEvent(pEventName, pParamNames, pParamValues));

            return false;
        }

        public static bool LogEvent(string pEventName, string[] pParamNames, string[] pParamValues)
        {
            if (analytics != null && analytics.initialized)
            {
                analytics.LogEvent(pEventName, pParamNames, pParamValues);
                return true;
            }

            mFailedEvents.Add(new AnalyticEvent(pEventName, pParamNames, pParamValues));

            return false;
        }

        //=== Firebase Database

        public static void CheckConnection(Action<bool> pOnConnected)
        {
            if (!initialized)
                pOnConnected.Raise(false);
            else
                database.CheckOnline(() => pOnConnected.Raise(true));
        }

        //=== Firebase Storage

        public static void DownloadFile(SavedFileDefinition pStoDef, Action<string> pFoundFile, Action pNotFoundFile, Action pFailed)
        {
            if (!initialized)
                pFailed.Raise();

            storage.DownloadBytes((task, content) =>
            {
                bool success = task != null && !task.IsFaulted && !task.IsCanceled;
                if (success)
                    pFoundFile.Raise(content);
                else if (task != null)
                {
                    if (task.IsFaulted)
                    {
                        string exception = task.Exception.ToString().ToLower();
                        if (exception.Contains("not found") || exception.Contains("not exist"))
                            pNotFoundFile.Raise();
                        else
                            pFailed.Raise();
                    }
                    else
                        pFailed.Raise();
                }
                else
                    pFailed.Raise();
            }, pStoDef);
        }

        //=== Firebase Crashlytics

        public static void Log(string message) => RCrashlytics.Log(message);
        public static void LogException(Exception exception) => RCrashlytics.LogException(exception);
        public static void LogCustomKey(string key, string value) => RCrashlytics.SetCustomKey(key, value);
        public static void LogUserId(string identifier) => RCrashlytics.SetUserId(identifier);

        #endregion

        //==============================================

        #region Private

        #endregion

        //==============================================

        #region Editor

#if UNITY_EDITOR

        [CustomEditor(typeof(RFirebaseManager))]
        private class RFirebaseManagerEditor : UnityEditor.Editor
        {
            private RFirebaseManager mScript;
            private List<string> mCurDirectives;
            private List<string> mDirectives;
            private List<string> mDisplayNames;
            private List<bool> mSelectedDirectives;

            private void OnEnable()
            {
                mScript = (RFirebaseManager)target;

                var taget = EditorUserBuildSettings.selectedBuildTargetGroup;
                string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(taget);
                mCurDirectives = directivesStr.Split(';').ToList();

                mDirectives = new List<string>()
                {
                    "ACTIVE_FIREBASE_ANALYTICS",
                    "ACTIVE_FIREBASE_STORAGE",
                    "ACTIVE_FIREBASE_DATABASE",
                    "ACTIVE_FIREBASE_AUTH",
                    "ACTIVE_FIREBASE_CRASHLYTICS",
                    "ACTIVE_FIREBASE_MESSAGING",
                    "ACTIVE_FIREBASE_REMOTE",
                };
                mDisplayNames = new List<string>()
                {
                    "Firebase Analytics",
                    "Firebase Storage",
                    "Firebase Database",
                    "Firebase Auth",
                    "Firebase Crashlytics",
                    "Firebase Messaging",
                    "Firebase Remote Config",
                };
                mSelectedDirectives = new List<bool>(mDirectives.Count);
                for (int i = 0; i < mDirectives.Count; i++)
                {
                    if (mCurDirectives.Contains(mDirectives[i]))
                        mSelectedDirectives.Add(true);
                    else
                        mSelectedDirectives.Add(false);
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                GUILayout.BeginVertical("box");

                var selectedDirectives = new List<string>();
                var nonSelectedDirectives = new List<string>();
                for (int i = 0; i < mDirectives.Count; i++)
                {
                    mSelectedDirectives[i] = EditorHelper.Toggle(mSelectedDirectives[i], mDisplayNames[i], 160, 25);
                    if (mSelectedDirectives[i])
                        selectedDirectives.Add(mDirectives[i]);
                    else
                        nonSelectedDirectives.Add(mDirectives[i]);
                }
                if (EditorHelper.Button("Apply"))
                {
                    if (selectedDirectives.Count > 0)
                        selectedDirectives.Add("ACTIVE_FIREBASE");
                    else
                        EditorHelper.RemoveDirective("ACTIVE_FIREBASE");
                    EditorHelper.AddDirectives(selectedDirectives);
                    EditorHelper.RemoveDirective(nonSelectedDirectives);
                }

                GUILayout.EndVertical();
            }
        }

#endif

        #endregion

        //==============================================

        #region Internal class

        [System.Serializable]
        public class AnalyticEvent
        {
            public string name;
            public string[] paramNames;
            public string[] paramValuesStr;
            public float[] paramValuesNum;
            public AnalyticEvent(string pName) { name = pName; }
            public AnalyticEvent(string pName, string pParamNames, float pPramValues)
            {
                name = pName;
                paramNames = new string[1] { pParamNames };
                paramValuesNum = new float[1] { pPramValues };
            }
            public AnalyticEvent(string pName, string pParamNames, int pPramValues)
            {
                name = pName;
                paramNames = new string[1] { pParamNames };
                paramValuesNum = new float[1] { pPramValues };
            }
            public AnalyticEvent(string pName, string pParamNames, string pPramValues)
            {
                name = pName;
                paramNames = new string[1] { pParamNames };
                paramValuesStr = new string[1] { pPramValues };
            }
            public AnalyticEvent(string pName, string[] pParamNames, float[] pPramValues)
            {
                name = pName;
                paramNames = pParamNames;
                paramValuesNum = pPramValues;
            }
            public AnalyticEvent(string pName, string[] pParamNames, int[] pPramValues)
            {
                name = pName;
                paramNames = pParamNames;
                paramValuesNum = new float[pPramValues.Length];
                for (int i = 0; i < pPramValues.Length; i++)
                    paramValuesNum[i] = pPramValues[i];
            }
            public AnalyticEvent(string pName, string[] pParamNames, string[] pPramValues)
            {
                name = pName;
                paramNames = pParamNames;
                paramValuesStr = pPramValues;
            }
        }

        #endregion
    }
}