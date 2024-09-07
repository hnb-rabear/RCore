using RCore.Common;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace RCore.Editor
{
	public class UnusedMonoScriptFinderWindow : EditorWindow
	{
		private Object m_TargetFolder;
		private List<MonoScript> m_AllScripts;
		private List<MonoScript> m_UsedScripts;
		private List<GameObject> m_ParticleSystemPrefabs;
		private Vector2 m_ScrollPosition;
		private string m_FolderPath;

		[MenuItem("RCore/Tools/Find Unused Mono Scripts")]
		public static void ShowWindow()
		{
			var window = GetWindow<UnusedMonoScriptFinderWindow>();
			window.titleContent = new GUIContent("Unused Scripts");
			window.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Target Folder:", GUILayout.Width(100));
			m_TargetFolder = EditorGUILayout.ObjectField(m_TargetFolder, typeof(DefaultAsset), false);
			EditorGUILayout.EndHorizontal();

			if (GUILayout.Button("Find Unused Scripts"))
				FindUnusedScripts();

			if (GUILayout.Button("Find Particle Systems"))
				FindParticleSystems();

			if (GUILayout.Button("Find Persistent Events"))
				FindPersistentEvents();

			if (GUILayout.Button("Refresh Assets"))
			{
				if (m_TargetFolder != null)
					m_FolderPath = AssetDatabase.GetAssetPath(m_TargetFolder);
				else m_FolderPath = "Assets";
				var guids = AssetDatabase.FindAssets("t:GameObject", new[] { m_FolderPath });
				foreach (string guid in guids)
				{
					var obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
					EditorUtility.SetDirty(obj);
				}
				AssetDatabase.SaveAssets();
			}

			EditorGUILayout.Space();

			m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

			if (m_AllScripts != null && m_AllScripts.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"All Scripts: {m_AllScripts.Count}", "All Scripts"))
				{
					foreach (var script in m_AllScripts)
					{
						EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
					}
				}
			}

			if (m_UsedScripts != null && m_UsedScripts.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"Used Scripts: {m_UsedScripts.Count}", "Used Scripts"))
					foreach (var script in m_UsedScripts)
						EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
			}

			if (m_UsedScripts != null && m_AllScripts != null && m_UsedScripts.Count > 0)
			{
				if (EditorHelper.HeaderFoldout("Unused Scripts"))
				{
					var scripts = m_AllScripts.Except(m_UsedScripts).ToList();
					GUILayout.Label($"Unused Scripts: {scripts.Count}");
					foreach (var script in scripts)
						EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
					if (GUILayout.Button("Delete Unused Scripts"))
						DeleteUnusedScripts(scripts);
				}
			}

			if (m_ParticleSystemPrefabs != null && m_ParticleSystemPrefabs.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"ParticleSystem prefabs {m_ParticleSystemPrefabs.Count}", "ParticleSystem prefabs"))
					foreach (var script in m_ParticleSystemPrefabs)
						EditorGUILayout.ObjectField(script, typeof(GameObject), false);

				if (EditorHelper.Button("Select All"))
					Selection.objects = m_ParticleSystemPrefabs.ToArray();
			}

			if (m_PersistentEvents != null && m_PersistentEvents.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"Prefabs has Persistent Event {m_PersistentEvents.Count}", "Prefabs has Persistent Event"))
				{
					foreach (var persistentEvents in m_PersistentEventsCount)
					{
						if (EditorHelper.Foldout($"{persistentEvents.Key.name} ({persistentEvents.Value.Count})"))
						{
							GUILayout.BeginHorizontal();
							EditorGUILayout.ObjectField(persistentEvents.Key, typeof(GameObject), false);
							if (GUILayout.Button("Log Methods"))
								Debug.Log(m_PersistentEventsMethod[persistentEvents.Key]);
							GUILayout.EndHorizontal();
							foreach (var obj in persistentEvents.Value)
								EditorGUILayout.ObjectField(obj, typeof(GameObject), false);
						}
					}
				}

				if (EditorHelper.Button("Log all persistent methods"))
				{
					Debug.Log(JsonHelper.ToJson(m_AllPersistentMethods));
				}

				if (EditorHelper.Button("Select All"))
					Selection.objects = m_PersistentEvents.ToArray();
			}

			EditorGUILayout.EndScrollView();
		}

		private void FindUnusedScripts()
		{
			if (m_TargetFolder != null)
				m_FolderPath = AssetDatabase.GetAssetPath(m_TargetFolder);
			else m_FolderPath = "Assets";
			m_AllScripts = GetAllMonoScriptsInFolder();
			m_UsedScripts = GetUsedMonoScripts();

			var unusedScripts = m_AllScripts.Except(m_UsedScripts).ToList();
			foreach (var unusedScript in unusedScripts)
			{
				var unusedScriptType = unusedScript.GetClass();
				foreach (var usedScript in m_UsedScripts)
				{
					var usedScriptType = usedScript.GetClass();
					if (usedScriptType.IsSubclassOf(unusedScriptType))
					{
						if (!m_UsedScripts.Contains(unusedScript))
							m_UsedScripts.Add(unusedScript);
						break;
					}
				}
			}
			Repaint();
		}

		private void FindParticleSystems()
		{
			if (m_TargetFolder != null)
				m_FolderPath = AssetDatabase.GetAssetPath(m_TargetFolder);
			else m_FolderPath = "Assets";

			m_ParticleSystemPrefabs = new List<GameObject>();
			var guids = AssetDatabase.FindAssets("t:GameObject", new[] { m_FolderPath });
			foreach (string guid in guids)
			{
				var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
				if (obj.GetComponent<ParticleSystem>())
				{
					if (!m_ParticleSystemPrefabs.Contains(obj))
						m_ParticleSystemPrefabs.Add(obj);
					continue;
				}
				var ps = obj.gameObject.GetComponentInChildren<ParticleSystem>(true);
				if (ps != null && !m_ParticleSystemPrefabs.Contains(obj) && !PrefabUtility.IsPartOfPrefabInstance(ps.gameObject))
					m_ParticleSystemPrefabs.Add(obj);
			}
		}

		private static List<GameObject> m_PersistentEvents;
		private static Dictionary<GameObject, List<GameObject>> m_PersistentEventsCount;
		private static Dictionary<GameObject, string> m_PersistentEventsMethod;
		private static List<string> m_AllPersistentMethods;
		private void FindPersistentEvents()
		{
			if (m_TargetFolder != null)
				m_FolderPath = AssetDatabase.GetAssetPath(m_TargetFolder);
			else m_FolderPath = "Assets";

			m_PersistentEvents = new List<GameObject>();
			m_PersistentEventsCount = new Dictionary<GameObject, List<GameObject>>();
			m_PersistentEventsMethod = new Dictionary<GameObject, string>();
			m_AllPersistentMethods = new List<string>();
			var guids = AssetDatabase.FindAssets("t:GameObject", new[] { m_FolderPath });
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (HasPersistentEvents(obj) && !m_PersistentEvents.Contains(obj))
					m_PersistentEvents.Add(obj);
			}
		}

		private static bool HasPersistentEvents(GameObject go)
		{
			int persistentCount = 0;
			var components = go.GetComponentsInChildren<MonoBehaviour>(true);
			foreach (var script in components)
			{
				if (script)
				{
					var fields = script.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					foreach (var field in fields)
					{
						if (field.FieldType.IsSubclassOf(typeof(UnityEventBase)))
						{
							var unityEventBase = (UnityEventBase)field.GetValue(script);
							if (unityEventBase != null)
							{
								int count = 0;
								string methodName = "";

								if (unityEventBase is UnityEvent unityEvent)
								{
									count = unityEvent.GetPersistentEventCount();
									for (int i = 0; i < count; i++)
									{
										string functionName = unityEvent.GetPersistentTarget(i) != null ? unityEvent.GetPersistentMethodName(i) : "";
										if (!string.IsNullOrEmpty(functionName))
										{
											if (!m_AllPersistentMethods.Contains(functionName))
												m_AllPersistentMethods.Add(functionName);
											methodName += functionName + (i < count - 1 ? "\n" : "");
										}
									}
								}
								else
								{
									if (unityEventBase is UnityEvent<int> unityEventGeneric)
									{
										count = unityEventGeneric.GetPersistentEventCount();
										for (int i = 0; i < count; i++)
										{
											string functionName = unityEventGeneric.GetPersistentTarget(i) != null ? unityEventGeneric.GetPersistentMethodName(i) : "";
											if (!string.IsNullOrEmpty(functionName))
											{
												if (!m_AllPersistentMethods.Contains(functionName))
													m_AllPersistentMethods.Add(functionName);
												methodName += functionName + (i < count - 1 ? "\n" : "");
											}
										}
									}
									// Add more cases for other UnityEvent types if needed
								}

								if (count > 0 && !string.IsNullOrEmpty(methodName))
								{
									if (m_PersistentEventsCount.ContainsKey(go))
									{
										m_PersistentEventsCount[go].Add(script.gameObject);
										if (!m_PersistentEventsMethod[go].Contains(methodName))
											m_PersistentEventsMethod[go] += "\n" + methodName;
									}
									else
									{
										m_PersistentEventsCount.Add(go, new List<GameObject> { script.gameObject });
										m_PersistentEventsMethod[go] = methodName;
									}
								}

								persistentCount += count;
							}
						}
					}
				}
				if (script is EventTrigger trigger)
				{
					string methodName = "";
					foreach (var entry in trigger.triggers)
					{
						int count = entry.callback.GetPersistentEventCount();
						for (int i = 0; i < count; i++)
						{
							string functionName = entry.callback.GetPersistentTarget(i) != null ? entry.callback.GetPersistentMethodName(i) : "";
							if (!string.IsNullOrEmpty(functionName))
							{
								if (!m_AllPersistentMethods.Contains(functionName))
									m_AllPersistentMethods.Add(functionName);
								methodName += functionName + (i < count - 1 ? "\n" : "");
							}
						}
						if (count > 0 && !string.IsNullOrEmpty(methodName))
						{
							if (m_PersistentEventsCount.ContainsKey(go))
							{
								m_PersistentEventsCount[go].Add(script.gameObject);
								if (!m_PersistentEventsMethod[go].Contains(methodName))
									m_PersistentEventsMethod[go] += "\n" + methodName;
							}
							else
							{
								m_PersistentEventsCount.Add(go, new List<GameObject>() { script.gameObject });
								m_PersistentEventsMethod[go] = methodName;
							}
							persistentCount++;
						}
					}
				}
			}
			return persistentCount > 0;
		}

		private List<MonoScript> GetAllMonoScriptsInFolder()
		{
			var scripts = new List<MonoScript>();
			string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { m_FolderPath });

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (script != null && IsUnityScript(script) && !IsEssentialScript(script))
					scripts.Add(script);
			}

			scripts = scripts.OrderBy(script => script.name).ToList();
			return scripts;
		}

		private List<MonoScript> GetUsedMonoScripts()
		{
			var usedScripts = new List<MonoScript>();

			//======================================

			// Find all objects in the project
			string[] allAssetGuids = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject", new[] { m_FolderPath });
			foreach (string assetGuid in allAssetGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
				var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

				if (asset != null)
				{
					if (asset is GameObject go)
					{
						var components = go.GetComponentsInChildren<MonoBehaviour>(true);
						foreach (var component in components)
						{
							if (component == null)
								continue;
							var script = MonoScript.FromMonoBehaviour(component);
							if (script != null && !usedScripts.Contains(script))
								usedScripts.Add(script);
						}
					}
					else if (asset is ScriptableObject so)
					{
						var script = MonoScript.FromScriptableObject(so);
						if (script != null && !usedScripts.Contains(script))
							usedScripts.Add(script);
					}
				}
			}

			// Find all scenes in the project
			string[] allScenes = GetAllScenes();
			foreach (string scenePath in allScenes)
			{
				EditorSceneManager.OpenScene(scenePath);
				var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

				foreach (var rootObject in rootObjects)
				{
					var components = rootObject.GetComponentsInChildren<Component>(true);

					foreach (var component in components)
					{
						if (component == null)
							continue;
						var script = MonoScript.FromMonoBehaviour(component as MonoBehaviour);
						if (script != null && !usedScripts.Contains(script))
							usedScripts.Add(script);
					}
				}
			}

			var sortedScripts = usedScripts.OrderBy(script => script.name).ToList();
			return sortedScripts;
		}

		private string[] GetAllScenes()
		{
			string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { m_FolderPath });
			var scenePaths = new List<string>();

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				scenePaths.Add(path);
			}

			return scenePaths.ToArray();
		}

		private static bool IsEssentialScript(MonoScript script)
		{
			var scriptType = script.GetClass();
			if (scriptType != null && (scriptType.IsSubclassOf(typeof(UnityEditor.Editor)) || scriptType.IsSubclassOf(typeof(EditorWindow)) || scriptType.IsSubclassOf(typeof(UIBehaviour))))
				return true;

			return false;
		}

		private static bool IsUnityScript(MonoScript script)
		{
			var scriptType = script.GetClass();
			if (scriptType != null && (scriptType.IsSubclassOf(typeof(MonoBehaviour)) || scriptType.IsSubclassOf(typeof(ScriptableObject))))
				return true;

			return false;
		}

		private void DeleteUnusedScripts(List<MonoScript> unreferencedScripts)
		{
			if (EditorUtility.DisplayDialog("Delete Unused Scripts",
				    "Are you sure you want to delete the unused scripts permanently? This operation cannot be undone.",
				    "Delete", "Cancel"))
			{
				foreach (var script in unreferencedScripts)
				{
					string scriptPath = AssetDatabase.GetAssetPath(script);
					AssetDatabase.DeleteAsset(scriptPath);
				}

				AssetDatabase.Refresh();
				Debug.Log("Unused scripts have been deleted.");
			}
		}
	}
}