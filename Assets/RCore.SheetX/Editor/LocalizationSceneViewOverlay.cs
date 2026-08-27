/***
 * Scene View overlay for switching localization language at edit time.
 * Discovers generated localization classes via reflection since
 * RCore.SheetX.Editor.asmdef cannot reference Assembly-CSharp.
 ***/

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RCore.SheetX.Editor
{
	[Overlay(typeof(SceneView), "rcore-sheetx-localization", "Localization", true)]
	internal sealed class LocalizationSceneViewOverlay : IMGUIOverlay
	{
		private struct LocalizationClassInfo
		{
			public Type type;
			public FieldInfo languageFilesField;
			public PropertyInfo currentLanguageProperty;
			public MethodInfo initInEditorMethod;
			public FieldInfo languageTempField;
		}

		private struct TextComponentInfo
		{
			public Type type;
			public MethodInfo refreshMethod;
		}

		private static List<LocalizationClassInfo> s_localizationClasses = new List<LocalizationClassInfo>();
		private static List<TextComponentInfo> s_textComponents = new List<TextComponentInfo>();
		private static string[] s_languageKeys = Array.Empty<string>();
		private static bool s_cacheBuilt;

		[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			s_cacheBuilt = false;
		}

		private static void RebuildCache()
		{
			s_localizationClasses.Clear();
			s_textComponents.Clear();
			// Ordered union: the arrows step through languages in LanguageFiles declaration
			// order, so a HashSet (unspecified enumeration order) is not usable here.
			var languageKeys = new List<string>();

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try { types = assembly.GetTypes(); }
				catch (ReflectionTypeLoadException ex) { types = ex.Types; }

				if (types == null)
					continue;

				foreach (var type in types)
				{
					if (type == null || !type.IsAbstract || !type.IsSealed) // static classes are abstract+sealed
						continue;

					var langFilesField = type.GetField("LanguageFiles",
						BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
					if (langFilesField == null || langFilesField.FieldType != typeof(Dictionary<string, string>))
						continue;

					var curLangProp = type.GetProperty("CurrentLanguage",
						BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
					if (curLangProp == null || curLangProp.PropertyType != typeof(string))
						continue;

					var initMethod = type.GetMethod("InitInEditor",
						BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
						null, Type.EmptyTypes, null);
					if (initMethod == null)
						continue;

					var langTempField = type.GetField("m_LanguageTemp",
						BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

					s_localizationClasses.Add(new LocalizationClassInfo
					{
						type = type,
						languageFilesField = langFilesField,
						currentLanguageProperty = curLangProp,
						initInEditorMethod = initMethod,
						languageTempField = langTempField,
					});

					// Collect language keys
					var dict = langFilesField.GetValue(null) as Dictionary<string, string>;
					if (dict != null)
					{
						foreach (var key in dict.Keys)
						{
							if (!languageKeys.Contains(key))
								languageKeys.Add(key);
						}
					}

					// Find matching text component: {ClassName}Text with Refresh()
					var textTypeName = type.Name + "Text";
					foreach (var textType in types)
					{
						if (textType == null || textType.Name != textTypeName || textType.Namespace != type.Namespace)
							continue;
						if (!typeof(MonoBehaviour).IsAssignableFrom(textType))
							continue;

						var refreshMethod = textType.GetMethod("Refresh",
							BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
							null, Type.EmptyTypes, null);
						if (refreshMethod == null)
							continue;

						s_textComponents.Add(new TextComponentInfo
						{
							type = textType,
							refreshMethod = refreshMethod,
						});
						break;
					}
				}
			}

			s_languageKeys = languageKeys.ToArray();
			s_cacheBuilt = true;
		}

		public override void OnGUI()
		{
			if (!s_cacheBuilt)
				RebuildCache();

			if (s_localizationClasses.Count == 0)
			{
				GUILayout.Label("No localization class found");
				return;
			}

			if (s_languageKeys.Length == 0)
			{
				GUILayout.Label("No language found");
				return;
			}

			// Read current language from first discovered class
			string currentLang = s_localizationClasses[0].currentLanguageProperty.GetValue(null) as string ?? "";
			int currentIndex = Array.IndexOf(s_languageKeys, currentLang);
			if (currentIndex < 0)
				currentIndex = 0;

			EditorGUILayout.BeginHorizontal();

			// Previous button
			if (GUILayout.Button("<", GUILayout.Width(24)))
			{
				int newIndex = (currentIndex - 1 + s_languageKeys.Length) % s_languageKeys.Length;
				SwitchLanguage(s_languageKeys[newIndex]);
			}

			// Dropdown
			EditorGUI.BeginChangeCheck();
			int selected = EditorGUILayout.Popup(currentIndex, s_languageKeys, GUILayout.Width(64));
			if (EditorGUI.EndChangeCheck() && selected != currentIndex)
			{
				SwitchLanguage(s_languageKeys[selected]);
			}

			// Next button
			if (GUILayout.Button(">", GUILayout.Width(24)))
			{
				int newIndex = (currentIndex + 1) % s_languageKeys.Length;
				SwitchLanguage(s_languageKeys[newIndex]);
			}

			EditorGUILayout.EndHorizontal();
		}

		private static void SwitchLanguage(string language)
		{
			// Language keys are a union. Set shared PlayerPrefs through one class that
			// supports the requested key before asking every class to reload.
			foreach (var info in s_localizationClasses)
			{
				var languageFiles = info.languageFilesField.GetValue(null) as Dictionary<string, string>;
				if (languageFiles == null || !languageFiles.ContainsKey(language))
					continue;

				try { info.currentLanguageProperty.SetValue(null, language); }
				catch (Exception ex) { Debug.LogException(ex); }
				break;
			}

			foreach (var info in s_localizationClasses)
			{
				try
				{
					// Clear m_LanguageTemp to force InitInEditor() to reload.
					if (info.languageTempField != null)
						info.languageTempField.SetValue(null, null);
					info.initInEditorMethod.Invoke(null, null);
				}
				catch (Exception ex) { Debug.LogException(ex); }
			}

			RefreshTextComponents();
			SceneView.RepaintAll();
		}

		private static void RefreshTextComponents()
		{
			if (s_textComponents.Count == 0)
				return;

			// Collect all root GameObjects from loaded scenes
			var roots = new List<GameObject>();
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded)
					roots.AddRange(scene.GetRootGameObjects());
			}

			// Prefab Stage
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null && prefabStage.prefabContentsRoot != null)
				roots.Add(prefabStage.prefabContentsRoot);

			// Scan for text components and invoke Refresh
			foreach (var root in roots)
			{
				var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
				foreach (var behaviour in behaviours)
				{
					if (behaviour == null)
						continue;
					var behaviourType = behaviour.GetType();
					foreach (var textInfo in s_textComponents)
					{
						if (behaviourType == textInfo.type)
						{
							try { textInfo.refreshMethod.Invoke(behaviour, null); }
							catch (Exception ex) { Debug.LogException(ex); }
							break;
						}
					}
				}
			}
		}
	}
}
