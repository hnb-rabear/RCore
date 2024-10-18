/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using System;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
#if FIREBASE
using Firebase;
using Firebase.Extensions;
#endif

#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif

namespace RCore.Service
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

		public static bool initialized { get; private set; }
		public static string userId { get; private set; }
		public static string userName { get; private set; }

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

#endregion

		//=============================================

#region Public

		public static void Init(Action<bool> pOnFinished)
		{
			if (initialized)
				return;

#if FIREBASE
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                Debug.Log("Firebase Status " + task.Result);
                if (task.Result == DependencyStatus.Available)
                {
                    RFirebaseAnalytics.Initialize();
                    RFirebaseAuth.Initialize();
                    RFirebaseStorage.Initialize();
                    RFirebaseDatabase.Initialize();
                    initialized = true;
                    pOnFinished.Raise(true);
                }
                else
                {
                    initialized = false;
                    pOnFinished.Raise(false);
                }
            });
#else
			initialized = false;
			pOnFinished.Raise(false);
#endif
		}

		public static void SetUser(string pUserId, string pUserName)
		{
			if (!initialized)
				return;

			userId = pUserId;
			userName = pUserName;

			RFirebaseAnalytics.SetUserId(pUserId);
			RFirebaseAnalytics.SetUserProperty("username", pUserName);
			RCrashlytics.SetUserId(pUserId);
		}

#endregion

		//==============================================

#region Editor

#if UNITY_EDITOR

		[CustomEditor(typeof(RFirebaseManager))]
		private class RFirebaseManagerEditor : UnityEditor.Editor
		{
			private List<string> m_CurDirectives;
			private List<string> m_Directives;
			private List<string> m_DisplayNames;
			private List<bool> m_SelectedDirectives;

			private void OnEnable()
			{
				var target = EditorUserBuildSettings.selectedBuildTargetGroup;
				string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
				m_CurDirectives = directivesStr.Split(';').ToList();

				m_Directives = new List<string>()
				{
					"FIREBASE",
					"FIREBASE_ANALYTICS",
					"FIREBASE_STORAGE",
					"FIREBASE_DATABASE",
					"FIREBASE_AUTH",
					"FIREBASE_CRASHLYTICS",
					"FIREBASE_MESSAGING",
					"FIREBASE_REMOTE",
					"FIREBASE_FIRESTORE"
				};
				m_DisplayNames = new List<string>()
				{
					"Firebase",
					"Firebase Analytics",
					"Firebase Storage",
					"Firebase Database",
					"Firebase Auth",
					"Firebase Crashlytics",
					"Firebase Messaging",
					"Firebase Remote Config",
					"Firebase Firestore"
				};
				m_SelectedDirectives = new List<bool>(m_Directives.Count);
				for (int i = 0; i < m_Directives.Count; i++)
				{
					m_SelectedDirectives.Add(m_CurDirectives.Contains(m_Directives[i]));
				}
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				GUILayout.BeginVertical("box");

				var selectedDirectives = new List<string>();
				var nonSelectedDirectives = new List<string>();
				for (int i = 0; i < m_Directives.Count; i++)
				{
					m_SelectedDirectives[i] = EditorHelper.Toggle(m_SelectedDirectives[i], m_DisplayNames[i], 160, 25);
					if (m_SelectedDirectives[i])
						selectedDirectives.Add(m_Directives[i]);
					else
						nonSelectedDirectives.Add(m_Directives[i]);
				}
				if (EditorHelper.Button("Apply"))
				{
					if (selectedDirectives.Count > 0)
						selectedDirectives.Add("FIREBASE");
					else
						EditorHelper.RemoveDirective("FIREBASE");
					EditorHelper.AddDirectives(selectedDirectives);
					EditorHelper.RemoveDirective(nonSelectedDirectives);
				}

				GUILayout.EndVertical();
			}
		}

#endif

#endregion
	}
}