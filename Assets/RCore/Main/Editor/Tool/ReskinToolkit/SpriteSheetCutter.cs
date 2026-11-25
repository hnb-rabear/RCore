using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Helper class to slice sprite sheets into individual sprites and export them.
	/// </summary>
	[Serializable]
	public class SpriteSheetCutter
	{
		[SerializeField] private List<Texture2D> m_spriteSheets = new List<Texture2D>();
		[SerializeField] private bool m_renameSprites;
		[SerializeField] private string m_spriteNamePattern;
		[SerializeField] private string m_stringRemovedInName;
		[SerializeField] private string m_prefix;
		[SerializeField] private string m_suffix;
		[SerializeField] private bool m_renameOrigin;
		
		public void Clear()
		{
			m_spriteSheets.Clear();
		}

		public void RemoveNull()
		{
			for (int i = m_spriteSheets.Count - 1; i >= 0; i--)
				if (m_spriteSheets[i] == null)
					m_spriteSheets.RemoveAt(i);
		}

		private string GetCustomName(string pName)
		{
			string customName = pName;
			if (m_renameSprites)
			{
				customName = string.IsNullOrEmpty(m_spriteNamePattern) ? $"{pName}" : $"{m_spriteNamePattern}";
				if (!string.IsNullOrEmpty(m_stringRemovedInName))
					customName = customName.Replace(m_stringRemovedInName, "");
				if (!string.IsNullOrEmpty(m_prefix))
					customName = m_prefix + customName;
				if (!string.IsNullOrEmpty(m_suffix))
					customName = customName + m_suffix;
			}
			return customName;
		}

		public void Draw()
		{
			m_spriteSheets ??= new List<Texture2D>();
			m_renameSprites = EditorGUILayout.ToggleLeft("Rename exported sprites", m_renameSprites, EditorStyles.boldLabel);
			if (m_renameSprites)
			{
				EditorGUI.indentLevel++;
				m_spriteNamePattern = EditorGUILayout.TextField("Name pattern", m_spriteNamePattern);
				m_stringRemovedInName = EditorGUILayout.TextField("Remove from name", m_stringRemovedInName);
				m_prefix = EditorGUILayout.TextField("Prefix", m_prefix);
				m_suffix = EditorGUILayout.TextField("Suffix", m_suffix);
				m_renameOrigin = EditorGUILayout.Toggle("Rename originals", m_renameOrigin);
				var first = m_spriteSheets.FirstOrDefault(x => x != null);
				EditorGUILayout.LabelField("Preview", GetCustomName(first != null ? first.name : "SheetName"));
				EditorGUI.indentLevel--;
			}

			EditorHelper.DragDropBox<Texture2D>("Sprite sheets", objs =>
			{
				foreach (var obj in objs)
					if (!m_spriteSheets.Contains(obj))
						m_spriteSheets.Add(obj);
			});

			int remove = -1;
			for (int i = 0; i < m_spriteSheets.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				m_spriteSheets[i] = (Texture2D)EditorGUILayout.ObjectField(m_spriteSheets[i], typeof(Texture2D), false);
				var sheet = m_spriteSheets[i];
				if (sheet != null)
				{
					if (m_renameSprites)
						GUILayout.Label(GetCustomName(sheet.name));
					if (EditorHelper.Button("Export", 60))
					{
						Export(sheet);
						GUIUtility.ExitGUI(); // export refreshes the AssetDatabase mid-layout
					}
				}
				if (EditorHelper.ButtonColor("x", Color.red, 23))
					remove = i;
				EditorGUILayout.EndHorizontal();
			}
			if (remove >= 0)
				m_spriteSheets.RemoveAt(remove);

			if (m_spriteSheets.Count == 0)
				return;
			EditorGUILayout.BeginHorizontal();
			if (EditorHelper.Button("Export all"))
			{
				foreach (var sheet in m_spriteSheets.Where(x => x != null).ToList())
					Export(sheet);
				GUIUtility.ExitGUI();
			}
			if (EditorHelper.Button("Sort"))
				m_spriteSheets = m_spriteSheets.Where(x => x != null).OrderBy(x => x.name).ToList();
			if (EditorHelper.ButtonColor("Clear", Color.red))
				m_spriteSheets.Clear();
			EditorGUILayout.EndHorizontal();
		}

		private void Export(Texture2D pSheet)
			=> EditorHelper.ExportSpritesFromTexture(pSheet, null, m_renameSprites ? GetCustomName(pSheet.name) : null, m_renameSprites && m_renameOrigin);
	}
}