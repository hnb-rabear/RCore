/***
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com - 2019
 **/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class EnvProfilesCollection : ScriptableObject
	{
		private const string FILE_PATH = "Assets/Editor/EnvProfilesCollection.asset";

		public List<EnvSetting.Profile> profiles = new List<EnvSetting.Profile>();

		public static EnvProfilesCollection LoadOrCreateCollection()
		{
			var collection = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(EnvProfilesCollection)) as EnvProfilesCollection;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<EnvProfilesCollection>(FILE_PATH);
			return collection;
		}

		private void OnValidate()
		{
			if (profiles.Count == 0 || profiles[0].name != "do_not_remove")
			{
				profiles.Insert(0, new EnvSetting.Profile()
				{
					name = "do_not_remove",
				});
			}
		}
	}
}