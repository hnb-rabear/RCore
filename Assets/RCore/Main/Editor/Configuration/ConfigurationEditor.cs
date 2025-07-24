/***
 * Author HNB-RaBear - 2019
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using EditorPrefs = UnityEditor.EditorPrefs;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Custom editor for the Configuration ScriptableObject.
	/// This editor provides a user-friendly interface for managing build environments (scripting define symbols)
	/// and for switching between different Firebase configurations (development vs. live).
	/// </summary>
	[CustomEditor(typeof(Configuration))]
	public class ConfigurationEditor : UnityEditor.Editor
	{
		private Configuration m_target;
		private const string DEFAULT_ENV_NAME = "do_not_remove";

		//-- FIREBASE CONFIGURATION (paths are stored in EditorPrefs to persist between sessions)
		private static string FirebaseDevConfigPath { get => EditorPrefs.GetString("firebaseDevConfigPath"); set => EditorPrefs.SetString("firebaseDevConfigPath", value); }
		private static string FirebaseLiveConfigPath { get => EditorPrefs.GetString("firebaseLiveConfigPath"); set => EditorPrefs.SetString("firebaseLiveConfigPath", value); }
		private static string FirebaseConfigOutputFolder { get => EditorPrefs.GetString("FirebaseConfigOutputFolder", Application.dataPath); set => EditorPrefs.SetString("FirebaseConfigOutputFolder", value); }
		private static string FirebaseConfig { get => EditorPrefs.GetString("firebase_config_" + Application.productName); set => EditorPrefs.SetString("firebase_config_" + Application.productName, value); }
		private static string FirebaseProjectNumber { get => EditorPrefs.GetString("project_number"); set => EditorPrefs.SetString("project_number", value); }

		private string m_typedEnvName;
		private string m_selectedEnv;
		private bool m_previewingEnvs;
		private string m_buildName;
		private float m_lastUpdateTime;
		private readonly Dictionary<string, ReorderableList> m_reorderDirectivesDict = new Dictionary<string, ReorderableList>();

		private void OnEnable()
		{
			EditorApplication.update += UpdateEditor;

			m_target = Configuration.Instance;
			m_selectedEnv = m_target.curEnv.name;
			m_typedEnvName = m_target.curEnv.name;

			EditEnv(false);
			InitEnv(m_target.curEnv);

			CheckFirebaseConfigPaths();
		}

		private void OnDisable()
		{
			EditorApplication.update -= UpdateEditor;
		}

		/// <summary>
		/// Periodically updates editor-specific information, like the build name,
		/// without causing constant repaints.
		/// </summary>
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

		/// <summary>
		/// Draws the main inspector GUI with a tabbed interface.
		/// </summary>
		public override void OnInspectorGUI()
		{
			var tab = EditorHelper.Tabs("dev_setting_tabs", "Default", "Envs Manager");
			GUILayout.Space(5);
			switch (tab)
			{
				case "Envs Manager":
					DrawTabPageEnv();
					break;
				case "Firebase":
					DrawTabPageFirebase();
					break;
				default:
					// Draw the default inspector for the Configuration object
					EditorHelper.BoxVertical(() => base.OnInspectorGUI(), default, true);
					break;
			}

			GUILayout.Space(10);
			if (m_previewingEnvs && EditorHelper.ButtonColor("Back", Color.yellow))
				EditEnv(false);
			if (EditorHelper.ButtonColor("Save", Color.green))
			{
				EditorUtility.SetDirty(m_target);
				AssetDatabase.SaveAssets();
			}
			GUILayout.Space(10);

			// Display the current build name with a copy button.
			GUILayout.BeginHorizontal();
			EditorHelper.TextArea(m_buildName, "Name of Build", readOnly: true);
			if (EditorHelper.Button("Copy", 50))
				GUIUtility.systemCopyBuffer = m_buildName;
			GUILayout.EndHorizontal();
		}

		/// <summary>
		/// Draws the UI for the "Envs Manager" tab, which handles scripting define symbols.
		/// </summary>
		private void DrawTabPageEnv()
		{
			EditorGUI.BeginChangeCheck();

			Debug.Enabled = EditorHelper.Toggle(Debug.Enabled, "Enable Log", 120, 280);
			DebugDraw.Enabled = EditorHelper.Toggle(DebugDraw.Enabled, "Enable Draw", 120, 280);

			if (!m_previewingEnvs)
			{
				// -- VIEW FOR EDITING THE CURRENT ENVIRONMENT --
				EditorHelper.BoxVertical(m_target.curEnv.name, () =>
				{
					GUILayout.Space(5);
					EditorGUILayout.BeginVertical("box");
					DrawEnv(m_target.curEnv);
					EditorGUILayout.EndVertical();
					DrawComboBoxEnvs(); // Show controls for loading/saving envs
				}, isBox: true);
			}
			else
				// -- VIEW FOR MANAGING ALL SAVED ENVIRONMENTS --
				EditorHelper.BoxVertical("Envs", () =>
				{
					var envs = m_target.envs;
					if (envs.Count == 0)
						return;

					for (int i = 0; i < envs.Count; i++)
					{
						if (EditorHelper.HeaderFoldout($"{envs[i].name} ({envs[i].directives.Count})", "PreviewEnv" + i))
						{
							EditorGUILayout.BeginVertical("box");
							DrawEnv(envs[i]);
							EditorGUILayout.EndVertical();
						}
						GUILayout.Space(5);
					}
				}, isBox: true);

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_target);
		}

		/// <summary>
		/// Initializes an environment by syncing its directive states with the current scripting define symbols in PlayerSettings.
		/// </summary>
		private void InitEnv(Configuration.Env pEnv)
		{
			if (pEnv == null)
				return;
			string[] currentDefines = EditorHelper.GetDirectives();
			// Ensure all current directives are present in the env's list
			foreach (string define in currentDefines)
				pEnv.AddDirective(define, true);

			// Update the 'enabled' state of each directive in the env to match PlayerSettings
			for (int i = 0; i < pEnv.directives.Count; i++)
			{
				if (currentDefines.Length > 0)
				{
					bool exist = false;
					for (int j = 0; j < currentDefines.Length; j++)
					{
						if (currentDefines[j] == pEnv.directives[i].name)
							exist = true;
					}
					pEnv.directives[i].enabled = exist;
				}
				else
					pEnv.directives[i].enabled = false;
			}
		}

		/// <summary>
		/// Draws the reorderable list of directives for a given environment.
		/// </summary>
		private void DrawEnv(Configuration.Env pEnv)
		{
			// Allow renaming of environments (except the default one)
			if (m_previewingEnvs && pEnv.name != DEFAULT_ENV_NAME)
			{
				var newName = EditorHelper.TextField(pEnv.name, "Name", 120, 280);
				if (pEnv.name != newName)
				{
					bool validName = !m_target.envs.Exists(x => x.name == newName && x != pEnv);
					if (validName)
						pEnv.name = newName;
				}
			}

			if (pEnv.directives != null && pEnv.name != null)
			{
				string[] defaultDirectives = { "DEVELOPMENT", "UNITY_IAP", "ADDRESSABLES" };
				foreach (string directive in defaultDirectives)
					pEnv.AddDirective(directive, false);

				// Initialize the ReorderableList for the directives
				if (!m_reorderDirectivesDict.ContainsKey(pEnv.name))
				{
					var reorderList = new ReorderableList(pEnv.directives, typeof(Configuration.Directive), true, true, true, true);
					m_reorderDirectivesDict.TryAdd(pEnv.name, reorderList);
					// Define how each element in the list is drawn
					reorderList.drawElementCallback = (rect, index, isActive, isFocused) =>
					{
						var define = pEnv.directives[index];
						GUI.backgroundColor = define.color;
						const float widthTog = 20;
						const float widthColor = 60;
						float widthName = rect.width - widthTog - widthColor - 10;
						define.enabled = EditorGUI.Toggle(new Rect(rect.x, rect.y, widthTog, 20), define.enabled);

						// Make default directives read-only
						if (pEnv.name == DEFAULT_ENV_NAME || defaultDirectives.Contains(define.name))
							EditorGUI.LabelField(new Rect(rect.x + widthTog + 5, rect.y, widthName, 20), define.name);
						else
							define.name = EditorGUI.TextField(new Rect(rect.x + widthTog + 5, rect.y, widthName, 20), define.name);
						define.color = EditorGUI.ColorField(new Rect(rect.x + widthTog + 5 + widthName + 5, rect.y, widthColor, 20), define.color);
						GUI.backgroundColor = Color.white;
					};
					// Define the logic for when an item can be removed
					m_reorderDirectivesDict[pEnv.name].onCanRemoveCallback = (reorderableList) =>
					{
						var directive = pEnv.directives[reorderableList.index];
						return pEnv.name != DEFAULT_ENV_NAME && !defaultDirectives.Contains(directive.name);
					};
				}
				m_reorderDirectivesDict[pEnv.name].DoLayoutList();
				if (GUI.changed)
					pEnv.directives = (List<Configuration.Directive>)m_reorderDirectivesDict[pEnv.name].list;

				// -- Draw action buttons for the environment --
				EditorGUILayout.BeginHorizontal();
				if (m_previewingEnvs)
				{
					if (EditorHelper.Button("Duplicate"))
					{
						var cloneEnv = CloneEnv(pEnv);
						cloneEnv.name += $" ({m_target.envs.Count})";
						m_target.envs.Add(cloneEnv);
					}
					if (EditorHelper.Button("Add Currents"))
					{
						// Adds all scripting defines currently in PlayerSettings to this environment
						string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
						string[] directives = directivesStr.Split(';');
						foreach (string directive in directives)
							if (!pEnv.directives.Exists(x => x.name == directive))
								pEnv.directives.Add(new Configuration.Directive()
								{
									name = directive,
								});
					}
					if (pEnv.name != DEFAULT_ENV_NAME && EditorHelper.ButtonColor("Remove", Color.red))
						m_target.envs.Remove(pEnv);
				}
				else
				{
					if (EditorHelper.Button("Apply"))
						ApplyDirectives(pEnv.directives);
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		/// <summary>
		/// Toggles between the "edit current env" and "manage all envs" views.
		/// </summary>
		private void EditEnv(bool pMode)
		{
			if (m_previewingEnvs == pMode)
				return;
			m_previewingEnvs = pMode;
			m_reorderDirectivesDict.Clear(); // Clear cache to force ReorderableLists to be rebuilt
		}

		/// <summary>
		/// Applies the given list of directives to the project's PlayerSettings.
		/// </summary>
		private static void ApplyDirectives(List<Configuration.Directive> defines)
		{
			string symbols = string.Join(";", defines
				.Where(d => d.enabled && !string.IsNullOrEmpty(d.name))
				.Select(d => d.name)
				.ToArray());
			var target = EditorUserBuildSettings.selectedBuildTargetGroup;
			PlayerSettings.SetScriptingDefineSymbolsForGroup(target, symbols);
		}

		/// <summary>
		/// Draws the UI for saving the current environment or loading a saved one.
		/// </summary>
		private void DrawComboBoxEnvs()
		{
			EditorHelper.BoxVertical(() =>
			{
				var envs = m_target.envs ?? new List<Configuration.Env>();

				// -- UI for saving the current setup as a new/existing named environment --
				EditorHelper.BoxHorizontal(() =>
				{
					m_typedEnvName = EditorHelper.TextField(m_typedEnvName, "Env Name");
					if (EditorHelper.ButtonColor("Save Env", Color.green))
					{
						if (string.IsNullOrEmpty(m_typedEnvName))
							return;

						int index = -1;
						for (int i = 0; i < envs.Count; i++)
						{
							if (envs[i].name == m_typedEnvName.Trim())
							{
								// Overwrite existing environment
								envs[i] = CloneEnv(m_target.curEnv);
								index = i;
								break;
							}
						}
						if (index == -1)
						{
							// Save as new environment
							var newEnv = CloneEnv(m_target.curEnv);
							newEnv.name = m_typedEnvName;
							envs.Add(newEnv);
						}

						m_selectedEnv = m_typedEnvName;

						ApplyEnv(m_selectedEnv, false);
					}
				}, Color.yellow);

				// -- UI for loading an environment from the dropdown --
				string[] allEnvNames = new string[envs.Count];
				for (int i = 0; i < envs.Count; i++)
					allEnvNames[i] = envs[i].name;

				if (allEnvNames.Length > 0)
				{
					GUILayout.BeginHorizontal();
					var preSelectedEnv = m_selectedEnv;
					m_selectedEnv = EditorHelper.DropdownList(m_selectedEnv, "Envs", allEnvNames);
					if (m_selectedEnv != preSelectedEnv)
						ApplyEnv(m_selectedEnv, false); // Load the new selection into the editor view
					if (EditorHelper.ButtonColor("Edit", Color.yellow))
						EditEnv(true);
					if (EditorHelper.ButtonColor("Apply", Color.green))
						ApplyEnv(m_selectedEnv); // Apply the directives to PlayerSettings
					GUILayout.EndHorizontal();
				}
			}, Color.cyan, true);
		}

		/// <summary>
		/// Sets the currently active environment in the configuration.
		/// </summary>
		/// <param name="pName">The name of the environment to apply.</param>
		/// <param name="pApplyDirectives">If true, also applies the environment's directives to PlayerSettings.</param>
		private void ApplyEnv(string pName, bool pApplyDirectives = true)
		{
			var envs = m_target.envs;
			for (int i = 0; i < envs.Count; i++)
				if (envs[i].name == pName)
				{
					m_target.curEnv = CloneEnv(envs[i]);
					m_typedEnvName = m_target.curEnv.name;
					if (pApplyDirectives)
						ApplyDirectives(m_target.curEnv.directives);
					m_reorderDirectivesDict.Clear(); // Refresh the reorderable list
					break;
				}
		}

		/// <summary>
		/// Creates a deep copy of an Env object using JSON serialization.
		/// </summary>
		private static Configuration.Env CloneEnv(Configuration.Env pEnv)
		{
			var toJson = JsonUtility.ToJson(pEnv);
			var fromJson = JsonUtility.FromJson<Configuration.Env>(toJson);
			return fromJson;
		}

		//========== FIREBASE ==========

		/// <summary>
		/// Draws the UI for the "Firebase" tab, which handles switching google-services.json files.
		/// </summary>
		private void DrawTabPageFirebase()
		{
			EditorHelper.BoxVertical("Firebase", () =>
			{
				FirebaseDevConfigPath = EditorHelper.FileField(FirebaseDevConfigPath, "Dev Config Fire", "json,txt");
				FirebaseLiveConfigPath = EditorHelper.FileField(FirebaseLiveConfigPath, "Live Config Fire", "json,txt");
				FirebaseConfigOutputFolder = EditorHelper.FolderField(FirebaseConfigOutputFolder, "Output Folder");

				string testPath = Application.dataPath + FirebaseDevConfigPath;
				string livePath = Application.dataPath + FirebaseLiveConfigPath;
				string destination = Application.dataPath + FirebaseConfigOutputFolder + "/google-services.json";
				string curProjectNumber = FirebaseProjectNumber;

				GUILayout.BeginHorizontal();
				if (livePath == testPath && livePath != "")
				{
					EditorGUILayout.HelpBox("Live and Test Path must not be the same!", MessageType.Warning);
					return;
				}

				if (EditorHelper.Button("Use Live Config"))
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
				}
				if (EditorHelper.Button("Use Dev Config"))
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
				}
				GUILayout.EndHorizontal();

				// -- Preview of the currently active google-services.json --
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

		/// <summary>
		/// Validates that the file and folder paths stored in EditorPrefs still exist.
		/// </summary>
		private static void CheckFirebaseConfigPaths()
		{
			string devFilePath = Application.dataPath + "/" + FirebaseDevConfigPath;
			string liveFilePath = Application.dataPath + "/" + FirebaseLiveConfigPath;
			string outputFolder = Application.dataPath + "/" + FirebaseConfigOutputFolder;
			if (!Directory.Exists(outputFolder))
				FirebaseConfigOutputFolder = FirebaseConfigOutputFolder.Contains("Assets") ? EditorHelper.FormatPathToUnityPath(FirebaseConfigOutputFolder) : "";
			if (!File.Exists(devFilePath))
				FirebaseDevConfigPath = FirebaseDevConfigPath.Contains("Assets") ? EditorHelper.FormatPathToUnityPath(FirebaseDevConfigPath) : "";
			if (!File.Exists(liveFilePath))
				FirebaseLiveConfigPath = FirebaseLiveConfigPath.Contains("Assets") ? EditorHelper.FormatPathToUnityPath(FirebaseLiveConfigPath) : "";
		}
	}
}