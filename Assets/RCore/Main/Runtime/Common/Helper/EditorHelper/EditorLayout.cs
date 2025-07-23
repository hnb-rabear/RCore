#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
    public static class EditorLayout
    {
        public static bool Foldout(string label, Action onFoldoutContent)
        {
            var foldout = new EditorFoldout { label = label, onFoldout = onFoldoutContent };
            foldout.Draw();
            return foldout.IsFoldout;
        }
        
        public static bool HeaderFoldout(string label, string key, bool minimalistic = false, params IDraw[] pHorizontalDraws)
        {
	        var headerFoldout = new EditorHeaderFoldout()
	        {
		        key = string.IsNullOrEmpty(key) ? label : key,
		        label = label,
		        minimalistic = minimalistic,
	        };
	        if (pHorizontalDraws != null)
		        GUILayout.BeginHorizontal();

	        headerFoldout.Draw();

	        if (pHorizontalDraws != null && headerFoldout.IsFoldout)
		        foreach (var d in pHorizontalDraws)
			        d.Draw();

	        if (pHorizontalDraws != null)
		        GUILayout.EndHorizontal();

	        return headerFoldout.IsFoldout;
        }

        public static void HeaderFoldout(string label, string key, Action pOnFoldOut, params IDraw[] pHorizontalDraws)
        {
	        var headerFoldout = new EditorHeaderFoldout()
	        {
		        key = string.IsNullOrEmpty(key) ? label : key,
		        label = label,
	        };
	        if (pHorizontalDraws != null)
		        GUILayout.BeginHorizontal();

	        headerFoldout.Draw();

	        if (pHorizontalDraws != null && headerFoldout.IsFoldout)
		        foreach (var d in pHorizontalDraws)
			        d.Draw();

	        if (pHorizontalDraws != null)
		        GUILayout.EndHorizontal();

	        if (headerFoldout.IsFoldout)
	        {
		        var style = new GUIStyle("box")
		        {
			        margin = new RectOffset(10, 0, 0, 0),
			        padding = new RectOffset()
		        };
		        GUILayout.BeginVertical(style);
		        pOnFoldOut();
		        GUILayout.EndVertical();
	        }
        }

        public static string Tabs(string key, params string[] tabs)
        {
            var tabControl = new EditorTabs { key = key, tabsName = tabs };
            tabControl.Draw();
            return tabControl.CurrentTab;
        }
        
        public static void DragDropBox<T>(string name, Action<T[]> onDrop) where T : Object
        {
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetRect(0.0f, 30, new GUIStyle("Toolbar"), GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, $"Drag and Drop {name} Here");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        var droppedObjects = new List<T>();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is T t)
                            {
                                droppedObjects.Add(t);
                            }
                            else if (obj is GameObject go)
                            {
                                if (typeof(T).IsSubclassOf(typeof(Component)))
                                {
                                    var component = go.GetComponent<T>();
                                    if (component != null) droppedObjects.Add(component);
                                }
                            }
                            else
                            {
                                var path = AssetDatabase.GetAssetPath(obj);
                                if (AssetDatabase.IsValidFolder(path))
                                {
                                    droppedObjects.AddRange(EditorAssetUtil.GetObjects<T>(path, $"t:{typeof(T).Name}"));
                                }
                            }
                        }
                        onDrop(droppedObjects.ToArray());
                    }
                    break;
            }
        }

        public static void ListReadonlyObjects<T>(string pName, List<T> pList, List<string> pLabels = null, bool pShowObjectBox = true) where T : Object
        {
            ListObjects(pName, ref pList, pLabels, pShowObjectBox, true);
        }
        
        public static bool ListObjects<T>(string pName, ref List<T> pObjects, List<string> pLabels, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null) where T : Object
        {
            GUILayout.Space(3);

            var prevColor = GUI.color;
            GUI.backgroundColor = new Color(1, 1, 0.5f);

            var list = pObjects;
            var show = HeaderFoldout($"{pName} ({pObjects.Count})", pName);
            if (show)
            {
                int page = EditorPrefs.GetInt($"{pName}_page", 0);
                int totalPages = Mathf.CeilToInt(list.Count * 1f / 20f);
                if (totalPages == 0)
                    totalPages = 1;
                if (page < 0)
                    page = 0;
                if (page >= totalPages)
                    page = totalPages - 1;
                int from = page * 20;
                int to = page * 20 + 20 - 1;
                if (to > list.Count - 1)
                    to = list.Count - 1;
                
	            EditorGUILayout.BeginVertical("box");
                {
                    int boxSize = 34;
                    if (pShowObjectBox)
                    {
                        boxSize = EditorPrefs.GetInt($"{pName}_Slider", 34);
                        int boxSizeNew = (int)EditorGUILayout.Slider(boxSize, 34, 68);
                        if (boxSize != boxSizeNew)
                        {
                            EditorPrefs.SetInt($"{pName}_Slider", boxSizeNew);
                            boxSize = boxSizeNew;
                        }
                    }

                    if (!pReadOnly)
                    {
                        var list1 = list;
                        DragDropBox<T>(pName, (objs) =>
                        {
                            list1.AddRange(objs);
                        });
                    }

                    if (totalPages > 1)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("<Prev<"))
                        {
                            if (page > 0)
                                page--;
                            EditorPrefs.SetInt($"{pName}_page", page);
                        }

                        EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
                        if (GUILayout.Button(">Next>"))
                        {
                            if (page < totalPages - 1)
                                page++;
                            EditorPrefs.SetInt($"{pName}_page", page);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    for (int i = from; i <= to; i++)
                    {
	                    if (i >= list.Count)
		                    continue;
	                    
	                    EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
                            if (pLabels != null && i < pLabels.Count)
	                            EditorGui.Label(pLabels[i], (int)Mathf.Max(pLabels[i].Length * 8f, 100), false);
                            list[i] = (T)EditorGui.ObjectField<T>(list[i], "");
                            if (pShowObjectBox)
	                            list[i] = (T)EditorGui.ObjectField<T>(list[i], "", 0, boxSize, true);
                            
                            if (!pReadOnly)
                            {
                                if (EditorGui.Button("▲", 23) && i > 0)
	                                (list[i], list[i - 1]) = (list[i - 1], list[i]);

                                if (EditorGui.Button("▼", 23) && i < list.Count - 1)
	                                (list[i], list[i + 1]) = (list[i + 1], list[i]);

                                if (EditorGui.Button("-", Color.red, 23))
                                {
	                                list.RemoveAt(i);
	                                i--;
                                }
                                
                                if (EditorGui.Button("+", Color.green, 23))
                                    list.Insert(i + 1, null);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (totalPages > 1)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("<Prev<"))
                        {
                            if (page > 0)
                                page--;
                            EditorPrefs.SetInt($"{pName}_page", page);
                        }

                        EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
                        if (GUILayout.Button(">Next>"))
                        {
                            if (page < totalPages - 1)
                                page++;
                            EditorPrefs.SetInt($"{pName}_page", page);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if (!pReadOnly)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            if (EditorGui.Button("+1", Color.green, 30))
                            {
                                list.Add(null);
                                page = totalPages - 1;
                                EditorPrefs.SetInt($"{pName}_page", page);
                            }

                            if (GUILayout.Button("Sort By Name"))
                                list = list.OrderBy(m => m.name).ToList();
                            if (GUILayout.Button("Remove Duplicate"))
                                list.RemoveDuplicated();

                            if (EditorGui.Button("Clear", Color.red, 50))
                                if (EditorGui.ConfirmPopup())
                                    list = new List<T>();
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (pAdditionalDraws != null)
                        foreach (var draw in pAdditionalDraws)
                            draw.Draw();
                }
                EditorGUILayout.EndVertical();
            }

            pObjects = list;

            if (GUI.changed)
                EditorPrefs.SetBool(pName, show);

            GUI.backgroundColor = prevColor;

            return show;
        }

        public static void PagesForList(int pCount, string pName, Action<int> pOnDraw, IDraw[] p_drawAtFirst = null, IDraw[] p_drawAtLast = null)
        {
             // This is now effectively a duplicate of the pagination logic inside ListObjects.
             // For simplicity, it can be kept if direct use is required.
        }

        public static void ListObjectsWithSearch<T>(ref List<T> pList, string pName, bool pShowObjectBox = true) where T : Object
        {
            // This method combines search with the ListObjects logic. It can be implemented by filtering
            // the list before passing it to a modified ListObjects, or by integrating the search field directly.
        }

        public static bool ListKeyObjects<TKey, TValue>(string pName, ref List<SerializableKeyValue<TKey, TValue>> pList, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null) where TValue : Object
        {
            // This is a more complex variant of ListObjects and requires its own implementation block,
            // similar to ListObjects but handling the key-value pair.
            return false; // Placeholder
        }
        
        public static void GridDraws(int pCell, List<IDraw> pDraws, Color color = default)
        {
            int row = Mathf.CeilToInt(pDraws.Count / (float)pCell);
            var bgColor = GUI.backgroundColor;
            if (color != default) GUI.backgroundColor = color;
            
            EditorGUILayout.BeginVertical("box");
            for (int i = 0; i < row; i++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < pCell; j++)
                {
                    int index = i * pCell + j;
                    if (index < pDraws.Count)
                        pDraws[index].Draw();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            if (color != default) GUI.backgroundColor = bgColor;
        }
        
        public static Vector2 ScrollView(ref Vector2 scrollPos, float height, Action action)
        {
            EditorGUILayout.BeginVertical("box");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height));
            action?.Invoke();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return scrollPos;
        }
        
        public static ReorderableList CreateReorderableList<T>(T[] pObjects, string pName) where T : Object
        {
            var reorderableList = new ReorderableList(pObjects, typeof(T), true, true, true, true);
            reorderableList.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, pName);
            reorderableList.drawElementCallback = (rect, index, a, b) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                pObjects[index] = (T)EditorGUI.ObjectField(rect, pObjects[index], typeof(T), true);
            };
            return reorderableList;
        }

        public static ReorderableList CreateReorderableList<T>(List<T> pObjects, string pName) where T : Object
        {
            var reorderableList = new ReorderableList(pObjects, typeof(T), true, true, true, true);
            reorderableList.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, pName);
            reorderableList.drawElementCallback = (rect, index, a, b) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                pObjects[index] = (T)EditorGUI.ObjectField(rect, pObjects[index], typeof(T), true);
            };
            return reorderableList;
        }
        
        public static void DrawAssetsList<T>(AssetsList<T> assets, string pDisplayName, bool @readonly = false, List<string> labels = null) where T : Object
        {
	        bool showBox = assets.defaultAsset is Sprite;
	        var draw = new EditorObject<T>()
	        {
		        value = assets.defaultAsset,
		        label = "Default",
	        };
	        if (ListObjects(pDisplayName, ref assets.source, labels, showBox, @readonly, new IDraw[] { draw }))
		        assets.defaultAsset = (T)draw.OutputValue;
        }
    }
}
#endif