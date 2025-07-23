#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
    public static class EditorFileUtil
    {
	    private static string LastOpenedDirectory { get => EditorPrefs.GetString("LastOpenedDirectory"); set => EditorPrefs.SetString("LastOpenedDirectory", value); }
	    
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
	    
	    public static string OpenFilePanel(string title, string extension, string directory = null)
	    {
		    string path = EditorUtility.OpenFilePanel(title, directory ?? LastOpenedDirectory, extension);
		    if (!string.IsNullOrEmpty(path))
			    LastOpenedDirectory = Path.GetDirectoryName(path);
		    return path;
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
	    
	    public static bool LoadJsonFilePanel<T>(string pMainDirectory, ref T pOutput)
	    {
		    if (string.IsNullOrEmpty(pMainDirectory))
			    pMainDirectory = Application.dataPath;

		    string path = EditorUtility.OpenFilePanel("Open File", pMainDirectory, "json,txt");
		    if (string.IsNullOrEmpty(path))
			    return false;

		    return LoadJsonFromFile(path, ref pOutput);
	    }
	    
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

        public static bool LoadJsonFromFile<T>(string path, ref T output)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                output = JsonUtility.FromJson<T>(File.ReadAllText(path));
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
    }
}
#endif