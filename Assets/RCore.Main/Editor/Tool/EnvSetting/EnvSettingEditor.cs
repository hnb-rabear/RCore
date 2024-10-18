/***
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com - 2019
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using RCore.Editor;
using System;
using EditorPrefs = UnityEditor.EditorPrefs;

namespace RCore.Editor.Tool
{
	[CustomEditor(typeof(EnvSetting))]
	public class EnvSettingEditor : UnityEditor.Editor
	{
		private EnvSetting m_target;

		//-- FIREBASE CONFIGURATION
		private static string FirebaseDevConfigPath { get => EditorPrefs.GetString("firebaseDevConfigPath"); set => EditorPrefs.SetString("firebaseDevConfigPath", value); }
		private static string FirebaseLiveConfigPath { get => EditorPrefs.GetString("firebaseLiveConfigPath"); set => EditorPrefs.SetString("firebaseLiveConfigPath", value); }
		private static string FirebaseConfigOutputFolder
		{
			get => EditorPrefs.GetString("FirebaseConfigOutputFolder", Application.dataPath);
			set => EditorPrefs.SetString("FirebaseConfigOutputFolder", value);
		}
		private static string FirebaseConfig
		{
			get => EditorPrefs.GetString("firebase_config_" + Application.productName);
			set => EditorPrefs.SetString("firebase_config_" + Application.productName, value);
		}
		private static string FirebaseProjectNumber { get => EditorPrefs.GetString("project_number"); set => EditorPrefs.SetString("project_number", value); }

		private string m_typedProfileName;
		private string m_selectedProfile;
		private EnvProfilesCollection m_envProfileCollections;
		private bool m_removingProfile;
		private bool m_previewingProfiles;
		private string m_buildName;
		private float m_lastUpdateTime;
		private readonly Dictionary<string, ReorderableList> m_reorderDirectivesDict = new Dictionary<string, ReorderableList>();

		private void OnEnable()
		{
			EditorApplication.update += UpdateEditor;
			
			m_target = EnvSetting.Instance;
			if (m_envProfileCollections == null)
				m_envProfileCollections = EnvProfilesCollection.LoadOrCreateCollection();
			m_removingProfile = false;
			m_selectedProfile = m_target.profile.name;
			m_typedProfileName = m_target.profile.name;

			m_removingProfile = false;
			SwitchMode(false);
			InitDirectives(m_target.profile);

			CheckFirebaseConfigPaths();
		}

		private void OnDisable()
		{
			EditorApplication.update -= UpdateEditor;
		}
		
		private void UpdateEditor()
		{
			float currentTime = Time.realtimeSinceStartup;

			// Check if at least 1 second has passed since the last update
			if (currentTime - m_lastUpdateTime >= 1f)
			{
				m_lastUpdateTime = currentTime;

				// Your logic to update the editor
				m_buildName = EditorHelper.GetBuildName();
			}
		}

		public override void OnInspectorGUI()
		{
			var tab = EditorHelper.Tabs("dev_setting_tabs", "Default", "Custom", "Firebase");
			GUILayout.Space(5);
			switch (tab)
			{
				case "Custom":
					DrawSettingsProfiles();
					break;
				case "Firebase":
					DrawFirebaseConfiguration();
					break;
				default:
					EditorHelper.BoxVertical(() => base.OnInspectorGUI(), default, true);
					break;
			}
			if (tab != "Custom")
			{
				m_removingProfile = false;
				SwitchMode(false);
			}
			GUILayout.Space(10);
			EditorHelper.BoxHorizontal(() =>
			{
				EditorHelper.TextArea(m_buildName, "Build Name", readOnly:true);
				if (EditorHelper.Button("Copy", 50))
					GUIUtility.systemCopyBuffer = m_buildName;
			});
			EditorHelper.BoxVertical(() =>
			{
				if (EditorHelper.ButtonColor("Save", Color.green))
				{
					EditorUtility.SetDirty(m_envProfileCollections);
					EditorUtility.SetDirty(m_target);
					AssetDatabase.SaveAssets();
				}
			}, Color.white, true);
		}

		//========= SETTINGS PROFILE

		private void DrawSettingsProfiles()
		{
			m_target.EnableLog = EditorHelper.Toggle(m_target.EnableLog, "Show Log", 120, 280);
			m_target.EnableDraw = EditorHelper.Toggle(m_target.EnableDraw, "Enable Draw", 120, 280);

			EditorHelper.BoxVertical("Project Settings" + (m_previewingProfiles ? " Preview" : ""), () =>
			{
				if (m_envProfileCollections != null && m_envProfileCollections.profiles.Count > 0 && m_envProfileCollections.profiles[0].name != "do_not_remove")
				{
					m_envProfileCollections.profiles.Insert(0, new EnvSetting.Profile()
					{
						name = "do_not_remove",
					});
				}

				if (!m_previewingProfiles)
				{
					DrawSettingsProfile(m_target.profile);
					GUILayout.Space(10);
					DrawProfilesSelection();
				}
				else
					DrawPreviewSettingsProfiles();
			}, Color.white, true);
		}

		private void InitDirectives(EnvSetting.Profile profile)
		{
			if (profile == null)
				return;
			string[] currentDefines = EditorHelper.GetDirectives();
			for (int i = 0; i < currentDefines.Length; i++)
				profile.AddDirective(currentDefines[i], true);

			for (int i = 0; i < profile.defines.Count; i++)
			{
				if (currentDefines.Length > 0)
				{
					bool exist = false;
					for (int j = 0; j < currentDefines.Length; j++)
					{
						if (currentDefines[j] == profile.defines[i].name)
							exist = true;
					}
					profile.defines[i].enabled = exist;
				}
				else
					profile.defines[i].enabled = false;
			}
		}

		private void DrawSettingsProfile(EnvSetting.Profile pProfile)
		{
			if (!m_previewingProfiles)
				EditorGUILayout.LabelField(pProfile.name, GUIStyleHelper.headerTitle);
			else
				pProfile.name = EditorHelper.TextField(pProfile.name, "Name", 120, 280);

			if (pProfile.defines != null)
			{
				string[] defaultDirectives = { "DEVELOPMENT", "UNITY_IAP", "ADDRESSABLES" };
				foreach (string directive in defaultDirectives)
					pProfile.AddDirective(directive, false);

				if (!m_reorderDirectivesDict.ContainsKey(pProfile.name))
				{
					var reorderList = new ReorderableList(pProfile.defines, typeof(EnvSetting.Directive), true, true, true, true);
					m_reorderDirectivesDict.TryAdd(pProfile.name, reorderList);
					reorderList.drawElementCallback = (rect, index, isActive, isFocused) =>
					{
						var define = pProfile.defines[index];
						GUI.backgroundColor = define.color;
						const float widthTog = 20;
						const float widthColor = 60;
						float widthName = rect.width - widthTog - widthColor - 10;
						define.enabled = EditorGUI.Toggle(new Rect(rect.x, rect.y, widthTog, 20), define.enabled);

						if (defaultDirectives.Contains(define.name))
							EditorGUI.LabelField(new Rect(rect.x + widthTog + 5, rect.y, widthName, 20), define.name);
						else
							define.name = EditorGUI.TextField(new Rect(rect.x + widthTog + 5, rect.y, widthName, 20), define.name);
						define.color = EditorGUI.ColorField(new Rect(rect.x + widthTog + 5 + widthName + 5, rect.y, widthColor, 20), define.color);
						GUI.backgroundColor = Color.white;
					};
					m_reorderDirectivesDict[pProfile.name].onCanRemoveCallback = (list) =>
					{
						var define = pProfile.defines[list.index];
						return !defaultDirectives.Contains(define.name);
					};
				}
				m_reorderDirectivesDict[pProfile.name].DoLayoutList();
				if (GUI.changed)
					pProfile.defines = (List<EnvSetting.Directive>)m_reorderDirectivesDict[pProfile.name].list;

				EditorGUILayout.BeginHorizontal();
				if (EditorHelper.Button("Apply"))
					ApplyDirectives(pProfile.defines);
				if (EditorHelper.Button("Clone"))
				{
					var cloneProfile = CloneProfile(pProfile);
					cloneProfile.name += "(clone)";
					m_envProfileCollections.profiles.Add(cloneProfile);
				}
				if (m_previewingProfiles && EditorHelper.ButtonColor("Remove", Color.red))
					m_envProfileCollections.profiles.Remove(pProfile);
				EditorGUILayout.EndHorizontal();
			}
		}

		private void DrawPreviewSettingsProfiles()
		{
			var profiles = m_envProfileCollections.profiles;
			if (profiles.Count == 0)
				return;

			for (int i = 0; i < profiles.Count; i++)
			{
				if (EditorHelper.HeaderFoldout($"{profiles[i].name} ({profiles[i].defines.Count})", "PreviewProfile" + i))
				{
					EditorGUILayout.BeginVertical();
					DrawSettingsProfile(profiles[i]);
					EditorGUILayout.EndVertical();
				}
				if (i < profiles.Count - 1)
					EditorHelper.Separator();
			}
			EditorHelper.Separator();
			if (EditorHelper.ButtonColor("Back", Color.yellow))
				SwitchMode(false);
		}

		private void SwitchMode(bool pMode)
		{
			if (m_previewingProfiles == pMode)
				return;
			m_previewingProfiles = pMode;
			m_reorderDirectivesDict.Clear();
		}

		private static void ApplyDirectives(List<EnvSetting.Directive> defines)
		{
			string symbols = string.Join(";", defines
				.Where(d => d.enabled)
				.Select(d => d.name)
				.ToArray());
			var target = EditorUserBuildSettings.selectedBuildTargetGroup;
			PlayerSettings.SetScriptingDefineSymbolsForGroup(target, symbols);
		}

		private void DrawProfilesSelection()
		{
			EditorHelper.BoxVertical(() =>
			{
				var profiles = m_envProfileCollections.profiles ?? new List<EnvSetting.Profile>();

				EditorHelper.BoxHorizontal(() =>
				{
					m_typedProfileName = EditorHelper.TextField(m_typedProfileName, "Profile Name");
					if (EditorHelper.ButtonColor("Save Profile", Color.green))
					{
						if (string.IsNullOrEmpty(m_typedProfileName))
							return;

						int index = -1;
						for (int i = 0; i < profiles.Count; i++)
						{
							if (profiles[i].name == m_typedProfileName.Trim())
							{
								profiles[i] = CloneProfile(m_target.profile);
								index = i;
								break;
							}
						}
						if (index == -1)
						{
							var newProfile = CloneProfile(m_target.profile);
							newProfile.name = m_typedProfileName;
							profiles.Add(newProfile);
						}

						m_selectedProfile = m_typedProfileName;

						ApplyProfile(m_selectedProfile, false);
					}
				}, Color.yellow);

				string[] allProfileNames = new string[profiles.Count];
				for (int i = 0; i < profiles.Count; i++)
					allProfileNames[i] = profiles[i].name;

				if (allProfileNames.Length > 0)
					EditorHelper.BoxHorizontal(() =>
					{
						var preSelectedProfile = m_selectedProfile;
						m_selectedProfile = EditorHelper.DropdownList(m_selectedProfile, "Profiles", allProfileNames);
						if (m_removingProfile)
						{
							if (EditorHelper.ButtonColor("Remove", Color.red))
							{
								for (int i = 0; i < profiles.Count; i++)
									if (profiles[i].name == m_selectedProfile)
									{
										profiles.RemoveAt(i);
										break;
									}
								m_removingProfile = false;
							}
							if (EditorHelper.ButtonColor("Cancel", Color.green))
								m_removingProfile = false;
						}
						else
						{
							if (EditorHelper.ButtonColor("Preview", Color.yellow))
								SwitchMode(true);
							if (EditorHelper.ButtonColor("Apply", Color.green))
								ApplyProfile(m_selectedProfile);
						}

						if (m_selectedProfile != preSelectedProfile)
							ApplyProfile(m_selectedProfile, false);
					});
			}, Color.cyan, true);
		}

		private void ApplyProfile(string pName, bool pApplyDirectives = true)
		{
			var profiles = m_envProfileCollections.profiles;
			for (int i = 0; i < profiles.Count; i++)
				if (profiles[i].name == pName)
				{
					m_target.profile = CloneProfile(profiles[i]);
					m_typedProfileName = m_target.profile.name;
					m_removingProfile = false;
					if (pApplyDirectives)
						ApplyDirectives(m_target.profile.defines);
					break;
				}
		}

		private static EnvSetting.Profile CloneProfile(EnvSetting.Profile pProfile)
		{
			var toJson = JsonUtility.ToJson(pProfile);
			var fromJson = JsonUtility.FromJson<EnvSetting.Profile>(toJson);
			return fromJson;
		}

		//========== FIREBASE

		private void DrawFirebaseConfiguration()
		{
			EditorHelper.BoxVertical("Firebase", () =>
			{
				FirebaseDevConfigPath = EditorHelper.FileSelector("Dev Config Fire", "FirebaseDevConfigPath" + GetInstanceID(), "json,txt");
				FirebaseLiveConfigPath = EditorHelper.FileSelector("Live Config Fire", "FirebaseLiveConfigPath" + GetInstanceID(), "json,txt");
				FirebaseConfigOutputFolder = EditorHelper.FolderSelector("Output Folder", "FirebaseConfigOutputFolder" + GetInstanceID());

				string testPath = Application.dataPath + FirebaseDevConfigPath;
				string livePath = Application.dataPath + FirebaseLiveConfigPath;
				string destination = Application.dataPath + FirebaseConfigOutputFolder + "/google-services.json";
				string curProjectNumber = FirebaseProjectNumber;

				EditorHelper.BoxHorizontal(() =>
				{
					if (livePath == testPath && livePath != "")
					{
						EditorGUILayout.HelpBox("Live and Test Path must not be the same!", MessageType.Warning);
						return;
					}

					EditorHelper.Button("Use Live Config", () =>
					{
						var theSourceFile = new FileInfo(livePath);
						using var reader = theSourceFile.OpenText();
						string content = reader.ReadToEnd();
						var contentNode = SimpleJSON.JSON.Parse(content);
						var projectNumberNode = contentNode["project_info"]["project_number"];
						if (curProjectNumber == projectNumberNode)
							return;
						curProjectNumber = projectNumberNode;
						File.WriteAllText(destination, content);
						FirebaseConfig = content;
						FirebaseProjectNumber = curProjectNumber;
						AssetDatabase.Refresh();
						EditorApplication.ExecuteMenuItem("Assets/Play Services Resolver/Android Resolver/Force Resolve");
						EditorApplication.ExecuteMenuItem("Assets/External Dependency Manager/Android Resolver/Force Resolve");
					});
					EditorHelper.Button("Use Dev Config", () =>
					{
						var theSourceFile = new FileInfo(testPath);
						using var reader = theSourceFile.OpenText();
						string content = reader.ReadToEnd();
						var contentNode = SimpleJSON.JSON.Parse(content);
						var projectNumberNode = contentNode["project_info"]["project_number"];
						if (curProjectNumber == projectNumberNode)
							return;
						curProjectNumber = projectNumberNode;
						File.WriteAllText(destination, content);
						FirebaseConfig = content;
						FirebaseProjectNumber = curProjectNumber;
						AssetDatabase.Refresh();
						EditorApplication.ExecuteMenuItem("Assets/Play Services Resolver/Android Resolver/Force Resolve");
						EditorApplication.ExecuteMenuItem("Assets/External Dependency Manager/Android Resolver/Force Resolve");
					});
				});
				string currentConfig = FirebaseConfig;
				if (string.IsNullOrEmpty(currentConfig))
				{
					var sourceFile = new FileInfo(destination);
					try
					{
						using var reader = sourceFile.OpenText();
						currentConfig = reader.ReadToEnd();
						FirebaseConfig = currentConfig;
					}
					catch
					{
						FirebaseConfig = "firebase did not config";
					}
				}
				EditorHelper.BoxVertical(() =>
				{
					EditorGUILayout.LabelField("Preview...", EditorStyles.boldLabel);
					EditorGUILayout.LabelField(currentConfig, GUILayout.MaxHeight(600), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 50));
				}, Color.white, true);
			}, default, true);
		}

		private static void CheckFirebaseConfigPaths()
		{
			string devFilePath = Application.dataPath + "/" + FirebaseDevConfigPath;
			string liveFilePath = Application.dataPath + "/" + FirebaseLiveConfigPath;
			string outputFolder = Application.dataPath + "/" + FirebaseConfigOutputFolder;
			if (!Directory.Exists(outputFolder))
				FirebaseConfigOutputFolder = FirebaseConfigOutputFolder.Contains("Assets") ? EditorHelper.FormatPathToUnityPath(FirebaseConfigOutputFolder).Replace("Assets/", "") : "";
			if (!File.Exists(devFilePath))
				FirebaseDevConfigPath = FirebaseDevConfigPath.Contains("Assets") ? EditorHelper.FormatPathToUnityPath(FirebaseDevConfigPath).Replace("Assets/", "") : "";
			if (!File.Exists(liveFilePath))
				FirebaseLiveConfigPath = FirebaseLiveConfigPath.Contains("Assets") ? EditorHelper.FormatPathToUnityPath(FirebaseLiveConfigPath).Replace("Assets/", "") : "";
		}
	}
}