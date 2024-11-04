/***
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com - 2019
 **/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class ConfigurationCollection : ScriptableObject
	{
		private const string FILE_PATH = "Assets/Editor/ConfigurationCollection.asset";

		public List<Configuration.Profile> profiles = new List<Configuration.Profile>();

		public static ConfigurationCollection Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(ConfigurationCollection)) as ConfigurationCollection;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<ConfigurationCollection>(FILE_PATH);
			return collection;
		}

		private void OnValidate()
		{
			if (profiles.Count == 0 || profiles[0].name != "do_not_remove")
			{
				profiles.Insert(0, new Configuration.Profile()
				{
					name = "do_not_remove",
				});
			}
		}
		
		public static List<string> BuiltinFeatures = new List<string>()
		{
			"DOTWEEN",
			"GPGS",
			"IN_APP_REVIEW",
			"IN_APP_UPDATE",
			"APPLOVINE",
			"IRONSOURCE",
			"FIREBASE_ANALYTICS",
			// "FIREBASE_STORAGE",
			// "FIREBASE_DATABASE",
			"FIREBASE_AUTH",
			"FIREBASE_CRASHLYTICS",
			// "FIREBASE_MESSAGING",
			"FIREBASE_REMOTE",
			// "FIREBASE_FIRESTORE"
		};
	}
}