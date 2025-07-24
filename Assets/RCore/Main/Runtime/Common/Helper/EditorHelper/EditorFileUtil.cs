#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
    /// <summary>
    /// Provides a collection of static utility methods for file and folder operations within the Unity Editor.
    /// This includes showing file/folder selection dialogs, reading from and writing to files, and path manipulation.
    /// </summary>
    public static class EditorFileUtil
    {
	    /// <summary>
	    /// Stores the last used directory path in EditorPrefs to provide a better user experience for file dialogs.
	    /// </summary>
	    private static string LastOpenedDirectory { get => EditorPrefs.GetString("LastOpenedDirectory"); set => EditorPrefs.SetString("LastOpenedDirectory", value); }
	    
	    /// <summary>
	    /// Opens the native folder selection dialog.
	    /// </summary>
	    /// <param name="pFolderPath">The initial directory to open the dialog in. If null, uses the last opened directory or the project's Assets folder.</param>
	    /// <returns>The absolute path of the selected folder, or an empty string if the user cancels.</returns>
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
	    
	    /// <summary>
	    /// Opens the native file selection dialog.
	    /// </summary>
	    /// <param name="title">The title of the dialog window.</param>
	    /// <param name="extension">The file extension to filter for (e.g., "png").</param>
	    /// <param name="directory">The initial directory to open. If null, uses the last opened directory.</param>
	    /// <returns>The absolute path of the selected file, or an empty string if the user cancels.</returns>
	    public static string OpenFilePanel(string title, string extension, string directory = null)
	    {
		    string path = EditorUtility.OpenFilePanel(title, directory ?? LastOpenedDirectory, extension);
		    if (!string.IsNullOrEmpty(path))
			    LastOpenedDirectory = Path.GetDirectoryName(path);
		    return path;
	    }
	    
	    /// <summary>
	    /// Opens the native file selection dialog with multiple extension filters.
	    /// </summary>
	    /// <param name="title">The title of the dialog window.</param>
	    /// <param name="filter">An array of filters, formatted as ["Filter Name", "ext1,ext2,...", ...].</param>
	    /// <returns>A list containing the absolute path(s) of the selected file(s).</returns>
	    public static List<string> OpenFilePanelWithFilters(string title, string[] filter)
	    {
		    string path = EditorUtility.OpenFilePanelWithFilters(title, LastOpenedDirectory, filter);
		    var paths = new List<string>();
		    if (!string.IsNullOrEmpty(path))
		    {
			    // Note: OpenFilePanelWithFilters on some platforms might return a single string for one file,
			    // not multiple paths. This splitting logic may need adjustment based on target OS behavior.
			    paths.AddRange(path.Split(';'));
			    LastOpenedDirectory = Path.GetDirectoryName(paths[0]);
		    }
		    return paths;
	    }
	    
	    /// <summary>
	    /// Opens a file selection dialog and reads the entire content of the selected file into a string.
	    /// </summary>
	    /// <param name="pMainDirectory">The initial directory to open.</param>
	    /// <param name="extensions">A comma-separated list of allowed file extensions (e.g., "json,txt").</param>
	    /// <returns>The text content of the file, or null if the user cancels.</returns>
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
	    
	    /// <summary>
	    /// Opens a file selection dialog and returns both the path and the content of the selected file.
	    /// </summary>
	    /// <param name="pMainDirectory">The initial directory to open.</param>
	    /// <param name="extensions">A comma-separated list of allowed file extensions.</param>
	    /// <returns>A KeyValuePair where the key is the file path and the value is its content. Returns an empty pair on cancellation.</returns>
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
	    
	    /// <summary>
	    /// Opens a file selection dialog, reads the selected JSON file, and deserializes it into a given object.
	    /// </summary>
	    /// <typeparam name="T">The type to deserialize the JSON into.</typeparam>
	    /// <param name="pMainDirectory">The initial directory to open.</param>
	    /// <param name="pOutput">The object to be populated with the deserialized data.</param>
	    /// <returns>True if the file was loaded and deserialized successfully, otherwise false.</returns>
	    public static bool LoadJsonFilePanel<T>(string pMainDirectory, ref T pOutput)
	    {
		    if (string.IsNullOrEmpty(pMainDirectory))
			    pMainDirectory = Application.dataPath;

		    string path = EditorUtility.OpenFilePanel("Open File", pMainDirectory, "json,txt");
		    if (string.IsNullOrEmpty(path))
			    return false;

		    return LoadJsonFromFile(path, ref pOutput);
	    }
	    
	    /// <summary>
	    /// Opens the native "Save File" dialog and writes the provided content to the selected file path.
	    /// </summary>
	    /// <param name="mainDirectory">The initial directory for the dialog.</param>
	    /// <param name="defaultName">The default file name.</param>
	    /// <param name="content">The string content to write to the file.</param>
	    /// <param name="extension">The file extension for the dialog filter.</param>
	    /// <returns>The full path where the file was saved, or an empty string if the user cancels.</returns>
	    public static string SaveFilePanel(string mainDirectory, string defaultName, string content, string extension = "json,txt")
	    {
		    if (string.IsNullOrEmpty(mainDirectory))
			    mainDirectory = Application.dataPath;

		    string path = EditorUtility.SaveFilePanel("Save File", mainDirectory, defaultName, extension);
		    if (!string.IsNullOrEmpty(path))
			    SaveFile(path, content);
		    return path;
	    }

        /// <summary>
        /// Writes string content to a specified file path, overwriting the file if it exists.
        /// Does not write if the content is null, empty, or just "{}".
        /// </summary>
        /// <param name="path">The full file path.</param>
        /// <param name="content">The string content to write.</param>
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
        /// Serializes an object to JSON and opens a "Save File" dialog to save it.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="pMainDirectory">The initial directory for the dialog.</param>
        /// <param name="defaultName">The default file name.</param>
        /// <param name="obj">The object to serialize and save.</param>
        public static void SaveJsonFilePanel<T>(string pMainDirectory, string defaultName, T obj)
        {
	        if (string.IsNullOrEmpty(pMainDirectory))
		        pMainDirectory = Application.dataPath;

	        string path = EditorUtility.SaveFilePanel("Save File", pMainDirectory, defaultName, "json,txt");
	        if (!string.IsNullOrEmpty(path))
		        SaveJsonFile(path, obj);
        }

        /// <summary>
        /// Serializes an object to JSON and saves it to the specified file path.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="pPath">The full path of the file to save.</param>
        /// <param name="pObj">The object to serialize.</param>
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
        /// Reads a JSON file from a specified path and deserializes it into an object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON into.</typeparam>
        /// <param name="path">The full path of the JSON file.</param>
        /// <param name="output">The object to be populated with the deserialized data.</param>
        /// <returns>True if the file was loaded and deserialized successfully, otherwise false.</returns>
        public static bool LoadJsonFromFile<T>(string path, ref T output)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                output = JsonUtility.FromJson<T>(File.ReadAllText(path));
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Serializes an object to XML and saves it to the specified file path.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="pPath">The full path of the file to save.</param>
        /// <param name="pObj">The object to serialize.</param>
        public static void SaveXMLFile<T>(string pPath, T pObj)
        {
	        if (File.Exists(pPath))
		        File.Delete(pPath);
	        var serializer = new XmlSerializer(typeof(T));
	        using TextWriter writer = new StreamWriter(pPath);
	        serializer.Serialize(writer, pObj);
        }

        /// <summary>
        /// Reads an XML file from a specified path and deserializes it into an object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the XML into.</typeparam>
        /// <param name="pPath">The full path of the XML file.</param>
        /// <returns>The deserialized object.</returns>
        public static T LoadXMLFile<T>(string pPath)
        {
	        var serializer = new XmlSerializer(typeof(T));
	        using TextReader reader = new StreamReader(pPath);
	        var pObj = (T)serializer.Deserialize(reader);
	        return pObj;
        }
        
        /// <summary>
        /// Formats a full system path to a Unity-relative path (starting with "Assets/").
        /// </summary>
        /// <param name="path">The full path to format.</param>
        /// <returns>A Unity-relative path.</returns>
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
        
        /// <summary>
        /// Gets all subdirectories of a given path, formatted as Unity-relative paths.
        /// If the path has no subdirectories, it returns the path itself.
        /// </summary>
        /// <param name="path">The path to search for directories.</param>
        /// <returns>An array of Unity-relative directory paths.</returns>
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