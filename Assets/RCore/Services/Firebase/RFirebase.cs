﻿/***
 * Author HNB-RaBear - 2019
 **/

using System;
#if FIREBASE
using Firebase;
using Firebase.Extensions;
#endif

namespace RCore.Service
{
	public static class RFirebase
	{
		public static bool Initialized;

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
                    RFirebaseAnalytics.Initialize();
                    RFirebaseAuth.Initialize();
                    RFirebaseStorage.Initialize();
                    RFirebaseDatabase.Initialize();
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
	}
}