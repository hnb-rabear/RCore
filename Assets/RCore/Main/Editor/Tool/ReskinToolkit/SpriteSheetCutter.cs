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
			m_renameSprites = EditorHelper.Toggle(m_renameSprites, "Rename sprites",150);
			if (m_renameSprites)
			{
				m_spriteNamePattern = EditorHelper.TextField(m_spriteNamePattern, "Sprite name pattern", 150);
				m_stringRemovedInName = EditorHelper.TextField(m_stringRemovedInName, "String removed in name", 150);
				m_prefix = EditorHelper.TextField(m_prefix, "Prefix in name", 150);
				m_suffix = EditorHelper.TextField(m_suffix, "Suffix in name", 150);
				m_renameOrigin = EditorHelper.Toggle(m_renameOrigin, "Rename originals", 150);
			}
			bool renameOrigin = m_renameSprites && m_renameOrigin;
			EditorHelper.DragDropBox<Texture2D>("Sprite sheet", objs =>
			{
				foreach (var obj in objs)
				{
					if (!m_spriteSheets.Contains(obj))
						m_spriteSheets.Add(obj);
				}
			});
			if (m_spriteSheets.Count > 0)
			{
				for (int i = 0; i < m_spriteSheets.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					var spriteSheet = m_spriteSheets[i];
					spriteSheet = (Texture2D)EditorHelper.ObjectField<Texture2D>(spriteSheet, null);
					if (m_renameSprites)
						EditorGUILayout.LabelField(GetCustomName(spriteSheet.name));
					if (spriteSheet != null)
					{
						if (EditorHelper.Button("Export sprites", 100))
						{
							string customName = GetCustomName(spriteSheet.name);
							EditorHelper.ExportSpritesFromTexture(spriteSheet, null, m_renameSprites ? customName : null, renameOrigin);
						}
						if (EditorHelper.ButtonColor("x", Color.red, 23))
						{
							m_spriteSheets.RemoveAt(i);
							i--;
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				if (EditorHelper.Button("Export sprites"))
				{
					foreach (var spriteSheet in m_spriteSheets)
					{
						string customName = GetCustomName(spriteSheet.name);
						EditorHelper.ExportSpritesFromTexture(spriteSheet, null, m_renameSprites ? customName : null, renameOrigin);
					}
				}
				if (EditorHelper.Button("Sort"))
					m_spriteSheets = m_spriteSheets.OrderBy(x => x.name).ToList();
				if (EditorHelper.Button("Remove sprite sheets"))
					m_spriteSheets.Clear();
			}
		}
	}
}