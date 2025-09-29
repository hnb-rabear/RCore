/***
 * Author HNB-RaBear - 2019
 **/

using System;
using UnityEngine;
#if FIREBASE
using Firebase;
using Firebase.Extensions;
#endif

#if FIREBASE_INSTALLATION
using Firebase.Installations;
#endif

namespace RCore.Service
{
	public static class RFirebase
	{
		public static bool Initialized;
		public static string InstallationID;

		public static void Init(Action<bool> pOnFinished)
		{
			if (Initialized)
				return;

#if FIREBASE
			FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
			{
				Debug.Log("Firebase Status " + task.Result);
				if (task.Result == DependencyStatus.Available)
				{
					RFirebaseAnalytics.Init();
					RFirebaseAuth.Init();
					RFirebaseStorage.Init();
					RFirebaseDatabase.Init();
					RFirebaseFirestore.Init();
					InitializeInstallation();
					Initialized = true;
					pOnFinished.Raise(true);
				}
				else
				{
					Initialized = false;
					pOnFinished.Raise(false);
				}
			});
#else
			Initialized = false;
			pOnFinished.Raise(false);
#endif
		}

		private static async void InitializeInstallation()
		{
#if FIREBASE_INSTALLATION
			var firebaseInstallations = FirebaseInstallations.GetInstance(FirebaseApp.DefaultInstance);
			InstallationID = await firebaseInstallations.GetIdAsync();
#endif
		}
	}
}