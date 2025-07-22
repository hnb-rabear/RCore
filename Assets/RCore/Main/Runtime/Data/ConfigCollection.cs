using System.Collections.Generic;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Data
{
	/// <summary>
	/// An abstract base class for creating ScriptableObject-based data containers that are populated
	/// from external text files (e.g., JSON, CSV). It provides a standardized way to load data
	/// from either a 'Resources' folder at runtime or directly from the asset path in the Unity Editor.
	/// </summary>
	public abstract class ConfigCollection : ScriptableObject
	{
		/// <summary>
		/// If true, data will be loaded from a 'Resources' folder. This is required for runtime loading in a build.
		/// If false, data is loaded directly from the asset path, which only works in the Unity Editor.
		/// </summary>
		[Tooltip("If true, loads from a 'Resources' folder. If false, loads directly from the specified asset path (Editor-only).")]
		[SerializeField] private bool m_loadFromResources;
		[SerializeField] private string m_loadFromFolder;

		/// <summary>
		/// Loads the text content of a specified file.
		/// The path is constructed based on the 'loadFromResources' flag and the 'm_loadFromFolder' path.
		/// </summary>
		/// <param name="pFile">The name of the file to load (without extension if loading from Resources, with .txt extension if loading from AssetDatabase).</param>
		/// <returns>The string content of the file, or null if the file is not found.</returns>
		protected string LoadConfig(string pFile)
		{
			TextAsset textAsset = null;
			if (m_loadFromResources)
			{
				// At runtime, files must be in a "Resources" folder.
				// The path is relative to any "Resources" folder in the project.
				string path = $"{m_loadFromFolder}/{pFile}";
				textAsset = Resources.Load<TextAsset>(path);
			}
			else
			{
#if UNITY_EDITOR
				// In the editor, we can load directly from the asset path.
				// This assumes the file has a .txt extension to be recognized as a TextAsset.
				string path = $"{m_loadFromFolder}/{pFile}.txt";
				textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
#endif
			}
			return textAsset != null ? textAsset.text : null;

		}

		/// <summary>
		/// This abstract method must be implemented by any inheriting class.
		/// It should contain the logic to call LoadConfig, LoadToArray, or LoadToList
		/// and populate the data fields of the ScriptableObject.
		/// </summary>
		public abstract void LoadData();

		/// <summary>
		/// A helper method that loads a file and deserializes its JSON content into an array of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the objects in the JSON array.</typeparam>
		/// <param name="pFile">The name of the file to load.</param>
		/// <returns>An array of objects of type T.</returns>
		public T[] LoadToArray<T>(string pFile) => JsonHelper.ToArray<T>(LoadConfig(pFile));

		/// <summary>
		/// A helper method that loads a file and deserializes its JSON content into a List of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the objects in the JSON array.</typeparam>
		/// <param name="pFile">The name of the file to load.</param>
		/// <returns>A List of objects of type T.</returns>
		public List<T> LoadToList<T>(string pFile) => JsonHelper.ToList<T>(LoadConfig(pFile));

#if UNITY_EDITOR
		/// <summary>
		/// A custom editor for any class that inherits from ConfigCollection.
		/// It provides a consistent UI in the Inspector for setting the load path and triggering the data loading process.
		/// </summary>
		[CustomEditor(typeof(ConfigCollection), true, isFallback = true)]
#if ODIN_INSPECTOR
		public class ConfigCollectionEditor : Sirenix.OdinInspector.Editor.OdinEditor
		{
			private ConfigCollection m_config;

			protected override void OnEnable()
			{
				base.OnEnable();
				m_config = target as ConfigCollection;
			}
#else
		public class ConfigCollectionEditor : UnityEditor.Editor
		{
			private ConfigCollection m_config;

			protected virtual void OnEnable()
			{
				m_config = target as ConfigCollection;
			}
#endif

			/// <summary>
			/// Draws the custom inspector GUI.
			/// </summary>
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				GUILayout.Space(10);
				
				// Draw a convenient folder selection field for the load path.
				m_config.m_loadFromFolder = EditorHelper.FolderField(m_config.m_loadFromFolder, "Load From Folder", 120);
			
				// Add a button to manually trigger the data loading process from the Inspector.
				if (GUILayout.Button("Load Data"))
				{
					m_config.LoadData();
					// Mark the asset as dirty so the changes are saved.
					EditorUtility.SetDirty(m_config);
					AssetDatabase.SaveAssetIfDirty(m_config);
					Debug.Log($"Data loaded for {m_config.name}");
				}
			}
		}
#endif
	}
}