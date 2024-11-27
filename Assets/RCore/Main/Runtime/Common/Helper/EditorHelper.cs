/***
 * Author HNB-RaBear - 2017
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using TMPro;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
    public interface IDraw
    {
        void Draw(GUIStyle style = null);
    }

    public class EditorColor : IDraw
    {
        public string label;
        public int labelWidth = 80;
        public int valueWidth;
        public Color value;
        public Color outputValue;

        public void Draw(GUIStyle style = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            Color color;
            if (valueWidth == 0)
                color = EditorGUILayout.ColorField(value, GUILayout.Height(16), GUILayout.MinWidth(40));
            else
                color = EditorGUILayout.ColorField(value, GUILayout.Height(20), GUILayout.MinWidth(40),
                    GUILayout.Width(valueWidth));
            outputValue = color;

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();
        }
    }

    public class EditorButton : IDraw
    {
        public string label;
        public Color color;
        public int width;
        public int height;
        public Action onPressed;
        public bool IsPressed { get; private set; }

        public EditorButton() { }

        public EditorButton(string pLabel, Action pOnPressed, Color pColor = default)
        {
            label = pLabel;
            onPressed = pOnPressed;
            color = pColor;
        }

        public void Draw(GUIStyle style = null)
        {
            var defaultColor = GUI.backgroundColor;
            style ??= new GUIStyle("Button");
            if (width > 0)
                style.fixedWidth = width;
            if (height > 0)
	            style.fixedHeight = height;
            if (color != default)
                GUI.backgroundColor = color;
            IsPressed = GUILayout.Button(label, style, GUILayout.MinHeight(21));
            if (IsPressed && onPressed != null)
                onPressed();
            GUI.backgroundColor = defaultColor;
        }
    }

    public class EditorText : IDraw
    {
        public string label;
        public int labelWidth = 80;
        public int valueWidth;
        public string value;
        public string OutputValue { get; private set; }
        public bool readOnly;
        public bool textArea;
        public Color color;

        public void Draw(GUIStyle style = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            value ??= "";

            if (style == null)
            {
                style = new GUIStyle(EditorStyles.textField)
                {
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 4, 4)
                };
                var normalColor = style.normal.textColor;
                if (color != default)
	                normalColor = color;
                style.normal.textColor = normalColor;
            }

            if (readOnly)
	            GUI.enabled = false;
            string str;
            if (valueWidth == 0)
            {
	            if (textArea)
		            str = EditorGUILayout.TextArea(value, style, GUILayout.MinWidth(40));
	            else
		            str = EditorGUILayout.TextField(value, style, GUILayout.MinWidth(40));
            }
            else
            {
	            if (textArea)
		            str = EditorGUILayout.TextArea(value, style, GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
	            else
		            str = EditorGUILayout.TextField(value, style, GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
            }
            if (readOnly)
	            GUI.enabled = true;

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            OutputValue = str;
        }
    }

    public class EditorDropdownListString : IDraw
    {
        public string label;
        public int labelWidth = 80;
        public string[] selections;
        public string value;
        public int valueWidth;
        public string OutputValue { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            if (selections.Length == 0)
            {
                OutputValue = "";
                return;
            }

            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            int index = 0;

            for (int i = 0; i < selections.Length; i++)
            {
                if (value == selections[i])
                    index = i;
            }

            if (valueWidth != 0)
                index = EditorGUILayout.Popup(index, selections, "DropDown", GUILayout.Width(valueWidth));
            else
                index = EditorGUILayout.Popup(index, selections, "DropDown");

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            OutputValue = selections[index] == null ? "" : selections[index];
        }
    }

    public class EditorDropdownListInt : IDraw
    {
        public string label;
        public int labelWidth = 80;
        public int[] selections;
        public int value;
        public int valueWidth;
        public int OutputValue { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            if (selections.Length == 0)
            {
                OutputValue = -1;
                return;
            }

            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            int index = 0;

            string[] selectionsStr = new string[selections.Length];
            for (int i = 0; i < selections.Length; i++)
            {
                if (value == selections[i])
                    index = i;
                selectionsStr[i] = selections[i].ToString();
            }

            if (valueWidth != 0)
                index = EditorGUILayout.Popup(index, selectionsStr, GUILayout.Width(valueWidth));
            else
                index = EditorGUILayout.Popup(index, selectionsStr);

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            OutputValue = selections[index];
        }
    }

    public class EditorDropdownListEnum<T> : IDraw
    {
        public string label;
        public int labelWidth;
        public T value;
        public int valueWidth;
        public T OutputValue { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enumerated type");

            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            var enumValues = Enum.GetValues(typeof(T));
            string[] selections = new string[enumValues.Length];

            int i = 0;
            foreach (T item in enumValues)
            {
                selections[i] = item.ToString();
                i++;
            }

            int index = 0;
            for (i = 0; i < selections.Length; i++)
            {
                if (value.ToString() == selections[i])
                {
                    index = i;
                }
            }

            if (valueWidth != 0)
                index = EditorGUILayout.Popup(index, selections, GUILayout.Width(valueWidth));
            else
                index = EditorGUILayout.Popup(index, selections);

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            i = 0;
            foreach (T item in enumValues)
            {
                if (i == index)
                {
                    OutputValue = item;
                    return;
                }

                i++;
            }

            OutputValue = default;
        }
    }

    public class EditorToggle : IDraw
    {
        public string label;
        public int labelWidth = 80;
        public bool value;
        public int valueWidth;
        public bool readOnly;
        public Color color;
        public bool OutputValue { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            bool result;

            var defaultColor = GUI.color;
            if (color != default)
                GUI.color = color;

            if (style == null)
            {
                style = new GUIStyle(EditorStyles.toggle);
                style.alignment = TextAnchor.MiddleCenter;
                var normalColor = style.normal.textColor;
                normalColor.a = readOnly ? 0.5f : 1;
                style.normal.textColor = normalColor;
            }

            if (valueWidth == 0)
                result = EditorGUILayout.Toggle(value, style, GUILayout.Height(20), GUILayout.MinWidth(40));
            else
                result = EditorGUILayout.Toggle(value, style, GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.Width(valueWidth));

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            if (color != default)
                GUI.color = defaultColor;

            OutputValue = result;
        }
    }

    public class EditorFoldout : IDraw
    {
        public string label;
        public Action onFoldout;
        public bool IsFoldout { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            IsFoldout = EditorPrefs.GetBool($"{label}foldout", false);
            IsFoldout = EditorGUILayout.Foldout(IsFoldout, label);
            if (IsFoldout && onFoldout != null)
                onFoldout();
            if (GUI.changed)
                EditorPrefs.SetBool($"{label}foldout", IsFoldout);
        }
    }

    public class EditorTabs : IDraw
    {
        public string key;
        public string[] tabsName;
        public string CurrentTab { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            CurrentTab = EditorPrefs.GetString($"{key}_current_tab", tabsName[0]);
            
            GUILayout.BeginHorizontal();
            foreach (var tabName in tabsName)
            {
	            bool isOn = CurrentTab == tabName;
                var buttonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedHeight = 0,
                    padding = new RectOffset(4, 4, 4, 4),
                    normal =
                    {
	                    textColor = EditorGUIUtility.isProSkin ? Color.white : (isOn ? Color.black : Color.black * 0.6f)
                    },
                    fontStyle = FontStyle.Bold,
                    fontSize = 13
                };

                var preColor = GUI.color;
                var color = isOn ? Color.white : Color.gray;
                GUI.color = color;

                if (GUILayout.Button(tabName, buttonStyle))
                {
                    CurrentTab = tabName;
                    EditorPrefs.SetString($"{key}_current_tab", CurrentTab);
                }

                GUI.color = preColor;
            }
            GUILayout.EndHorizontal();
        }
    }

    public class EditorHeaderFoldout : IDraw
    {
        public string key;
        public bool minimalistic;
        public string label;
        public bool IsFoldout { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            IsFoldout = EditorPrefs.GetBool(key, false);

            if (!minimalistic) GUILayout.Space(3f);
            if (!IsFoldout) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.BeginHorizontal();
            GUI.changed = false;

            if (minimalistic)
            {
                if (IsFoldout) label = $"\u25BC{(char)0x200a}{label}";
                else label = $"\u25BA{(char)0x200a}{label}";

                style ??= new GUIStyle("PreToolbar2");

                GUILayout.BeginHorizontal();
                GUI.contentColor = EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.7f)
                    : new Color(0f, 0f, 0f, 0.7f);
                if (!GUILayout.Toggle(true, label, style, GUILayout.MinWidth(20f)))
                    IsFoldout = !IsFoldout;
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();
            }
            else
            {
                if (IsFoldout) label = $"\u25BC {label}";
                else label = $"\u25BA {label}";
                if (style == null)
                {
                    string styleString = IsFoldout ? "Button" : "DropDownButton";
                    style = new GUIStyle(styleString)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 11,
                        fontStyle = IsFoldout ? FontStyle.Bold : FontStyle.Normal
                    };
                }

                if (!GUILayout.Toggle(true, label, style, GUILayout.MinWidth(20f)))
                    IsFoldout = !IsFoldout;
            }

            if (GUI.changed) EditorPrefs.SetBool(key, IsFoldout);

            if (!minimalistic) GUILayout.Space(2f);
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            if (!IsFoldout) GUILayout.Space(3f);
        }
    }

    public class EditorObject<T> : IDraw
    {
        public Object value;
        public string label;
        public int labelWidth = 80;
        public int valueWidth;
        public bool showAsBox;
        public Object OutputValue { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            if (valueWidth == 0 && showAsBox)
                valueWidth = 34;

            Object result;

            if (showAsBox)
                result = EditorGUILayout.ObjectField(value, typeof(T), true, GUILayout.Width(valueWidth), GUILayout.Height(valueWidth));
            else
            {
                if (valueWidth == 0)
                    result = EditorGUILayout.ObjectField(value, typeof(T), true);
                else
                    result = EditorGUILayout.ObjectField(value, typeof(T), true, GUILayout.Width(valueWidth));
            }

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            OutputValue = result;
        }
    }

    public class EditorInt : IDraw
    {
        public string label;
        public int labelWidth = 80;
        public int valueWidth;
        public int value;
        public int min;
        public int max;
        public bool readOnly;
        public int OutputValue { get; private set; }

        public void Draw(GUIStyle style = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            if (style == null)
            {
                style = new GUIStyle(EditorStyles.textField)
                {
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 4, 4)
                };
                var normalColor = style.normal.textColor;
                normalColor.a = readOnly ? 0.5f : 1;
                style.normal.textColor = normalColor;
            }

            int result;
            if (valueWidth == 0)
            {
                if (min == max)
                    result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40));
                else
                    result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40));
            }
            else
            {
                if (min == max)
                    result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
                else
                    result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
            }

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            OutputValue = result;
        }
    }

    public static class EditorHelper
    {
#region File Utilities

        public static string SaveFilePanel(string mainDirectory, string defaultName, string content, string extension = "json,txt")
        {
            if (string.IsNullOrEmpty(mainDirectory))
                mainDirectory = Application.dataPath;

            string path = EditorUtility.SaveFilePanel("Save File", mainDirectory, defaultName, extension);
            if (!string.IsNullOrEmpty(path))
                SaveFile(path, content);
            return path;
        }

        public static void SaveFile(string path, string content)
        {
            if (!string.IsNullOrEmpty(content) && content != "{}")
            {
                if (File.Exists(path))
                    File.Delete(path);
                File.WriteAllText(path, content);
            }
        }

        /// <summary>
        /// T must be Serializable
        /// </summary>
        public static void SaveJsonFilePanel<T>(string pMainDirectory, string defaultName, T obj)
        {
            if (string.IsNullOrEmpty(pMainDirectory))
                pMainDirectory = Application.dataPath;

            string path = EditorUtility.SaveFilePanel("Save File", pMainDirectory, defaultName, "json,txt");
            if (!string.IsNullOrEmpty(path))
                SaveJsonFile(path, obj);
        }

        public static void SaveJsonFile<T>(string pPath, T pObj)
        {
            string jsonString = JsonUtility.ToJson(pObj);
            if (!string.IsNullOrEmpty(jsonString) && jsonString != "{}")
            {
                if (File.Exists(pPath))
                    File.Delete(pPath);
                File.WriteAllText(pPath, jsonString);
            }
        }

        /// <summary>
        /// T must be Serializable
        /// </summary>
        public static bool LoadJsonFilePanel<T>(string pMainDirectory, ref T pOutput)
        {
            if (string.IsNullOrEmpty(pMainDirectory))
                pMainDirectory = Application.dataPath;

            string path = EditorUtility.OpenFilePanel("Open File", pMainDirectory, "json,txt");
            if (string.IsNullOrEmpty(path))
                return false;

            return LoadJsonFromFile(path, ref pOutput);
        }

        private static string m_CacheMainDirectory;
        public static string LoadFilePanel(string pMainDirectory, string extensions = "json,txt")
        {
            if (string.IsNullOrEmpty(pMainDirectory))
                pMainDirectory = Application.dataPath;

            string path = EditorUtility.OpenFilePanel("Open File", string.IsNullOrEmpty(LastOpenedDirectory) ? pMainDirectory : LastOpenedDirectory, extensions);
            if (string.IsNullOrEmpty(path))
                return null;

            LastOpenedDirectory = Path.GetDirectoryName(path);
            return File.ReadAllText(path);
        }

        public static KeyValuePair<string, string> LoadFilePanel2(string pMainDirectory, string extensions = "json,txt")
        {
            if (string.IsNullOrEmpty(pMainDirectory))
                pMainDirectory = Application.dataPath;

            string path = EditorUtility.OpenFilePanel("Open File", string.IsNullOrEmpty(LastOpenedDirectory) ? pMainDirectory : LastOpenedDirectory, extensions);
            if (string.IsNullOrEmpty(path))
                return new KeyValuePair<string, string>();

            LastOpenedDirectory = Path.GetDirectoryName(path);
            string content = File.ReadAllText(path);
            return new KeyValuePair<string, string>(path, content);
        }

        public static bool LoadJsonFromFile<T>(string pPath, ref T pOutput)
        {
            if (!string.IsNullOrEmpty(pPath))
            {
                pOutput = JsonUtility.FromJson<T>(File.ReadAllText(pPath));
                return true;
            }

            return false;
        }

        public static void SaveXMLFile<T>(string pPath, T pObj)
        {
            if (File.Exists(pPath))
                File.Delete(pPath);
            var serializer = new XmlSerializer(typeof(T));
            using TextWriter writer = new StreamWriter(pPath);
            serializer.Serialize(writer, pObj);
        }

        public static T LoadXMLFile<T>(string pPath)
        {
            var serializer = new XmlSerializer(typeof(T));
            using TextReader reader = new StreamReader(pPath);
            var pObj = (T)serializer.Deserialize(reader);
            return pObj;
        }

        public static string GetBuildName()
        {
            bool developmentBuild = EditorUserBuildSettings.development;
            string bundleVersion = PlayerSettings.bundleVersion;
            string appName = PlayerSettings.productName.Replace(" ", "").RemoveSpecialCharacters();

            string name = $"{appName}_";
            string version = string.IsNullOrEmpty(bundleVersion) ? "" : $"v{bundleVersion}_";
            string bundleCode = "";
#if UNITY_ANDROID
            int bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            bundleCode = bundleVersionCode == 0 ? "" : $"b{bundleVersionCode}_";
#endif
            const string NAME_BUILD_PATTERN = "#ProductName#Version#BundleCode#Time";
            var time = DateTime.Now;
            string file = NAME_BUILD_PATTERN.Replace("#ProductName", name)
                .Replace("#Version", version)
                .Replace("#BundleCode", bundleCode)
                .Replace("#Time", $"{time.Year % 100}{time.Month:00}{time.Day:00}_{time.Hour:00}h{time.Minute:00}");
            file = file.Replace(" ", "_").Replace("/", "-").Replace(":", "-");
            file += developmentBuild ? "_dev" : "";
            return file;
        }
        
#endregion

        //========================================

#region Quick Shortcut

        /// <summary>
        /// Find all scene components, active or inactive.
        /// </summary>
        public static List<T> FindAll<T>() where T : Component
        {
            var comps = Resources.FindObjectsOfTypeAll(typeof(T)) as T[];

            var list = new List<T>();

            if (comps != null)
                foreach (var comp in comps)
                {
                    if (comp.gameObject.hideFlags == 0)
                    {
                        string path = AssetDatabase.GetAssetPath(comp.gameObject);
                        if (string.IsNullOrEmpty(path)) list.Add(comp);
                    }
                }

            return list;
        }

        public static void Save(Object pObj)
        {
            EditorUtility.SetDirty(pObj);
            AssetDatabase.SaveAssets();
        }

        public static T CreateScriptableAsset<T>(string path) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            var directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
                if (directoryPath != null)
                    Directory.CreateDirectory(directoryPath);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            return asset;
        }

        private static Dictionary<int, string> m_ObjectFolderCaches;
        public static string GetObjectFolderName(Object pObj)
        {
            m_ObjectFolderCaches ??= new Dictionary<int, string>();
            if (m_ObjectFolderCaches.ContainsKey(pObj.GetInstanceID()))
                return m_ObjectFolderCaches[pObj.GetInstanceID()];

            var path = AssetDatabase.GetAssetPath(pObj);
            var pathWithoutFilename = Path.GetDirectoryName(path);
            var pathSplit = pathWithoutFilename.Split(Path.DirectorySeparatorChar);
            string folder = pathSplit[pathSplit.Length - 1];
            m_ObjectFolderCaches.Add(pObj.GetInstanceID(), folder);
            return folder;
        }

        public static void ClearObjectFolderCaches() => m_ObjectFolderCaches = new Dictionary<int, string>();

        public static Object LoadAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadMainAssetAtPath(path);
        }

        /// <summary>
        /// Convenience function to load an asset of specified type, given the full path to it.
        /// </summary>
        public static T LoadAsset<T>(string path) where T : Object
        {
            var obj = LoadAsset(path);
            if (obj == null) return null;

            var val = obj as T;
            if (val != null) return val;

            if (typeof(T).IsSubclassOf(typeof(Component)))
            {
                if (obj is GameObject go)
                {
                    return go.GetComponent(typeof(T)) as T;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the specified object's GUID.
        /// </summary>
        public static string ObjectToGuid(Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(path) ? AssetDatabase.AssetPathToGUID(path) : null;
        }

#endregion

	    //========================================

#region Layout

        private static readonly Dictionary<int, Color> BoxColours = new Dictionary<int, Color>();

        public static void BoxVerticalOpen(int id, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            var defaultColor = GUI.backgroundColor;
            if (!BoxColours.ContainsKey(id))
                BoxColours.Add(id, defaultColor);

            if (color != default)
                GUI.backgroundColor = color;

            if (!isBox)
            {
                var style = new GUIStyle();
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                EditorGUILayout.BeginVertical(style);
            }
            else
            {
                var style = new GUIStyle("box");
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                EditorGUILayout.BeginVertical(style);
            }
        }

        public static void BoxVerticalClose(int id)
        {
            GUI.backgroundColor = BoxColours[id];
            EditorGUILayout.EndVertical();
        }

        public static Rect BoxVertical(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            Rect rect;
            var defaultColor = GUI.backgroundColor;
            if (color != default)
                GUI.backgroundColor = color;

            if (!isBox)
            {
                var style = new GUIStyle();
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginVertical(style);
            }
            else
            {
                var style = new GUIStyle("box");
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginVertical(style);
            }

            doSomething();
            
            EditorGUILayout.EndVertical();
            if (color != default)
                GUI.backgroundColor = defaultColor;

            return rect;
        }

        public static Rect BoxVertical(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            Rect rect;
            var defaultColor = GUI.backgroundColor;
            if (color != default)
                GUI.backgroundColor = color;

            if (!isBox)
            {
                var style = new GUIStyle();
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginVertical(style);
            }
            else
            {
                var style = new GUIStyle("box");
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginVertical(style);
            }

            if (!string.IsNullOrEmpty(pTitle))
                DrawHeaderTitle(pTitle);

            doSomething();

            EditorGUILayout.EndVertical();
            if (color != default)
                GUI.backgroundColor = defaultColor;

            return rect;
        }

        public static Rect BoxHorizontal(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            Rect rect;
            var defaultColor = GUI.backgroundColor;
            if (color != default)
                GUI.backgroundColor = color;

            if (!isBox)
            {
                var style = new GUIStyle();
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginHorizontal(style);
            }
            else
            {
                var style = new GUIStyle("box");
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginHorizontal(style);
            }

            doSomething();

            EditorGUILayout.EndHorizontal();

            if (color != default)
                GUI.backgroundColor = defaultColor;
            return rect;
        }

        public static Rect BoxHorizontal(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            Rect rect;
            var defaultColor = GUI.backgroundColor;
            if (color != default)
                GUI.backgroundColor = color;

            if (!string.IsNullOrEmpty(pTitle))
            {
                EditorGUILayout.BeginVertical();
                DrawHeaderTitle(pTitle);
            }

            if (!isBox)
            {
                var style = new GUIStyle();
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginHorizontal(style);
            }
            else
            {
                var style = new GUIStyle("box");
                if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
                if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
                rect = EditorGUILayout.BeginHorizontal(style);
            }

            doSomething();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(pTitle))
                EditorGUILayout.EndVertical();

            if (color != default)
                GUI.backgroundColor = defaultColor;

            return rect;
        }

        public static void GridDraws(int pCell, List<IDraw> pDraws, Color color = default)
        {
            int row = Mathf.CeilToInt(pDraws.Count * 1f / pCell);
            var bgColor = GUI.backgroundColor;
            if (color != default)
                GUI.backgroundColor = color;
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
            if (color != default)
                GUI.backgroundColor = bgColor;
        }
        
        public static void DrawLine(float padding = 0)
        {
	        if (padding > 0)
		        EditorGUILayout.Space(padding);
	        var lineColor = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.6f, 0.6f, 0.6f);
	        var originalColor = GUI.color;
	        GUI.color = lineColor;
	        float lineThickness = 1;
	        var lineRect = EditorGUILayout.GetControlRect(false, lineThickness);
	        EditorGUI.DrawRect(lineRect, lineColor);
	        GUI.color = originalColor;
	        if (padding > 0)
		        EditorGUILayout.Space(padding);
        }

        public static void Separator(string label = null, Color labelColor = default)
        {
            if (string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();

                // Left separator line
                GUILayout.Label("", GUI.skin.horizontalSlider);

                // Set bold and colored style for the label
                var boldStyle = new GUIStyle(GUI.skin.label);
                boldStyle.fontStyle = FontStyle.Bold;
                boldStyle.alignment = TextAnchor.MiddleCenter; // Center the label vertically
                if (labelColor != default)
                    GUI.contentColor = labelColor; // Set the desired color (red in this example)

                // Label with "Editor" in bold and color
                GUILayout.Label(label, boldStyle, GUILayout.Width(50), GUILayout.ExpandWidth(false));

                // Reset content color to default (to avoid affecting other GUI elements)
                GUI.contentColor = Color.white;

                // Right separator line
                GUILayout.Label("", GUI.skin.horizontalSlider);

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
        }

        /// <summary>
        /// Draw a visible separator in addition to adding some padding.
        /// </summary>
        public static void SeparatorBox()
        {
            GUILayout.Space(10);

            if (Event.current.type == EventType.Repaint)
            {
                var tex = EditorGUIUtility.whiteTexture;
                var rect = GUILayoutUtility.GetLastRect();
                GUI.color = new Color(0f, 0f, 0f, 0.25f);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
                GUI.color = Color.white;
            }
        }

        public static Vector2 ScrollBar(ref Vector2 scrollPos, float width, float height, string label, Action action)
        {
            EditorGUILayout.BeginVertical("box");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height));
            action();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            return scrollPos;
        }

#endregion

	    //========================================

#region Tools

        public static bool Button(string pLabel, int pWidth = 0, int pHeight = 0)
        {
            var button = new EditorButton()
            {
                label = pLabel,
                width = pWidth,
                height = pHeight
            };
            button.Draw();
            return button.IsPressed;
        }

        public static void Button(string pLabel, Action pOnPressed, int pWidth = 0, int pHeight = 0)
        {
            var button = new EditorButton()
            {
                label = pLabel,
                width = pWidth,
                onPressed = pOnPressed,
                height = pHeight,
            };
            button.Draw();
        }

        public static bool ButtonColor(string pLabel, Color pColor = default, int pWidth = 0, int pHeight = 0)
        {
            var button = new EditorButton()
            {
                label = pLabel,
                width = pWidth,
                color = pColor,
                height = pHeight,
            };
            button.Draw();
            return button.IsPressed;
        }

        public static void ButtonColor(string pLabel, Action pOnPressed, Color pColor = default, int pWidth = 0, int pHeight = 0)
        {
            var button = new EditorButton()
            {
                label = pLabel,
                width = pWidth,
                color = pColor,
                onPressed = pOnPressed,
                height = pHeight,
            };
            button.Draw();
        }

        public static bool Button(string pLabel, GUIStyle pStyle)
        {
            var button = new EditorButton()
            {
                label = pLabel,
            };
            button.Draw(pStyle);
            return button.IsPressed;
        }

        public static string FolderField(string defaultPath, string label, int labelWidth = 0, bool pFormatToUnityPath = true)
        {
	        EditorGUILayout.BeginHorizontal();
	        var newPath = TextField(defaultPath, label, labelWidth > 0 ? labelWidth : label.Length * 7);
	        if (Button("...", 25))
	        {
		        newPath = EditorUtility.OpenFolderPanel("Select Folder", string.IsNullOrEmpty(defaultPath) ? LastOpenedDirectory : defaultPath, "");
		        if (!string.IsNullOrEmpty(newPath))
		        {
			        if (pFormatToUnityPath && newPath.StartsWith(Application.dataPath))
				        newPath = FormatPathToUnityPath(newPath);
			        LastOpenedDirectory = newPath;
		        }
	        }
	        EditorGUILayout.EndHorizontal();
	        return string.IsNullOrEmpty(newPath) ? defaultPath : newPath;
        }
        
        public static string FileField(string defaultPath, string label, string extension, int labelWidth = 0, bool pFormatToUnityPath = true)
        {
	        EditorGUILayout.BeginHorizontal();
	        var newPath = TextField(defaultPath, label, labelWidth > 0 ? labelWidth : label.Length * 7);
	        if (Button("...", 25))
	        {
		        newPath = EditorUtility.OpenFilePanel("Select Folder", string.IsNullOrEmpty(defaultPath) ? LastOpenedDirectory : defaultPath, extension);
		        if (!string.IsNullOrEmpty(newPath))
		        {
			        LastOpenedDirectory = Path.GetDirectoryName(newPath);
			        if (pFormatToUnityPath && newPath.StartsWith(Application.dataPath))
				        newPath = FormatPathToUnityPath(newPath);
		        }
	        }
	        EditorGUILayout.EndHorizontal();
	        return string.IsNullOrEmpty(newPath) ? defaultPath : newPath;
        }

        public static bool Foldout(string label)
        {
            var foldout = new EditorFoldout()
            {
                label = label,
            };
            foldout.Draw();
            return foldout.IsFoldout;
        }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        public static bool HeaderFoldout(string label, string key = "", bool minimalistic = false, params IDraw[] pHorizontalDraws)
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

        public static bool ConfirmPopup(string pMessage = null, string pYes = null, string pNo = null)
        {
            if (string.IsNullOrEmpty(pMessage))
                pMessage = "Are you sure you want to do this";
            if (string.IsNullOrEmpty(pYes))
                pYes = "Yes";
            if (string.IsNullOrEmpty(pNo))
                pNo = "No";
            return EditorUtility.DisplayDialog("Confirm your action", pMessage, pYes, pNo);
        }

        public static void ListReadonlyObjects<T>(string pName, List<T> pList, List<string> pLabels = null,
            bool pShowObjectBox = true) where T : Object
        {
            ListObjects(pName, ref pList, pLabels, pShowObjectBox, true);
        }

        //public static List<T> OrderedListObject<T>(List<T> list, bool pShowObjectBox) where T : UnityEngine.Object
        //{
        //    var reorderableList = new ReorderableList(list, typeof(T), true, false, true, true);
        //    reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        //    {
        //        int lineHeight = pShowObjectBox ? 34 : 17;
        //        Rect rtIndex = new Rect(rect.x, rect.y, 17, lineHeight);
        //        Rect rtBox = new Rect();
        //        if (pShowObjectBox)
        //            rtBox = new Rect(rect.x + 17, rect.y, 34, lineHeight);
        //        Rect rtMain = new Rect(rect.x + rtIndex.width + rtBox.width, rect.y, rect.width - rtIndex.width - rtBox.width, 17);

        //        EditorGUI.LabelField(rtIndex, (index + 1).ToString());
        //        list[index] = (T)EditorGUI.ObjectField(rtBox, list[index], typeof(T), false);
        //        list[index] = (T)EditorGUI.ObjectField(rtMain, list[index], typeof(T), false);
        //    };
        //    reorderableList.onAddCallback = (lli) =>
        //    {
        //        list.Add(default(T));
        //    };
        //    reorderableList.elementHeight = pShowObjectBox ? 34 : 17;
        //    reorderableList.DoLayoutList();
        //    return list;
        //}

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
                                LabelField(pLabels[i], (int)Mathf.Max(pLabels[i].Length * 8f, 100), false);
                            list[i] = (T)ObjectField<T>(list[i], "");
                            if (pShowObjectBox)
	                            list[i] = (T)ObjectField<T>(list[i], "", 0, boxSize, true);
                            
                            if (!pReadOnly)
                            {
                                if (Button("▲", 23) && i > 0)
	                                (list[i], list[i - 1]) = (list[i - 1], list[i]);

                                if (Button("▼", 23) && i < list.Count - 1)
	                                (list[i], list[i + 1]) = (list[i + 1], list[i]);

                                if (ButtonColor("-", Color.red, 23))
                                {
	                                list.RemoveAt(i);
	                                i--;
                                }
                                
                                if (ButtonColor("+", Color.green, 23))
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
                            if (ButtonColor("+1", Color.green, 30))
                            {
                                list.Add(null);
                                page = totalPages - 1;
                                EditorPrefs.SetInt($"{pName}_page", page);
                            }

                            if (GUILayout.Button("Sort By Name"))
                                list = list.OrderBy(m => m.name).ToList();
                            if (GUILayout.Button("Remove Duplicate"))
                                list.RemoveDuplicated();

                            if (ButtonColor("Clear", Color.red, 50))
                                if (ConfirmPopup())
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
            GUILayout.Space(3);

            var prevColor = GUI.color;
            GUI.backgroundColor = new Color(1, 1, 0.5f);

            int page = EditorPrefs.GetInt($"{pName}_page", 0);
            int totalPages = Mathf.CeilToInt(pCount * 1f / 20f);
            if (totalPages == 0)
                totalPages = 1;
            if (page < 0)
                page = 0;
            if (page >= totalPages)
                page = totalPages - 1;
            int from = page * 20;
            int to = page * 20 + 20 - 1;
            if (to > pCount - 1)
                to = pCount - 1;

            EditorGUILayout.BeginVertical("box");
            {
                if (totalPages > 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (Button("\u25c4", 23))
                    {
                        if (page > 0)
                            page--;
                        EditorPrefs.SetInt($"{pName}_page", page);
                    }

                    EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({pCount})");

                    if (p_drawAtFirst != null)
                    {
                        foreach (var draw in p_drawAtFirst)
                            draw.Draw();
                    }

                    if (Button("\u25ba", 23))
                    {
                        if (page < totalPages - 1)
                            page++;
                        EditorPrefs.SetInt($"{pName}_page", page);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                for (int i = from; i <= to; i++)
                {
                    pOnDraw?.Invoke(i);
                }

                if (totalPages > 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (Button("\u25c4", 23))
                    {
                        if (page > 0)
                            page--;
                        EditorPrefs.SetInt($"{pName}_page", page);
                    }

                    EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({pCount})");

                    if (p_drawAtLast != null)
                    {
                        foreach (var draw in p_drawAtLast)
                            draw.Draw();
                    }

                    if (Button("\u25ba", 23))
                    {
                        if (page < totalPages - 1)
                            page++;
                        EditorPrefs.SetInt($"{pName}_page", page);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = prevColor;
        }

        public static void ListObjectsWithSearch<T>(ref List<T> pList, string pName, bool pShowObjectBox = true) where T : Object
        {
            var prevColor = GUI.color;
            GUI.backgroundColor = new Color(1, 1, 0.5f);

            //bool show = EditorPrefs.GetBool(pName, false);
            //GUIContent content = new GUIContent(pName);
            //GUIStyle style = new GUIStyle(EditorStyles.foldout);
            //style.margin = new RectOffset(pInBox ? 13 : 0, 0, 0, 0);
            //show = EditorGUILayout.Foldout(show, content, style);

            var list = pList;
            string search = EditorPrefs.GetString($"{pName}_search");
            var show = HeaderFoldout($"{pName} ({pList.Count})", pName);
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
                
				EditorGUILayout.BeginVertical("true");
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

                    var list1 = list;
                    DragDropBox<T>(pName, (objs) =>
                    {
                        list1.AddRange(objs);
                    });

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
                        if (GUILayout.Button("<Next<"))
                        {
                            if (page < totalPages - 1)
                                page++;
                            EditorPrefs.SetInt($"{pName}_page", page);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    search = GUILayout.TextField(search);

                    bool searching = !string.IsNullOrEmpty(search);
                    for (int i = from; i <= to; i++)
                    {
	                    if (i >= list.Count)
		                    continue;
	                    
                        if (searching && !list[i].name.Contains(search))
                            continue;

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
                            list[i] = (T)ObjectField<T>(list[i], "");
                            if (pShowObjectBox)
	                            list[i] = (T)ObjectField<T>(list[i], "", 0, boxSize, true);
                            
                            if (Button("▲", 23) && i > 0)
	                            (list[i], list[i - 1]) = (list[i - 1], list[i]);

                            if (Button("▼", 23) && i < list.Count - 1)
	                            (list[i], list[i + 1]) = (list[i + 1], list[i]);

                            if (ButtonColor("-", Color.red, 23))
                            {
	                            list.RemoveAt(i);
	                            i--;
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


                    EditorGUILayout.BeginHorizontal();
                    {
                        if (ButtonColor("+1", Color.green, 30))
                        {
                            list.Add(null);
                            page = totalPages - 1;
                            EditorPrefs.SetInt($"{pName}_page", page);
                        }

                        if (GUILayout.Button("Sort By Name"))
                            list = list.OrderBy(m => m.name).ToList();
                        if (GUILayout.Button("Remove Duplicate"))
                            list.RemoveDuplicated();

                        if (ButtonColor("Clear", Color.red, 50))
                            if (ConfirmPopup())
                                list = new List<T>();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            pList = list;

            if (GUI.changed)
            {
                EditorPrefs.SetBool(pName, show);
                EditorPrefs.SetString($"{pName}_search", search);
            }

            GUI.backgroundColor = prevColor;
        }

        public static bool ListKeyObjects<TKey, TValue>(string pName, ref List<SerializableKeyValue<TKey, TValue>> pList, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null)
	        where TValue : Object
        {
            GUILayout.Space(3);

            var prevColor = GUI.color;
            GUI.backgroundColor = new Color(1, 1, 0.5f);

            var list = pList;
            var show = HeaderFoldout($"{pName} ({pList.Count})", pName);
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
                        DragDropBox<TValue>(pName, (objs) =>
                        {
                            foreach (var value in objs)
                                list1.Add(new SerializableKeyValue<TKey, TValue>(default, value));
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
	                    if (i >= list.Count || list[i] == null)
	                        continue;
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
                            if (typeof(TKey).IsEnum)
                            {
	                            var key = list[i].k;
	                            var newKey = (TKey)Enum.Parse(typeof(TKey), EditorGUILayout.EnumPopup((Enum)Enum.ToObject(typeof(TKey), key)).ToString());
	                            list[i].k = newKey;
                            }
                            else if (typeof(TKey) == typeof(int))
                            {
	                            int key = Convert.ToInt32(list[i].k);
	                            int newKey = EditorGUILayout.IntField(key, EditorStyles.textField, GUILayout.MinWidth(40));
	                            list[i].k = (TKey)(object)newKey;
                            }
                            else if (typeof(TKey) == typeof(string))
                            {
	                            string key = list[i].k.ToString();
	                            string newKey = EditorGUILayout.TextField(key, EditorStyles.textField, GUILayout.MinWidth(40));
	                            list[i].k = (TKey)(object)newKey;
                            }
                            list[i].v = (TValue)ObjectField<TValue>(list[i].v, "");
                            if (pShowObjectBox)
	                            list[i].v = (TValue)ObjectField<TValue>(list[i].v, "", 0, boxSize, true);

                            if (!pReadOnly)
                            {
                                if (Button("▲", 23) && i > 0)
                                    (list[i], list[i - 1]) = (list[i - 1], list[i]);

                                if (Button("▼", 23) && i < list.Count - 1)
                                    (list[i], list[i + 1]) = (list[i + 1], list[i]);
                            
                                if (ButtonColor("-", Color.red, 23))
                                {
                                    list.RemoveAt(i);
                                    i--;
                                }
                            
                                if (ButtonColor("+", Color.green, 23))
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
                            if (ButtonColor("+1", Color.green, 30))
                            {
                                list.Add(null);
                                page = totalPages - 1;
                                EditorPrefs.SetInt($"{pName}_page", page);
                            }
                            if (GUILayout.Button("Sort By Name"))
                                list = list.OrderBy(m => m.v.name).ToList();
                            if (GUILayout.Button("Remove Duplicated Key"))
                                list.RemoveDuplicatedKey();
                            if (GUILayout.Button("Remove Duplicated Value"))
                                list.RemoveDuplicatedValue();
                            if (ButtonColor("Clear", Color.red, 50))
                                if (ConfirmPopup())
                                    list = new List<SerializableKeyValue<TKey, TValue>>();
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (pAdditionalDraws != null)
                        foreach (var draw in pAdditionalDraws)
                            draw.Draw();
                }
                EditorGUILayout.EndVertical();
            }

            pList = list;

            if (GUI.changed)
                EditorPrefs.SetBool(pName, show);

            GUI.backgroundColor = prevColor;

            return show;
        }
        
        public static string Tabs(string pKey, params string[] pTabsName)
        {
            var tabs = new EditorTabs()
            {
                key = pKey,
                tabsName = pTabsName,
            };
            tabs.Draw();
            return tabs.CurrentTab;
        }

        private static void DrawHeaderTitle(string pHeader)
        {
            var prevColor = GUI.color;

            var boxStyle = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 0,
                padding = new RectOffset(5, 5, 5, 5)
            };

            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleCenter
            };

            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            EditorGUILayout.BeginHorizontal(boxStyle, GUILayout.Height(20));
            {
                GUI.color = prevColor;
                EditorGUILayout.LabelField(pHeader, titleStyle, GUILayout.Height(20));
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DragDropBox<T>(string pName, Action<T[]> pOnDrop) where T : Object
        {
            var evt = Event.current;
            var style = new GUIStyle("Toolbar");
            var dropArea = GUILayoutUtility.GetRect(0.0f, 30, style, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, $"Drag drop {pName}");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        var objs = new List<T>();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj == null)
                                continue;

                            if (obj is GameObject gameObject && typeof(T).IsSubclassOf(typeof(Component)))
                            {
                                var component = gameObject.GetComponent<T>();
                                if (component != null)
                                    objs.Add(component);
                            }
                            else
                            {
                                var path = AssetDatabase.GetAssetPath(obj);
                                if (IsDirectory(path))
                                {
                                    var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
                                    foreach (var guid in guids)
                                    {
                                        var subObjPath = AssetDatabase.GUIDToAssetPath(guid);
                                        var subObj = AssetDatabase.LoadAssetAtPath<T>(subObjPath);
                                        if (subObj != null)
                                            objs.Add(subObj);
                                    }
                                }
                                else
                                {
                                    var s = obj as T;
                                    if (s != null)
                                        objs.Add(s);
                                }
                            }
                        }

                        pOnDrop(objs.ToArray());
                    }

                    break;
            }
        }

        public static void DrawTextureIcon(Texture pTexture, Vector2 pSize)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(pSize.x), GUILayout.Height(pSize.y));
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            if (pTexture != null)
                GUI.DrawTexture(rect, pTexture, ScaleMode.ScaleToFit);
            else
                GUI.DrawTexture(rect, EditorGUIUtility.FindTexture("console.warnicon"), ScaleMode.ScaleToFit);
        }

        public static ReorderableList CreateReorderableList<T>(T[] pObjects, string pName) where T : Object
        {
            var reorderableList = new ReorderableList(pObjects, typeof(T), true, false, true, true);
            reorderableList.drawElementCallback += (rect, index, a, b) =>
            {
                pObjects[index] = (T)EditorGUI.ObjectField(rect, pObjects[index], typeof(T), true);
            };
            reorderableList.elementHeight = 17f;
            reorderableList.headerHeight = 17f;
            reorderableList.drawHeaderCallback += (rect) =>
            {
                EditorGUI.LabelField(rect, pName);
            };
            return reorderableList;
        }

        public static ReorderableList CreateReorderableList<T>(List<T> pObjects, string pName) where T : Object
        {
            var reorderableList = new ReorderableList(pObjects, typeof(T), true, false, true, true);
            reorderableList.drawElementCallback += (rect, index, a, b) =>
            {
                pObjects[index] = (T)EditorGUI.ObjectField(rect, pObjects[index], typeof(T), true);
            };
            reorderableList.elementHeight = 17f;
            reorderableList.headerHeight = 17f;
            reorderableList.drawHeaderCallback += (rect) =>
            {
                EditorGUI.LabelField(rect, pName);
            };
            return reorderableList;
        }

        public static void ReplaceGameObjectsInScene(ref List<GameObject> selections, List<GameObject> prefabs)
        {
            for (var i = selections.Count - 1; i >= 0; --i)
            {
                GameObject newObject;
                var selected = selections[i];
                var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
                if (prefab.IsPrefab())
                {
                    newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    newObject = Object.Instantiate(prefab);
                    newObject.name = prefab.name;
                }

                if (newObject == null)
                {
                    UnityEngine.Debug.LogError("Error instantiating prefab");
                    break;
                }

                Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
                newObject.transform.parent = selected.transform.parent;
                newObject.transform.localPosition = selected.transform.localPosition;
                newObject.transform.localRotation = selected.transform.localRotation;
                newObject.transform.localScale = selected.transform.localScale;
                newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
                Undo.DestroyObjectImmediate(selected);
                selections[i] = newObject;
            }
        }

#endregion

	    //========================================

#region Input Fields

        public static string TextField(string value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false, Color color = default)
        {
            var text = new EditorText()
            {
                value = value,
                label = label,
                labelWidth = labelWidth,
                valueWidth = valueWidth,
                readOnly = readOnly,
                color = color,
            };
            text.Draw();
            return text.OutputValue;
        }

        public static string TextArea(string value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false)
        {
            var text = new EditorText()
            {
                value = value,
                label = label,
                labelWidth = labelWidth,
                valueWidth = valueWidth,
                readOnly = readOnly,
                textArea = true,
            };
            text.Draw();
            return text.OutputValue;
        }

        public static string DropdownList(string value, string label, string[] selections, int labelWidth = 80, int valueWidth = 0)
        {
            var dropdownList = new EditorDropdownListString()
            {
                label = label,
                labelWidth = labelWidth,
                selections = selections,
                value = value,
                valueWidth = valueWidth,
            };
            dropdownList.Draw();
            return dropdownList.OutputValue;
        }

        public static int DropdownList(int value, string label, int[] selections, int labelWidth = 80, int valueWidth = 0)
        {
            var dropdownList = new EditorDropdownListInt()
            {
                label = label,
                labelWidth = labelWidth,
                value = value,
                valueWidth = valueWidth,
                selections = selections
            };
            dropdownList.Draw();
            return dropdownList.OutputValue;
        }

        public static T DropdownListEnum<T>(T value, string label, int labelWidth = 80, int valueWidth = 0) where T : struct, IConvertible
        {
            var dropdownList = new EditorDropdownListEnum<T>()
            {
                label = label,
                labelWidth = labelWidth,
                value = value,
                valueWidth = valueWidth,
            };
            dropdownList.Draw();
            return dropdownList.OutputValue;
        }

        public static T DropdownList<T>(T selectedObj, string label, List<T> pOptions) where T : Object
        {
            string selectedName = selectedObj == null ? "None" : selectedObj.name;
            string[] options = new string[pOptions.Count + 1];
            options[0] = "None";
            for (int i = 1; i < pOptions.Count + 1; i++)
            {
                if (pOptions[i - 1] != null)
                    options[i] = pOptions[i - 1].name;
                else
                    options[i] = "NULL";
            }

            var selected = DropdownList(selectedName, label, options);
            if (selectedName != selected)
            {
                selectedName = selected;
                foreach (var o in pOptions)
                {
                    if (o.name == selectedName)
                    {
                        selectedObj = o;
                        break;
                    }
                }

                if (selectedName == "None")
                    selectedObj = null;
            }

            return selectedObj;
        }

        public static bool Toggle(bool value, string label, int labelWidth = 80, int valueWidth = 0, Color color = default)
        {
            var toggle = new EditorToggle()
            {
                label = label,
                labelWidth = labelWidth,
                value = value,
                valueWidth = valueWidth,
                color = color,
            };
            toggle.Draw();
            return toggle.OutputValue;
        }

        public static int IntField(int value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false, int pMin = 0, int pMax = 0)
        {
            var intField = new EditorInt()
            {
                value = value,
                label = label,
                labelWidth = labelWidth,
                valueWidth = valueWidth,
                readOnly = readOnly,
                min = pMin,
                max = pMax
            };
            intField.Draw();
            return intField.OutputValue;
        }

        public static float FloatField(float value, string label, int labelWidth = 80, int valueWidth = 0, float pMin = 0, float pMax = 0)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            float result;
            if (valueWidth == 0)
            {
                if (pMin != pMax)
                    result = EditorGUILayout.Slider(value, pMin, pMax, GUILayout.Height(20));
                else
                    result = EditorGUILayout.FloatField(value, GUILayout.Height(20));
            }
            else
            {
                if (pMin != pMax)
                    result = EditorGUILayout.Slider(value, pMin, pMax, GUILayout.Height(20), GUILayout.Width(valueWidth));
                else
                    result = EditorGUILayout.FloatField(value, GUILayout.Height(20), GUILayout.Width(valueWidth));
            }

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            return result;
        }

        public static Object ObjectField<T>(Object value, string label, int labelWidth = 80, int valueWidth = 0, bool showAsBox = false)
        {
            var obj = new EditorObject<T>()
            {
                value = value,
                label = label,
                labelWidth = labelWidth,
                valueWidth = valueWidth,
                showAsBox = showAsBox
            };
            obj.Draw();
            return obj.OutputValue;
        }

        public static void LabelField(string label, int width = 0, bool isBold = true, TextAnchor pTextAnchor = TextAnchor.MiddleLeft, Color pTextColor = default)
        {
            var style = new GUIStyle(isBold ? EditorStyles.boldLabel : EditorStyles.label)
            {
                alignment = pTextAnchor,
                margin = new RectOffset(0, 0, 0, 0)
            };
            if (pTextColor != default)
                style.normal.textColor = pTextColor;
            if (width > 0)
                EditorGUILayout.LabelField(label, style, GUILayout.MinWidth(width), GUILayout.MaxWidth(width));
            else
                EditorGUILayout.LabelField(label, style);
        }

        public static Color ColorField(Color value, string label, int labelWidth = 80, int valueWidth = 0)
        {
            var colorField = new EditorColor()
            {
                value = value,
                label = label,
                labelWidth = labelWidth,
                valueWidth = valueWidth
            };
            colorField.Draw();
            return colorField.outputValue;
        }

        public static Vector2 Vector2Field(Vector2 value, string label, int labelWidth = 80, int valueWidth = 0)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            Vector2 result;
            if (valueWidth == 0)
                result = EditorGUILayout.Vector2Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40));
            else
                result = EditorGUILayout.Vector2Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40),
                    GUILayout.Width(valueWidth));

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            return result;
        }

        public static Vector3 Vector3Field(Vector3 value, string label, int labelWidth = 80, int valueWidth = 0)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            Vector3 result;
            if (valueWidth == 0)
                result = EditorGUILayout.Vector3Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40));
            else
                result = EditorGUILayout.Vector3Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40),
                    GUILayout.Width(valueWidth));

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            return result;
        }

        public static float[] ArrayField(float[] values, string label, bool showHorizontal = true, int labelWidth = 80,
            int valueWidth = 0)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            }

            if (showHorizontal)
                EditorGUILayout.BeginHorizontal();
            else
                EditorGUILayout.BeginVertical();
            float[] results = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                float result;
                if (valueWidth == 0)
                    result = EditorGUILayout.FloatField(values[i], GUILayout.Height(20));
                else
                    result = EditorGUILayout.FloatField(values[i], GUILayout.Height(20), GUILayout.Width(valueWidth));

                results[i] = result;
            }

            if (showHorizontal)
                EditorGUILayout.EndHorizontal();
            else
                EditorGUILayout.EndVertical();

            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.EndHorizontal();

            return results;
        }



        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }

            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }

            return enm.Current;
        }

#endregion
	    
	    //========================================

#region SerializedProperty SerializedObject

		public static void SerializeFields(this SerializedProperty pProperty, params string[] properties)
        {
            foreach (var p in properties)
            {
                var item = pProperty.FindPropertyRelative(p);
                EditorGUILayout.PropertyField(item, true);
            }
        }

        public static void SerializeFields(this SerializedObject pObj, params string[] properties)
        {
            foreach (var p in properties)
                pObj.SerializeField(p);
        }

        public static SerializedProperty SerializeField(this SerializedObject pObj, string pPropertyName, string pDisplayName = null, params GUILayoutOption[] options)
        {
            var property = pObj.FindProperty(pPropertyName);
            if (property == null)
            {
                UnityEngine.Debug.LogError($"Not found property {pPropertyName}");
                return null;
            }

            if (!property.isArray)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(string.IsNullOrEmpty(pDisplayName) ? property.displayName : pDisplayName));
                return property;
            }

            if (property.isExpanded)
                EditorGUILayout.PropertyField(property, true, options);
            else
                EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), options);
            return property;
        }

        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal))
                        .Replace("[", "")
                        .Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }

            return obj;
        }

        public static bool IsFirstElementOfList(this SerializedProperty property)
        {
	        string path = property.propertyPath;
	        int index = path.LastIndexOf('[');
	        if (index < 0)
		        return false;

	        int endIndex = path.IndexOf(']', index);
	        if (endIndex < 0)
		        return false;

	        int elementIndex = int.Parse(path.Substring(index + 1, endIndex - index - 1));
	        return elementIndex == 0;
        }

        public static bool IsInList(this SerializedProperty property)
        {
	        return property.displayName.Contains("Element");
        }
        
#endregion

	    //========================================

#region Build

        public static void RemoveDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
            if (string.IsNullOrEmpty(pSymbol))
                return;
            var target = pTarget == BuildTargetGroup.Unknown
                ? EditorUserBuildSettings.selectedBuildTargetGroup
                : pTarget;
            string directives = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            directives = directives.Replace(pSymbol, "");
            if (directives.Length > 1 && directives[directives.Length - 1] == ';')
                directives = directives.Remove(directives.Length - 1, 1);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directives);
        }

        public static void RemoveDirective(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
            var target = pTarget == BuildTargetGroup.Unknown
                ? EditorUserBuildSettings.selectedBuildTargetGroup
                : pTarget;
            string directives = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            foreach (var s in pSymbols)
            {
                if (directives.Contains($"{s};"))
                    directives = directives.Replace($"{s};", "");
                else if (directives.Contains(s))
                    directives = directives.Replace(s, "");
            }

            if (directives.Length > 1 && directives[directives.Length - 1] == ';')
                directives = directives.Remove(directives.Length - 1, 1);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directives);
        }

        public static void AddDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
            var target = pTarget == BuildTargetGroup.Unknown
                ? EditorUserBuildSettings.selectedBuildTargetGroup
                : pTarget;
            string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            string[] directives = directivesStr.Split(';');
            foreach (var d in directives)
                if (d == pSymbol)
                    return;

            if (string.IsNullOrEmpty(directivesStr))
                directivesStr += pSymbol;
            else
                directivesStr += $";{pSymbol}";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directivesStr);
        }

        public static void AddDirectives(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
            var target = pTarget == BuildTargetGroup.Unknown
                ? EditorUserBuildSettings.selectedBuildTargetGroup
                : pTarget;
            string directivesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            string[] directives = directivesStr.Split(';');
            foreach (var s in pSymbols)
            {
                bool existed = false;
                foreach (var d in directives)
                    if (d == s)
                    {
                        existed = true;
                        break;
                    }

                if (existed)
                    continue;

                if (string.IsNullOrEmpty(directivesStr))
                    directivesStr += s;
                else
                    directivesStr += $";{s}";
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, directivesStr);
        }

        public static string[] GetDirectives(BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
            var target = pTarget == BuildTargetGroup.Unknown
                ? EditorUserBuildSettings.selectedBuildTargetGroup
                : pTarget;
            string defineStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            string[] currentDefines = defineStr.Split(';');
            for (int i = 0; i < currentDefines.Length; i++)
                currentDefines[i] = currentDefines[i].Trim();
            return currentDefines;
        }

        public static bool ContainDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown)
        {
            var directives = GetDirectives(pTarget);
            foreach (var d in directives)
                if (d == pSymbol)
                    return true;
            return false;
        }

        public static string[] GetScenePaths()
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            string[] scenes = new string[sceneCount];
            for (int i = 0; i < sceneCount; i++)
            {
                scenes[i] = Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility
                    .GetScenePathByBuildIndex(i));
            }

            return scenes;
        }

#endregion

	    //========================================

#region Get / Load Assets

        public static string OpenFolderPanel(string pFolderPath = null)
        {
	        if (string.IsNullOrEmpty(pFolderPath))
		        pFolderPath = LastOpenedDirectory;
	        if (string.IsNullOrEmpty(pFolderPath))
				pFolderPath ??= Application.dataPath;
            string path = EditorUtility.OpenFolderPanel("Select Folder", pFolderPath, "");
            if (!string.IsNullOrEmpty(path))
				LastOpenedDirectory = path;
            return path;
        }

        public static string FormatPathToUnityPath(string path)
        {
            string[] paths = path.Split('/');

            int startJoint = -1;
            string realPath = "";

            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] == "Assets")
	                startJoint = i;

                if (startJoint != -1 && i >= startJoint)
                {
                    if (i == paths.Length - 1)
                        realPath += paths[i];
                    else
                        realPath += $"{paths[i]}/";
                }
            }

            return realPath;
        }

        public static string[] GetDirectories(string path)
        {
            var directories = Directory.GetDirectories(path);

            if (directories.Length > 0)
            {
                for (int i = 0; i < directories.Length; i++)
                    directories[i] = FormatPathToUnityPath(directories[i]);

                return directories;
            }

            return new[] { FormatPathToUnityPath(path) };
        }

        private static T Assign<T>(string pPath) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath(pPath, typeof(T)) as T;
        }

        /// <summary>
        /// Example: GetObjects<AudioClip>(@"Assets\Game\Sounds\Musics", "t:AudioClip")
        /// </summary>
        /// <returns></returns>
        public static List<T> GetObjects<T>(string pPath, string filter, bool getChild = true)
            where T : Object
        {
            var directories = GetDirectories(pPath);

            var list = new List<T>();

            var resources = AssetDatabase.FindAssets(filter, directories);

            foreach (var re in resources)
            {
                if (getChild)
                {
                    var childAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(re));
                    foreach (var child in childAssets)
                    {
                        if (child is T o)
                        {
                            list.Add(o);
                        }
                    }
                }
                else
                {
                    list.Add(Assign<T>(AssetDatabase.GUIDToAssetPath(re)));
                }
            }

            return list;
        }

        public static List<AnimationClip> GetAnimClipsFromFBX()
        {
            var list = new List<AnimationClip>();
            var selections = Selection.objects;
            foreach (var s in selections)
            {
                var path = AssetDatabase.GetAssetPath(s);
                var representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                foreach (var asset in representations)
                {
                    var clip = asset as AnimationClip;
                    if (clip != null)
                        list.Add(clip);
                }
            }

            return list;
        }

        public static ModelImporterClipAnimation[] GetAnimationsFromModel(string pPath)
        {
            var mi = AssetImporter.GetAtPath(pPath) as ModelImporter;
            if (mi != null)
                return mi.defaultClipAnimations;
            return null;
        }

        public static AnimationClip GetAnimationFromModel(string pPath, string pName)
        {
            //var anims = GetAnimationsFromModel(pPath);
            //if (anims != null)
            //    foreach (var anim in anims)
            //        if (anim.name == pName)
            //            return anim;
            //return null;

            var representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(pPath);
            foreach (var asset in representations)
            {
                var clip = asset as AnimationClip;
                if (clip != null && clip.name == pName)
                    return clip;
            }

            return null;
        }

        private static string LastOpenedDirectory
        {
	        get => EditorPrefs.GetString("LastOpenedDirectory");
	        set => EditorPrefs.SetString("LastOpenedDirectory", value);
        }
        
        public static List<string> OpenFilePanelWithFilters(string title, string[] filter)
        {
	        string path = EditorUtility.OpenFilePanelWithFilters(title, LastOpenedDirectory, filter);
	        var paths = new List<string>();
	        if (!string.IsNullOrEmpty(path))
	        {
		        paths.AddRange(path.Split(';'));
		        LastOpenedDirectory = Path.GetDirectoryName(paths[0]);
	        }
			return paths;
        }
        
        public static string OpenFilePanel(string title, string extension, string directory = null)
        {
	        string path = EditorUtility.OpenFilePanel(title, directory ?? LastOpenedDirectory, extension);
	        if (!string.IsNullOrEmpty(path))
		        LastOpenedDirectory = Path.GetDirectoryName(path);
	        return path;
        } 
        
        public static void ExportSelectedFoldersToUnityPackage()
        {
	        var objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
	        if (objects.Length == 0)
		        return;

	        var folders = new List<string>();
	        for (int i = 0; i < objects.Length; i++)
	        {
		        var obj = objects[i];
		        bool isFolder = AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj));
		        if (isFolder)
			        folders.Add(AssetDatabase.GetAssetPath(obj));
	        }
	        if (folders.Count > 0)
	        {
		        string directoryPath = AssetDatabase.GetAssetPath(objects[0]);
		        string packagePath = EditorUtility.SaveFilePanel("Export Unity Package", directoryPath, objects[0].name + ".unitypackage", "unitypackage");
		        AssetDatabase.ExportPackage(folders.ToArray(), packagePath, ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
	        }
        }
        
        public static void RefreshAssetsInSelectedFolder(string filter)
        {
	        var objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
	        if (objects.Length == 0)
		        return;
	        for (int i = 0; i < objects.Length; i++)
	        {
		        var obj = objects[i];
		        bool isFolder = AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj));
		        if (isFolder)
		        {
			        string directoryPath = AssetDatabase.GetAssetPath(obj);
			        RefreshAssets(filter, directoryPath);
		        }
	        }
        }
        
        public static void RefreshAssets(string filter, string folderPath = null)
        {
	        if (string.IsNullOrEmpty(folderPath))
	        {
		        folderPath = OpenFolderPanel();
		        if (string.IsNullOrEmpty(folderPath))
			        return;
		        folderPath = FormatPathToUnityPath(folderPath);
	        }
	        var assetGUIDs = AssetDatabase.FindAssets(filter, new[] { folderPath });
	        foreach (string guid in assetGUIDs)
	        {
		        var path = AssetDatabase.GUIDToAssetPath(guid);
		        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
		        if (asset != null)
			        EditorUtility.SetDirty(asset);
	        }
	        AssetDatabase.SaveAssets();
        }
        
#endregion

	    //========================================

#region Misc

        private static Dictionary<string, HashSet<string>> m_InverseReferenceMap;
        private static int m_ReferencesCount;

        public static void BuildReferenceMapCache<T>(string[] assetGUIDs, List<T> cachedObjects) where T : Object
        {
            if (assetGUIDs == null)
            {
                const string searchFilter = "t:Object";
                string[] searchDirectories = { "Assets" };
                assetGUIDs = AssetDatabase.FindAssets(searchFilter, searchDirectories);
            }

            m_InverseReferenceMap = new Dictionary<string, HashSet<string>>();
            m_ReferencesCount = 0;

            // Initialize map to store all paths that have a reference to our selectedGuids
            foreach (var selectedObj in cachedObjects)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selectedObj);
                string selectedGuid = AssetDatabase.AssetPathToGUID(selectedPath);
                m_InverseReferenceMap[selectedGuid] = new HashSet<string>();
            }

            // Scan all assets and store the inverse reference if contains a reference to any selectedGuid...
            var scanProgress = 0;
            foreach (var guid in assetGUIDs)
            {
                scanProgress++;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsDirectory(path))
                    continue;

                var dependencies = AssetDatabase.GetDependencies(path);
                foreach (var dependency in dependencies)
                {
                    EditorUtility.DisplayProgressBar("Scanning guid references on:", path, (float)scanProgress / assetGUIDs.Length);

                    var dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
                    if (m_InverseReferenceMap.ContainsKey(dependencyGuid))
                    {
                        m_InverseReferenceMap[dependencyGuid].Add(path);

                        // Also include .meta path. This fixes broken references when an FBX uses external materials
                        // var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
                        // inverseReferenceMap[dependencyGUID].Add(metaPath);

                        m_ReferencesCount++;
                    }
                }
            }
        }

        public static Dictionary<string, int> SearchAndReplaceGuid<T>(List<T> oldObjects, T newObject, string[] assetGUIDs) where T : Object
        {
            if (assetGUIDs == null)
            {
                const string searchFilter = "t:Object";
                string[] searchDirectories = { "Assets" };
                assetGUIDs = AssetDatabase.FindAssets(searchFilter, searchDirectories);
            }
            var updatedAssets = new Dictionary<string, int>();

            if (oldObjects.Count == 0)
                return updatedAssets;

            var inverseReferenceMap = new Dictionary<string, HashSet<string>>();
            int referencesCount = 0;
            if (m_InverseReferenceMap == null)
            {
                // Initialize map to store all paths that have a reference to our selectedGuids
                foreach (var selectedObj in oldObjects)
                {
                    if (selectedObj == null)
                        continue;
                    string selectedPath = AssetDatabase.GetAssetPath(selectedObj);
                    string selectedGuid = AssetDatabase.AssetPathToGUID(selectedPath);
                    inverseReferenceMap[selectedGuid] = new HashSet<string>();
                }

                // Scan all assets and store the inverse reference if contains a reference to any selectedGuid...
                var scanProgress = 0;
                foreach (var guid in assetGUIDs)
                {
                    scanProgress++;
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (IsDirectory(path))
                        continue;

                    var dependencies = AssetDatabase.GetDependencies(path);
                    foreach (var dependency in dependencies)
                    {
                        EditorUtility.DisplayProgressBar("Scanning guid references on:", path, (float)scanProgress / assetGUIDs.Length);

                        var dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
                        if (inverseReferenceMap.ContainsKey(dependencyGuid))
                        {
                            inverseReferenceMap[dependencyGuid].Add(path);

                            // Also include .meta path. This fixes broken references when an FBX uses external materials
                            // var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
                            // inverseReferenceMap[dependencyGUID].Add(metaPath);

                            referencesCount++;
                        }
                    }
                }
            }
            else
            {
                inverseReferenceMap = m_InverseReferenceMap;
                referencesCount = m_ReferencesCount;
            }

            string newPath = AssetDatabase.GetAssetPath(newObject);
            string newGuid = AssetDatabase.AssetPathToGUID(newPath);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newObject, out string assetId, out long newFileId);
            var countProgress = 0;
            int countReplaced = 0;
            foreach (var selectedObj in oldObjects)
            {
                if (selectedObj == null)
                    continue;
                bool found = false;
                string selectedPath = AssetDatabase.GetAssetPath(selectedObj);
                string selectedGuid = AssetDatabase.AssetPathToGUID(selectedPath);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(selectedObj, out assetId, out long selectedFileId);
                var referencePaths = inverseReferenceMap[selectedGuid];
                foreach (var referencePath in referencePaths)
                {
                    if (referencePath == selectedPath)
                        continue;

                    countProgress++;

                    EditorUtility.DisplayProgressBar($"Replacing GUID: {selectedPath}", referencePath, (float)countProgress / referencesCount);

                    if (IsDirectory(referencePath))
                        continue;

                    var contents = File.ReadAllText(referencePath);

                    if (contents.Contains($"fileID: {selectedFileId}, guid: {selectedGuid}"))
                    {
                        contents = contents.Replace($"fileID: {selectedFileId}, guid: {selectedGuid}", $"fileID: {newFileId}, guid: {newGuid}");
                        File.WriteAllText(referencePath, contents);
                        countReplaced++;
                        found = true;
                    }
                }

                UnityEngine.Debug.Log($"Replace GUID in: {selectedPath}");
                updatedAssets.Add(selectedPath, countReplaced);

                if (found)
                    EditorUtility.SetDirty(selectedObj);
            }
            return updatedAssets;
        }

        public static string[] ReadMetaFile(Object pObject)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string path = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObject)}";
            string metaPath = $"{path}.meta";
            string[] lines = File.ReadAllLines(metaPath);
            return lines;
        }

        public static string ReadContentMetaFile(Object pObject)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string path = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObject)}";
            string metaPath = $"{path}.meta";
            string content = File.ReadAllText(metaPath);
            return content;
        }

        public static void WriteMetaFile(Object pObject, string[] pLines, bool pRefreshDatabase)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string path = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObject)}";
            string metaPath = $"{path}.meta";
            File.WriteAllLines(metaPath, pLines);
            if (pRefreshDatabase)
                AssetDatabase.Refresh();
        }
        
        public struct SpriteInfo
        {
            public string name;
            public Vector2 pivot;
            public Vector4 border;
            public int alignment;
        }

        public static Dictionary<string, SpriteInfo> GetPivotsOfSprites(Sprite pSpriteFrom)
        {
            var results = new Dictionary<string, SpriteInfo>();
            var lines = ReadMetaFile(pSpriteFrom);
            var nameLines = lines.Where(line => line.Trim().StartsWith("name:", StringComparison.OrdinalIgnoreCase)).ToList();
            if (nameLines.Count > 0) //SpriteFrom is inside a atlas
            {
                //Get names of all sprites inside atlas which contain spriteFrom
                var names = new List<string>();
                foreach (var line in nameLines)
                {
                    string name = line.Replace("name: ", "").Trim();
                    names.Add(name);
                }

                var alignmentLines = lines.Where(line => line.Trim().StartsWith("alignment:", StringComparison.OrdinalIgnoreCase)).ToList();
                var alignments = new List<int>();
                for (int i = 0; i < alignmentLines.Count; i++)
                {
                    if (i == 0)
                        continue;
                    string line = alignmentLines[i];
                    var alignmentStr = line.Replace("alignment: ", "").Trim();
                    alignments.Add(int.Parse(alignmentStr));
                }

                //Get pivots of all sprites inside atlas which contain spriteFrom
                var pivotLines = lines.Where(line => line.Trim().StartsWith("pivot:", StringComparison.OrdinalIgnoreCase)).ToList();
                var pivots = new List<Vector2>();
                foreach (var line in pivotLines)
                {
	                var pivotStr = line.Replace("pivot: ", "").Trim();
	                var pivot = JsonUtility.FromJson<RVector2>(pivotStr);
	                pivots.Add(new Vector2(pivot.x, pivot.y));
                }

                var borders = new List<Vector4>();
                var borderLines = lines.Where(line => line.Trim().StartsWith("border:", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var line in borderLines)
                {
	                var borderStr = line.Replace("border: ", "").Trim();
	                var border = JsonUtility.FromJson<RVector4>(borderStr);
	                borders.Add(new Vector4(border.x, border.y, border.z, border.w));
                }
                for (int i = 0; i < names.Count; i++)
                    results.Add(names[i], new SpriteInfo
                    {
                        name = names[i],
                        pivot = pivots[i],
                        border = borders[i],
                        alignment = alignments[i],
                    });
            }
            else
            {
                var alignmentLine = lines.First(line => line.Trim().StartsWith("alignment: ", StringComparison.OrdinalIgnoreCase));
                var alignmentStr = alignmentLine.Replace("alignment: ", "").Trim();
                var alignment = int.Parse(alignmentStr);

                var pivotLine = lines.First(line => line.Trim().StartsWith("spritePivot: ", StringComparison.OrdinalIgnoreCase));
                var pivotStr = pivotLine.Replace("spritePivot: ", "").Trim();
                var pivot = JsonUtility.FromJson<RVector2>(pivotStr);

                var borderStr = lines.First(line => line.Trim().StartsWith("spriteBorder: ", StringComparison.OrdinalIgnoreCase));
                var border = JsonUtility.FromJson<RVector4>(borderStr);

                results.Add(pSpriteFrom.name, new SpriteInfo
                {
	                name = pSpriteFrom.name,
	                pivot = new Vector2(pivot.x, pivot.y),
	                border = new Vector4(border.x, border.y, border.z, border.w),
	                alignment = alignment,
                });
            }
            return results;
        }

        public static void SetTextureReadable(Texture2D p_texture2D, bool p_readable)
        {
            var lines = ReadMetaFile(p_texture2D);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("isReadable:", StringComparison.OrdinalIgnoreCase))
                {
                    int spaceIndex = lines[i].IndexOf("isReadable:", StringComparison.OrdinalIgnoreCase);
                    string readable = p_readable ? "1" : "0";
                    lines[i] = $"isReadable: {readable}"; //Replace pivot
                    for (int s = 0; s < spaceIndex; s++)
                        lines[i] = lines[i].Insert(0, " ");
                    break;
                }
            }
            WriteMetaFile(p_texture2D, lines, true);
        }

        public static void CopyPivotAndBorder(Sprite pOriginal, Sprite pTarget, bool pRefreshDatabase)
        {
            var spriteInfo = GetPivotsOfSprites(pOriginal);
            var pivotForm = spriteInfo[pOriginal.name].pivot;
            var borderFrom = spriteInfo[pOriginal.name].border;
            var alignmentFrom = spriteInfo[pOriginal.name].alignment;

            var lines = ReadMetaFile(pTarget);
            var nameLines = lines.Where(line => line.Trim().StartsWith("name:", StringComparison.OrdinalIgnoreCase)).ToList();
            if (nameLines.Count > 0) //SpriteTo is inside a atlas
            {
                int nameIndex = 0;
                bool foundName = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    bool found = false;
                    int spaceIndex = 0;
                    if (lines[i].Trim().StartsWith("name:", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = lines[i].Replace("name:", "").Trim();
                        if (name == pTarget.name)
                        {
                            nameIndex = i;
                            foundName = true;
                        }
                    }
                    if (foundName && i > nameIndex && lines[i].Trim().StartsWith("alignment:", StringComparison.OrdinalIgnoreCase))
                    {
                        spaceIndex = lines[i].IndexOf("alignment:", StringComparison.OrdinalIgnoreCase);
                        lines[i] = $"alignment: {alignmentFrom}"; //Replace pivot
                    }
                    else if (foundName && i > nameIndex && lines[i].Trim().StartsWith("pivot:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (alignmentFrom == 0 && pivotForm == Vector2.zero)
                            continue;
                        spaceIndex = lines[i].IndexOf("pivot:", StringComparison.OrdinalIgnoreCase);
                        lines[i] = $"pivot: {{x: {pivotForm.x}, y: {pivotForm.y}}}"; //Replace pivot
                    }
                    else if (foundName && i > nameIndex && lines[i].Trim().StartsWith("border:", StringComparison.OrdinalIgnoreCase))
                    {
                        spaceIndex = lines[i].IndexOf("border:", StringComparison.OrdinalIgnoreCase);
                        lines[i] = $"border: {{x: {borderFrom.x}, y: {borderFrom.y}, z: {borderFrom.z}, w: {borderFrom.w}}}"; //Replace border
                        found = true;
                    }
                    if (spaceIndex > 0)
                    {
                        for (int s = 0; s < spaceIndex; s++)
                            lines[i] = lines[i].Insert(0, " ");
                    }
                    if (found)
                        break;
                }
            }
            else
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    bool found = false;
                    int spaceIndex = 0;
                    if (lines[i].Trim().StartsWith("alignment:", StringComparison.OrdinalIgnoreCase))
                    {
                        spaceIndex = lines[i].IndexOf("alignment:", StringComparison.OrdinalIgnoreCase);
                        lines[i] = $"alignment: {alignmentFrom}"; //Replace pivot
                    }
                    else if (lines[i].Trim().StartsWith("spritePivot:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (alignmentFrom == 0 && pivotForm == Vector2.zero)
                            continue;
                        spaceIndex = lines[i].IndexOf("spritePivot:", StringComparison.OrdinalIgnoreCase);
                        lines[i] = $"spritePivot: {{x: {pivotForm.x}, y: {pivotForm.y}}}"; //Replace pivot
                    }
                    else if (lines[i].Trim().StartsWith("spriteBorder:", StringComparison.OrdinalIgnoreCase))
                    {
                        spaceIndex = lines[i].IndexOf("spriteBorder:", StringComparison.OrdinalIgnoreCase);
                        lines[i] = $"spriteBorder: {{x: {borderFrom.x}, y: {borderFrom.y}, z: {borderFrom.z}, y: {borderFrom.w}}}"; //Replace border
                        found = true;
                    }
                    if (spaceIndex > 0)
                        for (int s = 0; s < spaceIndex; s++)
                            lines[i] = lines[i].Insert(0, " ");
                    if (found)
                        break;
                }
            }

            WriteMetaFile(pTarget, lines, pRefreshDatabase);
        }

        public static void ExportSpritesFromTexture(Object pObj,
            string pExportDirectory = null,
            string pNamePattern = null,
            bool pRenameOriginal = false)
        {
            var results = new List<Sprite>();
            string path = AssetDatabase.GetAssetPath(pObj);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length > 0)
            {
                if (string.IsNullOrEmpty(pExportDirectory))
                    pExportDirectory = Path.GetDirectoryName(path);
                var texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!texture2D.isReadable)
                    SetTextureReadable(texture2D, true);
                foreach (var sprite in sprites)
                {
                    int x = (int)sprite.rect.x;
                    int y = (int)sprite.rect.y;
                    int width = (int)sprite.rect.width;
                    int height = (int)sprite.rect.height;
                    var newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    var pixels = sprite.texture.GetPixels(x, y, width, height);
                    newTexture.SetPixels(pixels);
                    newTexture.Apply();
                    byte[] newTextureData = newTexture.EncodeToPNG();

                    string customName = sprite.name;
                    if (!string.IsNullOrEmpty(pNamePattern))
                    {
                        var match = Regex.Match(customName, @"\d+$");
                        if (match.Success)
                        {
                            int number = int.Parse(match.Value);
                            string numberStr = number.ToString();
                            if (sprites.Length > 100)
                                numberStr = number.ToString("D3");
                            else if (sprites.Length > 10)
                                numberStr = number.ToString("D2");
                            customName = pNamePattern + numberStr;
                        }
                    }
                    string newSpritePath = Path.Combine(pExportDirectory, $"{customName}.png");
                    File.WriteAllBytes(newSpritePath, newTextureData);
                    Object.DestroyImmediate(newTexture);
                }
                AssetDatabase.Refresh();
                string[] metaFileContent = null;
                if (pRenameOriginal)
                    metaFileContent = ReadMetaFile(pObj);

                foreach (var sprite in sprites)
                {
                    string customName = sprite.name;
                    if (!string.IsNullOrEmpty(pNamePattern))
                    {
                        var match = Regex.Match(customName, @"\d+$");
                        if (match.Success)
                        {
                            int number = int.Parse(match.Value);
                            string numberStr = number.ToString();
                            if (sprites.Length > 100)
                                numberStr = number.ToString("D3");
                            else if (sprites.Length > 10)
                                numberStr = number.ToString("D2");
                            customName = pNamePattern + numberStr;
                        }
                    }
                    string newSpritePath = Path.Combine(pExportDirectory, $"{customName}.png");
                    var newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(newSpritePath);
                    CopyPivotAndBorder(sprite, newSprite, false);
                    results.Add(newSprite);

                    if (pRenameOriginal)
                    {
                        for (int line = 0; line < metaFileContent.Length; line++)
                        {
                            string pattern = $@"\b{sprite.name}\b";
                            metaFileContent[line] = Regex.Replace(metaFileContent[line], pattern, customName);
                        }
                    }
                }
                if (pRenameOriginal)
                {
                    string projectPath = Application.dataPath.Replace("/Assets", "");
                    string metaPath = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObj)}.meta";
                    File.WriteAllLines(metaPath, metaFileContent);
                }
                AssetDatabase.Refresh();
            }
        }

        private static bool IsDirectory(string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

        public static Dictionary<GameObject, List<T>> FindComponents<T>(GameObject[] objs, ConditionalDelegate<T> pValidCondition) where T : Component
        {
            var allComponents = new Dictionary<GameObject, List<T>>();
            for (int i = 0; i < objs.Length; i++)
            {
                var components = objs[i].gameObject.GetComponentsInChildren<T>(true);
                if (components.Length > 0)
                {
                    allComponents.Add(objs[i], new List<T>());
                    foreach (var component in components)
                    {
                        if (pValidCondition != null && !pValidCondition(component))
                            continue;

                        if (!allComponents[objs[i]].Contains(component))
                            allComponents[objs[i]].Add(component);
                    }
                }
            }

            return allComponents;
        }

        public static void ReplaceTextsByTextTMP(params GameObject[] gos)
        {
            var textsDict = FindComponents<Text>(gos, null);
            if (textsDict != null)
                foreach (var item in textsDict)
                {
                    for (int i = item.Value.Count - 1; i >= 0; i--)
                    {
                        var go = item.Value[i].gameObject;
                        var content = item.Value[i].text;
                        var fontSize = item.Value[i].fontSize;
                        var alignment = item.Value[i].alignment;
                        var bestFit = item.Value[i].resizeTextForBestFit;
                        var horizontalOverflow = item.Value[i].horizontalOverflow;
                        var verticalOverflow = item.Value[i].verticalOverflow;
                        var raycastTarget = item.Value[i].raycastTarget;
                        var color = item.Value[i].color;
                        if (item.Value[i].gameObject.TryGetComponent(out Outline outline))
                            Object.DestroyImmediate(outline);
                        if (item.Value[i].gameObject.TryGetComponent(out Shadow shadow))
                            Object.DestroyImmediate(shadow);
                        Object.DestroyImmediate(item.Value[i]);
                        var textTMP = go.AddComponent<TextMeshProUGUI>();
                        textTMP.text = content;
                        textTMP.fontSize = fontSize;
                        textTMP.enableAutoSizing = bestFit;
                        textTMP.color = color;
                        textTMP.raycastTarget = raycastTarget;
                        switch (alignment)
                        {
                            case TextAnchor.MiddleLeft:
                                textTMP.alignment = TextAlignmentOptions.Left;
                                break;
                            case TextAnchor.MiddleCenter:
                                textTMP.alignment = TextAlignmentOptions.Center;
                                break;
                            case TextAnchor.MiddleRight:
                                textTMP.alignment = TextAlignmentOptions.Right;
                                break;

                            case TextAnchor.LowerLeft:
                                textTMP.alignment = TextAlignmentOptions.BottomLeft;
                                break;
                            case TextAnchor.LowerCenter:
                                textTMP.alignment = TextAlignmentOptions.Bottom;
                                break;
                            case TextAnchor.LowerRight:
                                textTMP.alignment = TextAlignmentOptions.BottomRight;
                                break;

                            case TextAnchor.UpperLeft:
                                textTMP.alignment = TextAlignmentOptions.TopLeft;
                                break;
                            case TextAnchor.UpperCenter:
                                textTMP.alignment = TextAlignmentOptions.Top;
                                break;
                            case TextAnchor.UpperRight:
                                textTMP.alignment = TextAlignmentOptions.TopRight;
                                break;
                        }
                        textTMP.enableWordWrapping = horizontalOverflow == HorizontalWrapMode.Wrap;
                        if (verticalOverflow == VerticalWrapMode.Truncate)
                            textTMP.overflowMode = TextOverflowModes.Truncate;
                        UnityEngine.Debug.Log($"Replace Text in GameObject {go.name}");
                        EditorUtility.SetDirty(go);
                    }
                }
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
        
#endregion
    }

    //========================================
    // Custom GUIStyle
    //========================================

    public static class GUIStyleHelper
    {
        public static readonly GUIStyle headerTitle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            fixedHeight = 30,
        };
    }
}
#endif