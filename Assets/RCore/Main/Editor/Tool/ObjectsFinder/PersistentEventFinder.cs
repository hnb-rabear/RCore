using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Helper class to scan for GameObjects with persistent events (e.g. OnClick) configured in the inspector.
	/// </summary>
	public class PersistentEventFinder
	{
		private static List<GameObject> m_PersistentEvents;
		private static Dictionary<GameObject, List<GameObject>> m_PersistentEventsCount;
		private static Dictionary<GameObject, string> m_PersistentEventsMethod;
		private static List<string> m_AllPersistentMethods;

		public void DrawOnGUI()
		{
			if (GUILayout.Button("Scan Persistent Events"))
			{
				string folderPath = EditorUtility.OpenFilePanel("Select Folder", Application.dataPath, "");
				if (!string.IsNullOrEmpty(folderPath) && folderPath.StartsWith(Application.dataPath))
					Debug.Log($"Selected folder: {folderPath}");
				else
					Debug.LogError("The selected folder is outside the project directory or no folder was selected.");
				folderPath = EditorHelper.FormatPathToUnityPath(folderPath);
				FindPersistentEvents(folderPath);
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
		}
		private void FindPersistentEvents(string folderPath)
		{
			m_PersistentEvents = new List<GameObject>();
			m_PersistentEventsCount = new Dictionary<GameObject, List<GameObject>>();
			m_PersistentEventsMethod = new Dictionary<GameObject, string>();
			m_AllPersistentMethods = new List<string>();
			var guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
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
	}
}