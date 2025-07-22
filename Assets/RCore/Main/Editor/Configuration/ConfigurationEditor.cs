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

		#region Firebase Configuration Paths (stored in EditorPrefs)

		private static string FirebaseDevConfigPath { get => EditorPrefs.GetString("firebaseDevConfigPath"); set => EditorPrefs.SetString("firebaseDevConfigPath", value); }
		private static string FirebaseLiveConfigPath { get => EditorPrefs.GetString("firebaseLiveConfigPath"); set => EditorPrefs.SetString("firebaseLiveConfigPath", value); }
		private static string FirebaseConfigOutputFolder { get => EditorPrefs.GetString("FirebaseConfigOutputFolder", Application.dataPath); set => EditorPrefs.SetString("FirebaseConfigOutputFolder", value); }
		private static string FirebaseConfig { get => EditorPrefs.GetString("firebase_config_" + Application.productName); set => EditorPrefs.SetString("firebase_config_" + Application.productName, value); }
		private static string FirebaseProjectNumber { get => EditorPrefs.GetString("project_number"); set => EditorPrefs.SetString("project_number", value); }

		#endregion

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
			if (currentTime - m_lastUpdateTime >= 1f)
			{
				m_lastUpdateTime = currentTime;
				m_buildName = EditorHelper.GetBuildName();
			}
		}

		/// <summary>
		/// Draws the main inspector GUI with a tabbed interface.
		/// </summary>
		public override void OnInspectorGUI()
		{
			// The "Firebase" tab is currently disabled. To re-enable, add "Firebase" to the list of tabs.
			var tab = EditorHelper.Tabs("dev_setting_tabs", "Default", "Envs Manager");
			GUILayout.Space(5);
			switch (tab)
			{
				case "Envs Manager":
					DrawTabPageEnv();
					break;
				// To re-enable the Firebase tab:
				// case "Firebase":
				// 	DrawTabPageFirebase();
				// 	break;
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
				EditorHelper.BoxVertical($"Current Environment: {m_target.curEnv.name}", () =>
				{
					GUILayout.Space(5);
					EditorGUILayout.BeginVertical("box");
					DrawEnv(m_target.curEnv);
					EditorGUILayout.EndVertical();
					DrawComboBoxEnvs(); // Show controls for loading/saving envs
				}, isBox: true);
			}
			else
			{
				// -- VIEW FOR MANAGING ALL SAVED ENVIRONMENTS --
				EditorHelper.BoxVertical("All Environments", () =>
				{
					var envs = m_target.envs;
					if (envs.Count == 0) return;

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
			}

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_target);
		}

		/// <summary>
		/// Initializes an environment by syncing its directive states with the current scripting define symbols in PlayerSettings.
		/// </summary>
		private void InitEnv(Configuration.Env pEnv)
		{
			if (pEnv == null) return;
			string[] currentDefines = EditorHelper.GetDirectives();
			
			// Ensure all current directives are present in the env's list
			foreach (string define in currentDefines)
				pEnv.AddDirective(define, true);

			// Update the 'enabled' state of each directive in the env to match PlayerSettings
			for (int i = 0; i < pEnv.directives.Count; i++)
			{
				pEnv.directives[i].enabled = currentDefines.Contains(pEnv.directives[i].name);
			}
		}

		/// <summary>
		/// Draws the reorderable list of directives for a given environment.
		/// </summary>
		private void DrawEnv(Configuration.Env pEnv)
		{
			if (pEnv == null) return;
			
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
					m_reorderDirectivesDict[pEnv.name] = reorderList;
					
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
					reorderList.onCanRemoveCallback = (list) =>
					{
						var directive = pEnv.directives[list.index];
						return pEnv.name != DEFAULT_ENV_NAME && !defaultDirectives.Contains(directive.name);
					};
				}
				
				m_reorderDirectivesDict[pEnv.name].DoLayoutList();

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
						string[] directives = EditorHelper.GetDirectives();
						foreach (string directive in directives)
							pEnv.AddDirective(directive, true);
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
			if (m_previewingEnvs == pMode) return;
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
			Debug.Log($"Applied new directives for {target}: {symbols}");
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
						if (string.IsNullOrEmpty(m_typedEnvName)) return;

						int index = envs.FindIndex(e => e.name == m_typedEnvName.Trim());
						if (index != -1)
						{
							// Overwrite existing environment
							envs[index] = CloneEnv(m_target.curEnv);
						}
						else
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
				string[] allEnvNames = envs.Select(e => e.name).ToArray();
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
			var env = m_target.envs.FirstOrDefault(e => e.name == pName);
			if (env != null)
			{
				m_target.curEnv = CloneEnv(env);
				m_typedEnvName = m_target.curEnv.name;
				if (pApplyDirectives)
					ApplyDirectives(m_target.curEnv.directives);
				m_reorderDirectivesDict.Clear(); // Refresh the reorderable list
			}
		}

		/// <summary>
		/// Creates a deep copy of an Env object using JSON serialization.
		/// </summary>
		private static Configuration.Env CloneEnv(Configuration.Env pEnv)
		{
			var toJson = JsonUtility.ToJson(pEnv);
			return JsonUtility.FromJson<Configuration.Env>(toJson);
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

				string devPath = Application.dataPath + "/" + FirebaseDevConfigPath;
				string livePath = Application.dataPath + "/" + FirebaseLiveConfigPath;
				string destination = Application.dataPath + "/" + FirebaseConfigOutputFolder + "/google-services.json";

				GUILayout.BeginHorizontal();
				if (livePath == devPath && !string.IsNullOrEmpty(livePath))
				{
					EditorGUILayout.HelpBox("Live and Dev paths must not be the same!", MessageType.Warning);
					GUILayout.EndHorizontal();
					return;
				}

				if (EditorHelper.Button("Use Live Config"))
					SwitchFirebaseConfig(livePath, destination);
				if (EditorHelper.Button("Use Dev Config"))
					SwitchFirebaseConfig(devPath, destination);
				
				GUILayout.EndHorizontal();

				// -- Preview of the currently active google-services.json --
				string currentConfig = GetCurrentFirebaseConfig(destination);
				EditorHelper.BoxVertical(() =>
				{
					EditorGUILayout.LabelField("Current Config Preview...", EditorStyles.boldLabel);
					EditorGUILayout.LabelField(currentConfig, GUILayout.MaxHeight(600), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 50));
				}, Color.white, true);
			}, default, true);
		}
		
		/// <summary>
		/// Switches the active google-services.json file.
		/// </summary>
		private void SwitchFirebaseConfig(string sourcePath, string destinationPath)
		{
			if (!File.Exists(sourcePath))
			{
				Debug.LogError($"Firebase config file not found at: {sourcePath}");
				return;
			}
			
			var sourceFile = new FileInfo(sourcePath);
			using var reader = sourceFile.OpenText();
			string content = reader.ReadToEnd();
			var contentNode = SimpleJSON.JSON.Parse(content);
			var projectNumberNode = contentNode["project_info"]["project_number"];
			
			// Only switch if the project number is different, to avoid unnecessary work
			if (FirebaseProjectNumber == projectNumberNode.Value)
			{
				Debug.Log("Firebase config is already set to this version.");
				return;
			}
			
			FirebaseProjectNumber = projectNumberNode;
			File.WriteAllText(destinationPath, content);
			FirebaseConfig = content;
			AssetDatabase.Refresh();
			Debug.Log($"Switched Firebase config to: {sourcePath}");
			
			// Force the dependency resolver to run
			EditorApplication.ExecuteMenuItem("Assets/Play Services Resolver/Android Resolver/Force Resolve");
			EditorApplication.ExecuteMenuItem("Assets/External Dependency Manager/Android Resolver/Force Resolve");
		}
		
		/// <summary>
		/// Gets the content of the current google-services.json file for previewing.
		/// </summary>
		private string GetCurrentFirebaseConfig(string destinationPath)
		{
			string currentConfig = FirebaseConfig;
			if (string.IsNullOrEmpty(currentConfig) || !File.Exists(destinationPath))
			{
				try
				{
					using var reader = new FileInfo(destinationPath).OpenText();
					currentConfig = reader.ReadToEnd();
					FirebaseConfig = currentConfig;
				}
				catch
				{
					FirebaseConfig = "google-services.json not found or could not be read.";
					currentConfig = FirebaseConfig;
				}
			}
			return currentConfig;
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
				FirebaseConfigOutputFolder = "";
			if (!File.Exists(devFilePath))
				FirebaseDevConfigPath = "";
			if (!File.Exists(liveFilePath))
				FirebaseLiveConfigPath = "";
		}
	}
}