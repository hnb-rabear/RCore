using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace RevCore.Tools.Editor
{
    public class PersistentEventFinder
    {
        private List<GameObject> m_persistentEvents;
        private Dictionary<GameObject, List<GameObject>> m_persistentEventsCount;
        private Dictionary<GameObject, string> m_persistentEventsMethod;
        private List<string> m_allPersistentMethods;
        private readonly Dictionary<string, bool> m_foldouts = new();

        public void DrawOnGUI()
        {
            if (GUILayout.Button("Scan Persistent Events"))
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith(Application.dataPath))
                {
                    Debug.LogError("The selected folder is outside the project directory or no folder was selected.");
                    return;
                }
                folderPath = AssetPathHelper.ToUnityPath(folderPath);
                FindPersistentEvents(folderPath);
            }

            if (m_persistentEvents == null || m_persistentEvents.Count <= 0)
                return;

            if (EditorGuiHelper.HeaderFoldout($"Prefabs has Persistent Event {m_persistentEvents.Count}", "Prefabs has Persistent Event"))
            {
                foreach (var kvp in m_persistentEventsCount)
                {
                    string key = kvp.Key.name;
                    if (!m_foldouts.ContainsKey(key))
                        m_foldouts[key] = false;
                    m_foldouts[key] = EditorGUILayout.Foldout(m_foldouts[key], $"{key} ({kvp.Value.Count})", true);
                    if (m_foldouts[key])
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), false);
                        if (GUILayout.Button("Log Methods"))
                            Debug.Log(m_persistentEventsMethod[kvp.Key]);
                        GUILayout.EndHorizontal();
                        foreach (var obj in kvp.Value)
                            EditorGUILayout.ObjectField(obj, typeof(GameObject), false);
                    }
                }
            }

            if (GUILayout.Button("Log all persistent methods"))
                Debug.Log(string.Join("\n", m_allPersistentMethods));

            if (GUILayout.Button("Select All"))
                Selection.objects = m_persistentEvents.ToArray();
        }

        private void FindPersistentEvents(string folderPath)
        {
            m_persistentEvents = new List<GameObject>();
            m_persistentEventsCount = new Dictionary<GameObject, List<GameObject>>();
            m_persistentEventsMethod = new Dictionary<GameObject, string>();
            m_allPersistentMethods = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (HasPersistentEvents(obj) && !m_persistentEvents.Contains(obj))
                    m_persistentEvents.Add(obj);
            }
        }

        private bool HasPersistentEvents(GameObject go)
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
                        if (!field.FieldType.IsSubclassOf(typeof(UnityEventBase)))
                            continue;

                        var unityEventBase = (UnityEventBase)field.GetValue(script);
                        if (unityEventBase == null)
                            continue;

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
                                    if (!m_allPersistentMethods.Contains(functionName))
                                        m_allPersistentMethods.Add(functionName);
                                    methodName += functionName + (i < count - 1 ? "\n" : "");
                                }
                            }
                        }
                        else if (unityEventBase is UnityEvent<int> unityEventGeneric)
                        {
                            count = unityEventGeneric.GetPersistentEventCount();
                            for (int i = 0; i < count; i++)
                            {
                                string functionName = unityEventGeneric.GetPersistentTarget(i) != null ? unityEventGeneric.GetPersistentMethodName(i) : "";
                                if (!string.IsNullOrEmpty(functionName))
                                {
                                    if (!m_allPersistentMethods.Contains(functionName))
                                        m_allPersistentMethods.Add(functionName);
                                    methodName += functionName + (i < count - 1 ? "\n" : "");
                                }
                            }
                        }

                        if (count > 0 && !string.IsNullOrEmpty(methodName))
                        {
                            if (m_persistentEventsCount.ContainsKey(go))
                            {
                                m_persistentEventsCount[go].Add(script.gameObject);
                                if (!m_persistentEventsMethod[go].Contains(methodName))
                                    m_persistentEventsMethod[go] += "\n" + methodName;
                            }
                            else
                            {
                                m_persistentEventsCount.Add(go, new List<GameObject> { script.gameObject });
                                m_persistentEventsMethod[go] = methodName;
                            }
                        }

                        persistentCount += count;
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
                                if (!m_allPersistentMethods.Contains(functionName))
                                    m_allPersistentMethods.Add(functionName);
                                methodName += functionName + (i < count - 1 ? "\n" : "");
                            }
                        }
                        if (count > 0 && !string.IsNullOrEmpty(methodName))
                        {
                            if (m_persistentEventsCount.ContainsKey(go))
                            {
                                m_persistentEventsCount[go].Add(script.gameObject);
                                if (!m_persistentEventsMethod[go].Contains(methodName))
                                    m_persistentEventsMethod[go] += "\n" + methodName;
                            }
                            else
                            {
                                m_persistentEventsCount.Add(go, new List<GameObject> { script.gameObject });
                                m_persistentEventsMethod[go] = methodName;
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
