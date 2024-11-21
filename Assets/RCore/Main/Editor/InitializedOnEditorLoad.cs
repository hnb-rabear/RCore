using System.Globalization;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Editor
{
	[InitializeOnLoad]
	public static class InitializedOnEditorLoad
	{
		static InitializedOnEditorLoad()
		{
			EditorApplication.update += RunOnEditorStart;
		}

		private static void RunOnEditorStart()
		{
			EditorApplication.update -= RunOnEditorStart;

			var culture = CultureInfo.CreateSpecificCulture("en-US");
			var dateTimeFormat = new DateTimeFormatInfo
			{
				ShortDatePattern = "dd-MM-yyyy",
				LongTimePattern = "HH:mm:ss",
				ShortTimePattern = "HH:mm"
			};
			culture.DateTimeFormat = dateTimeFormat;
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;

			Validate_DOTWEEN();
			Validate_ADDRESSABLES();
			Validate_UNITY_IAP();
			Validate_GPGS();
			Validate_IN_APP_REVIEW();
			Validate_IN_APP_UPDATE();
			Validate_APPLOVIN();
			Validate_FIREBASE_ANALYTICS();
			Validate_FIREBASE_CRASHLYTICS();
			Validate_FIREBASE_REMOTE_CONFIG();
			Validate_FIREBASE_AUTH();
			Validate_FIREBASE_MESSAGING();
			Validate_FIREBASE_FIRESTORE();
			Validate_FIREBASE_DATABASE();
			Validate_FIREBASE_STORAGE();
		}

		private static bool IsNamespaceAvailable(string namespaceName)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				foreach (var type in types)
					if (type.Namespace == namespaceName)
						return true;
			}
			return false;
		}

		private static bool IsClassAvailable(string className)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var type = assembly.GetType(className);
				if (type != null)
					return true;
			}
			return false;
		}

		private static void Validate_DOTWEEN()
		{
			var dotweenType = Type.GetType("DG.Tweening.DOTween, DOTween");
			if (dotweenType != null)
				EditorHelper.AddDirective("DOTWEEN");
			else
				EditorHelper.RemoveDirective("DOTWEEN");
		}

		private static void Validate_ADDRESSABLES()
		{
			var addressablesType = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
			if (addressablesType != null)
				EditorHelper.AddDirective("ADDRESSABLES");
			else
				EditorHelper.RemoveDirective("ADDRESSABLES");
		}

		private static void Validate_UNITY_IAP()
		{
			var rcore = IsClassAvailable("RCore.Service.IAPManager");
			var iapType = Type.GetType("UnityEngine.Purchasing.ConfigurationBuilder, UnityEngine.Purchasing");
			if (iapType != null && rcore)
				EditorHelper.AddDirective("UNITY_IAP");
			else
				EditorHelper.RemoveDirective("UNITY_IAP");
		}

		private static void Validate_GPGS()
		{
			var rcore = IsClassAvailable("RCore.Service.GameServices");
			var gpgsType = Type.GetType("GooglePlayGames.PlayGamesPlatform, Google.Play.Games");
			if (gpgsType != null && rcore)
				EditorHelper.AddDirective("GPGS");
			else
				EditorHelper.RemoveDirective("GPGS");
		}

		private static void Validate_IN_APP_REVIEW()
		{
			var rcore = IsClassAvailable("RCore.Service.GameServices");
			var inAppReviewType = Type.GetType("Google.Play.Review.ReviewManager, Google.Play.Review");
			if (inAppReviewType != null && rcore)
				EditorHelper.AddDirective("IN_APP_REVIEW");
			else
				EditorHelper.RemoveDirective("IN_APP_REVIEW");
		}

		private static void Validate_IN_APP_UPDATE()
		{
			var rcore = IsClassAvailable("RCore.Service.GameServices");
			var inAppUpdateType = Type.GetType("Google.Play.AppUpdate.AppUpdateManager, Google.Play.AppUpdate");
			if (inAppUpdateType != null && rcore)
				EditorHelper.AddDirective("IN_APP_UPDATE");
			else
				EditorHelper.RemoveDirective("IN_APP_UPDATE");
		}

		private static void Validate_APPLOVIN()
		{
			var rcore = IsClassAvailable("RCore.Service.ApplovinProvider");
			var appLovinType = Type.GetType("MaxSdk, MaxSdk.Scripts");
			if (appLovinType != null && rcore)
				EditorHelper.AddDirective("MAX");
			else
				EditorHelper.RemoveDirective("MAX");
		}

		private static void Validate_FIREBASE_ANALYTICS()
		{
			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseAnalyticsType = Type.GetType("Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics");
			if (firebaseAnalyticsType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_ANALYTICS", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_ANALYTICS");
		}

		private static void Validate_FIREBASE_CRASHLYTICS()
		{
			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseCrashlyticsType = Type.GetType("Firebase.Crashlytics.Crashlytics, Firebase.Crashlytics");
			if (firebaseCrashlyticsType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_CRASHLYTICS", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_CRASHLYTICS");
		}

		private static void Validate_FIREBASE_REMOTE_CONFIG()
		{

			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseRemoteConfigType = Type.GetType("Firebase.RemoteConfig.FirebaseRemoteConfig, Firebase.RemoteConfig");
			if (firebaseRemoteConfigType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_REMOTE_CONFIG", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_REMOTE_CONFIG");
		}

		private static void Validate_FIREBASE_AUTH()
		{
			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseAuthType = Type.GetType("Firebase.Auth.FirebaseAuth, Firebase.Auth");
			if (firebaseAuthType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_AUTH", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_AUTH");
		}

		private static void Validate_FIREBASE_FIRESTORE()
		{
			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseFirestoreType = Type.GetType("Firebase.Firestore.FirebaseFirestore, Firebase.Firestore");
			if (firebaseFirestoreType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_FIRESTORE", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_FIRESTORE");
		}

		private static void Validate_FIREBASE_DATABASE()
		{
			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseDatabaseType = Type.GetType("Firebase.Database.FirebaseDatabase, Firebase.Database");
			if (firebaseDatabaseType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_DATABASE", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_DATABASE");
		}

		private static void Validate_FIREBASE_STORAGE()
		{
			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseStorageType = Type.GetType("Firebase.Storage.FirebaseStorage, Firebase.Storage");
			if (firebaseStorageType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_STORAGE", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_STORAGE");
		}

		private static void Validate_FIREBASE_MESSAGING()
		{
			var rcore = IsClassAvailable("RCore.Service.FirebaseManager");
			var firebaseMessagingType = Type.GetType("Firebase.Messaging.FirebaseMessaging, Firebase.Messaging");
			if (firebaseMessagingType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_MESSAGING", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_MESSAGING");
		}
	}
}