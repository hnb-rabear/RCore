using RCore.Editor.Tool;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class RCoreEditorConfig : ScriptableObject
	{
		private static readonly string m_FilePath = $"Assets/Editor/{nameof(RCoreEditorConfig)}.asset";
		
		private static RCoreEditorConfig m_Instance;
		public static RCoreEditorConfig Instance
		{
			get
			{
				m_Instance = AssetDatabase.LoadAssetAtPath(m_FilePath, typeof(RCoreEditorConfig)) as RCoreEditorConfig;
				if (m_Instance == null)
					m_Instance = EditorHelper.CreateScriptableAsset<RCoreEditorConfig>(m_FilePath);
				return m_Instance;
			}
		}
		
		public string tinyPngApiKey = "";
	}
}