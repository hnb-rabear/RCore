using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
	/// <summary>
	/// Hybrid audio collection
	/// Require Pre-setup in Json Data, Json data must be serialized List of SoundsCollection.Sound class
	/// It is suitable for game with many sounds and managed by Excel
	/// </summary>
	[CreateAssetMenu(fileName = "HybridAudioCollection", menuName = "RCore/Hybird Audio Collection")]
	public class HybridAudioCollection : ScriptableObject
	{
		[System.Serializable]
		public class Clip
		{
			public string fileName;
			public int id;
			public int limitNumber;
			public AudioClip clip;
		}

		private static HybridAudioCollection mInstance;
		public static HybridAudioCollection Instance
		{
			get
			{
				if (mInstance == null)
					mInstance = Resources.Load<HybridAudioCollection>(nameof(HybridAudioCollection));
				return mInstance;
			}
		}

		public List<Clip> SFXClips;
		public List<Clip> musicClips;

		public Clip GetClipById(int pId, bool pIsMusic, out int pIndex)
		{
			var list = pIsMusic ? musicClips : SFXClips;
			for (int i = 0; i < list.Count; i++)
			{
				var s = list[i];
				pIndex = i;
				if (s.id == pId)
					return s;
			}
			pIndex = -1;
			return null;
		}

		public Clip GetClipByIndex(int pIndex, bool pIsMusic)
		{
			var list = pIsMusic ? musicClips : SFXClips;
			if (pIndex >= list.Count || pIndex < 0)
				return null;
			return list[pIndex];
		}

		public Clip GetClip(string pName, bool pIsMusic, out int pIndex)
		{
			var list = pIsMusic ? musicClips : SFXClips;
			for (int i = 0; i < list.Count; i++)
			{
				var s = list[i];
				pIndex = i;
				if (s.fileName.ToLower() == pName.ToLower())
					return s;
			}
			pIndex = -1;
			return null;
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(HybridAudioCollection))]
		private class SoundsCollectionEditor : UnityEditor.Editor
		{
			private HybridAudioCollection mScript;

			private void OnEnable()
			{
				mScript = target as HybridAudioCollection;
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				GUILayout.Space(5);
				DrawCustom();
			}

			private void DrawCustom()
			{
				EditorHelper.BoxVertical(() =>
				{
					string musicsSourcePath = EditorHelper.FolderSelector("Musics Sources Path", "musicsSourcePath", Application.dataPath);
					string sfxsSourcePath = EditorHelper.FolderSelector("SFX Sources Path", "sfxsSourcePath", Application.dataPath);

					if (EditorHelper.Button("Build"))
					{
						musicsSourcePath = Application.dataPath + musicsSourcePath;
						sfxsSourcePath = Application.dataPath + sfxsSourcePath;

						var musicFiles = EditorHelper.GetObjects<AudioClip>(musicsSourcePath, "t:AudioClip");
						string musicsJsonData = Resources.Load<TextAsset>("Data/Musics")?.text;
						mScript.musicClips = JsonHelper.ToList<Clip>(musicsJsonData);
						if (mScript.musicClips != null)
							foreach (var sound in mScript.musicClips)
							{
								bool found = false;
								foreach (var clip in musicFiles)
								{
									if (sound.fileName == null) continue;
									if (sound.fileName.ToLower() == clip.name.ToLower())
									{
										found = true;
										sound.clip = clip;
										break;
									}
								}
								if (!found)
									UnityEngine.Debug.LogError("Not found music audio file " + sound.fileName);
							}

						var sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxsSourcePath, "t:AudioClip");
						string sFXsJsonData = Resources.Load<TextAsset>("Data/SFXs")?.text;
						mScript.SFXClips = JsonHelper.ToList<Clip>(sFXsJsonData);
						if (mScript.SFXClips != null)
							foreach (var sound in mScript.SFXClips)
							{
								bool found = false;
								foreach (var clip in sfxFiles)
								{
									if (sound.fileName == null) continue;
									if (sound.fileName.ToLower() == clip.name.ToLower())
									{
										found = true;
										sound.clip = clip;
										break;
									}
								}
								if (!found)
									UnityEngine.Debug.LogError("Not found sfx audio file " + sound.fileName);
							}

						if (GUI.changed)
						{
							EditorUtility.SetDirty(mScript);
							AssetDatabase.SaveAssets();
						}
					}
				}, ColorHelper.DarkMagenta, true);
			}
		}
#endif
	}
}