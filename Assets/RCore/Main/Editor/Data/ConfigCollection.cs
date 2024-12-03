using RCore.Inspector;
using System.Collections.Generic;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Data
{
	public abstract class ConfigCollection : ScriptableObject
	{
		private const bool LOAD_FROM_RESOURCES = false;

		[SerializeField] private string m_loadFromFolder;

		protected string LoadConfig(string pFile)
		{
			TextAsset textAsset = null;
			if (LOAD_FROM_RESOURCES)
			{
				string path = $"{m_loadFromFolder}/{pFile}";
				textAsset = (TextAsset)Resources.Load<TextAsset>(path);
			}
			else
			{
#if UNITY_EDITOR
				string path = $"{m_loadFromFolder}/{pFile}.txt";
				textAsset = (TextAsset)AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));
#endif
			}
			return textAsset != null ? textAsset.text : null;

		}

		public abstract void LoadData();

		public T[] LoadToArray<T>(string pFile) => JsonHelper.ToArray<T>(LoadConfig(pFile));

		public List<T> LoadToList<T>(string pFile) => JsonHelper.ToList<T>(LoadConfig(pFile));

#if UNITY_EDITOR
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

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				GUILayout.Space(10);
				
				m_config.m_loadFromFolder = EditorHelper.FolderField(m_config.m_loadFromFolder, "Load From Folder", 120);
			
				if (GUILayout.Button("Load"))
				{
					m_config.LoadData();
					EditorUtility.SetDirty(m_config);
				}
			}
		}
#endif
	}
}