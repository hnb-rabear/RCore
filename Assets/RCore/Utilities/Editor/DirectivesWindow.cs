using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace RCore.Common
{
	[Serializable]
	public class Directive
	{
		public Directive() { }
		public Directive(string pName, bool pEnable)
		{
			name = pName;
			enabled = pEnable;
		}
		public string name;
		public bool enabled = true;
		public bool @fixed;
	}

	public class DirectivesWindow : EditorWindow
	{
		private List<Directive> m_directives = new List<Directive>();

		private void OnEnable()
		{
			using (File.AppendText("Assets/Editor/DirectivesCollection.txt")) { }

			m_directives = LoadDirectives();

			var currentDefines = GetCurrentDefines();
			for (int i = 0; i < currentDefines.Count; i++)
			{
				if (!ContainDefine(currentDefines[i]))
					m_directives.Add(new Directive(currentDefines[i], true));
			}

			for (int i = 0; i < m_directives.Count; i++)
			{
				if (currentDefines.Count > 0)
				{
					bool isUsed = false;
					for (int j = 0; j < currentDefines.Count; j++)
					{
						if (currentDefines[j] == m_directives[i].name)
						{
							isUsed = true;
							break;
						}
					}
					m_directives[i].enabled = isUsed;
				}
				else
				{
					m_directives[i].enabled = false;
				}
			}
		}

		private void OnGUI()
		{
			DrawDirectives();
		}

		private void DrawDirectives()
		{
			float windowWidth = position.width - 10;
			EditorHelper.BoxVertical("Projective Directives", () =>
			{
				float w1 = windowWidth * 0.6f;
				float w2 = windowWidth * 0.1f;
				float w3 = windowWidth * 0.1f;
				float w4 = windowWidth * 0.1f;

				if (m_directives.Count > 0)
					EditorHelper.BoxHorizontal(() =>
					{
						GUILayout.Label("Directive", EditorStyles.boldLabel, GUILayout.Width(w1), GUILayout.Height(30));
						GUILayout.Label("Enable", EditorStyles.boldLabel, GUILayout.Width(w2));
						GUILayout.Label("Fixed", EditorStyles.boldLabel, GUILayout.Width(w3));
						GUILayout.Label("", EditorStyles.boldLabel, GUILayout.Width(w4));
					});

				for (int i = 0; i < m_directives.Count; i++)
				{
					int i1 = i;
					EditorHelper.BoxHorizontal(() =>
					{
						m_directives[i1].name = EditorHelper.TextField(m_directives[i1].name, "", 0, (int)w1);
						m_directives[i1].enabled = EditorHelper.Toggle(m_directives[i1].enabled, "", 0, (int)w2);
						m_directives[i1].@fixed = EditorHelper.Toggle(m_directives[i1].@fixed, "", 0, (int)w3);
						if (!m_directives[i1].@fixed)
						{
							EditorHelper.ButtonColor("X", () =>
							{
								m_directives.RemoveAt(i1);
							}, Color.red, (int)w4);
						}
						else
							GUILayout.Label("Lock", EditorStyles.boldLabel, GUILayout.Width(w4));
					});
				}

				//===================================================

				EditorHelper.BoxHorizontal(() =>
				{
					EditorHelper.ButtonColor("Add Directive", () =>
					{
						var lastDirective = m_directives.LastOrDefault();
						var newDirective = new Directive();
						if (lastDirective != null)
						{
							newDirective.enabled = lastDirective.enabled;
						}
						m_directives.Add(newDirective);
					});
					//
					EditorHelper.ButtonColor("Save", () => SaveDirectives(m_directives));
					//
					EditorHelper.ButtonColor("Apply", ApplyDirectiveSymbols, Color.green);
					//
					EditorHelper.ButtonColor("Revert", () => m_directives = LoadDirectives(), Color.yellow);
				});

			}, default, true, windowWidth);
		}

		private void ApplyDirectiveSymbols()
		{
			string symbols = string.Join(";", m_directives.Where(d => d.enabled).Select(d => d.name).ToArray());
			var target = EditorUserBuildSettings.selectedBuildTargetGroup;
			PlayerSettings.SetScriptingDefineSymbolsForGroup(target, symbols);
		}

		private List<Directive> LoadDirectives()
		{
			List<Directive> directives;

			var serializer = new XmlSerializer(typeof(List<Directive>));
			using TextReader reader = new StreamReader("Assets/Editor/DirectivesCollection.txt");
			try
			{
				directives = (List<Directive>)serializer.Deserialize(reader);
			}
			catch
			{
				directives = new List<Directive>();
			}

			return directives;
		}

		private void SaveDirectives(List<Directive> directives)
		{
			var serializer = new XmlSerializer(typeof(List<Directive>));
			using (TextWriter writer = new StreamWriter("Assets/Editor/DirectivesCollection.txt"))
			{
				serializer.Serialize(writer, directives);
			}
			AssetDatabase.Refresh();
		}

		private List<string> GetCurrentDefines()
		{
			string defineStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			var currentDefines = defineStr.Split(';').ToList();
			for (int i = 0; i < currentDefines.Count; i++)
				currentDefines[i] = currentDefines[i].Trim();
			return currentDefines;
		}

		private bool ContainDefine(string pDefine)
		{
			if (string.IsNullOrEmpty(pDefine))
				return true;

			for (int i = 0; i < m_directives.Count; i++)
			{
				if (m_directives[i].name == pDefine)
					return true;
			}
			return false;
		}

		[MenuItem("RCore/Tools/Open Directives Window (obsolete)")]
		private static void OpenDirectivesEditorWindow()
		{
			var window = GetWindow<DirectivesWindow>("Directives Manager", true);
			window.Show();
		}
	}
}