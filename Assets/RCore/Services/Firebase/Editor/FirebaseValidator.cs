using RCore.Editor;
using RCore.Service;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace RCore.Services.Firebase.Editor
{
	/// <summary>
	/// Automatically handles Firebase directives based on available classes and SDKs.
	/// </summary>
	[InitializeOnLoad]
	public static class FirebaseValidator
	{
		private const string MENU_ITEM = "Directives/Toggle Firebase Directives Validator";
		private static REditorPrefBool m_Active;

		static FirebaseValidator()
		{
			m_Active = new REditorPrefBool(MENU_ITEM);
			EditorApplication.update += RunOnEditorStart;
		}

		private static void RunOnEditorStart()
		{
			if (m_Active.Value)
				Validate();
		}

		private static void Validate()
		{
			Validate_FIREBASE_ANALYTICS();
			Validate_FIREBASE_CRASHLYTICS();
			Validate_FIREBASE_REMOTE_CONFIG();
			Validate_FIREBASE_AUTH();
			Validate_FIREBASE_MESSAGING();
			Validate_FIREBASE_FIRESTORE();
			Validate_FIREBASE_DATABASE();
			Validate_FIREBASE_STORAGE();
			Validate_FIREBASE_INSTALLATION();
		}

		private static void Validate_FIREBASE_ANALYTICS()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseAnalyticsType = Type.GetType("Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics");
			if (firebaseAnalyticsType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_ANALYTICS", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_ANALYTICS");
		}

		private static void Validate_FIREBASE_CRASHLYTICS()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseCrashlyticsType = Type.GetType("Firebase.Crashlytics.Crashlytics, Firebase.Crashlytics");
			if (firebaseCrashlyticsType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_CRASHLYTICS", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_CRASHLYTICS");
		}

		private static void Validate_FIREBASE_REMOTE_CONFIG()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseRemoteConfigType = Type.GetType("Firebase.RemoteConfig.FirebaseRemoteConfig, Firebase.RemoteConfig");
			if (firebaseRemoteConfigType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_REMOTE_CONFIG", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_REMOTE_CONFIG");
		}

		private static void Validate_FIREBASE_AUTH()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseAuthType = Type.GetType("Firebase.Auth.FirebaseAuth, Firebase.Auth");
			if (firebaseAuthType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_AUTH", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_AUTH");
		}

		private static void Validate_FIREBASE_FIRESTORE()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseFirestoreType = Type.GetType("Firebase.Firestore.FirebaseFirestore, Firebase.Firestore");
			if (firebaseFirestoreType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_FIRESTORE", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_FIRESTORE");
		}

		private static void Validate_FIREBASE_DATABASE()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseDatabaseType = Type.GetType("Firebase.Database.FirebaseDatabase, Firebase.Database");
			if (firebaseDatabaseType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_DATABASE", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_DATABASE");
		}

		private static void Validate_FIREBASE_STORAGE()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseStorageType = Type.GetType("Firebase.Storage.FirebaseStorage, Firebase.Storage");
			if (firebaseStorageType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_STORAGE", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_STORAGE");
		}
		
		private static void Validate_FIREBASE_INSTALLATION()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseInstallationsType = Type.GetType("Firebase.Installations.FirebaseInstallations, Firebase.Installations");
			if (firebaseInstallationsType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_INSTALLATION", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_INSTALLATION");
		}

		private static void Validate_FIREBASE_MESSAGING()
		{
			var rcore = IsClassAvailable("RCore.Service.RFirebase");
			var firebaseMessagingType = Type.GetType("Firebase.Messaging.FirebaseMessaging, Firebase.Messaging");
			if (firebaseMessagingType != null && rcore)
				EditorHelper.AddDirectives(new List<string> { "FIREBASE_MESSAGING", "FIREBASE" });
			else
				EditorHelper.RemoveDirective("FIREBASE_MESSAGING");
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

		[MenuItem(RMenu.R_TOOLS + MENU_ITEM)]
		private static void ToggleActive()
		{
			m_Active.Value = !m_Active.Value;
			if (m_Active.Value)
				Validate();
		}

		[MenuItem(RMenu.R_TOOLS + MENU_ITEM, true)]
		private static bool ToggleActiveValidate()
		{
			Menu.SetChecked(RMenu.R_TOOLS + MENU_ITEM, m_Active.Value);
			return true;
		}
	}
}