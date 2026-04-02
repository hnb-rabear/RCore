using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RCore.Editor.Tool
{
	public class GenerateAnimationsPackScriptTool : RCoreHubTool
	{
		public override string Name => "Animations Pack Script";
		public override string Category => "Generators";
		public override string Description => "Scan a folder containing FBX animations to auto-generate a C# Animation Pack script.";
		public override bool IsQuickAction => false;

		private List<AnimationClip> m_animationClips;
		private List<string> m_animationPaths;
		private REditorPrefString m_animationClipsPackScript;
		private REditorPrefString m_animationClipsPackPath;

		public override void Initialize()
		{
			m_animationClipsPackScript = new REditorPrefString(nameof(m_animationClipsPackScript));
			m_animationClipsPackPath = new REditorPrefString(nameof(m_animationClipsPackPath));
		}

		public override void DrawFocusMode()
		{
			if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
			{
				EditorGUILayout.HelpBox("Select FBX files that have Animations to scan", MessageType.Warning);
				return;
			}

			GUILayout.BeginVertical("box");
			if (EditorHelper.Button("Scan FBX Files"))
			{
				m_animationClips = new List<AnimationClip>();
				m_animationPaths = new List<string>();

				var objs = Selection.gameObjects;
				for (int i = 0; i < objs.Length; i++)
				{
					var path = AssetDatabase.GetAssetPath(objs[i]);
					var representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
					foreach (var asset in representations)
					{
						if (asset is AnimationClip clip)
						{
							m_animationClips.Add(clip);
							m_animationPaths.Add(path);
						}
					}
				}
			}

			if (m_animationClips != null && m_animationClips.Count > 0)
			{
				for (int i = 0; i < m_animationClips.Count; i++)
				{
					var clip = m_animationClips[i];
					EditorGUILayout.LabelField($"{i}: {clip.name} | {clip.length} | {clip.wrapMode} | {clip.isLooping}");
				}

				GUILayout.BeginHorizontal();
				m_animationClipsPackScript.Value = EditorHelper.TextField(m_animationClipsPackScript.Value, "Script Name");

				if (EditorHelper.ButtonColor("Generate Script", Color.cyan))
				{
					if (string.IsNullOrEmpty(m_animationClipsPackScript.Value)) return;

					string fieldsName = "";
					string enum_ = "\tpublic enum Clip \n\t{\n";
					string indexes = "";
					string arrayElements = "";
					string paths = "";
					string names = "";
					string validateFields = "";
					int i = 0;
					foreach (var clip in m_animationClips)
					{
						string fieldName = clip.name.ToCapitalizeEachWord().Replace(" ", "").RemoveSpecialCharacters();
						fieldsName += $"\tpublic AnimationClip {fieldName};\n";
						enum_ += $"\t\t{fieldName} = {i},\n";
						indexes += $"\tpublic const int {fieldName}_ID = {i};\n";
						names += $"\tpublic const string {fieldName}_NAME = \"{clip.name}\";\n";
						arrayElements += $"\t\t\t\t{fieldName},\n";
						paths += $"\tprivate const string {fieldName}_PATH = \"{m_animationPaths[i]}\";\n";
						validateFields += $"\t\t\tif ({fieldName} == null) {fieldName} = RCore.Editor.EditorHelper.GetAnimationFromModel({fieldName}_PATH, {fieldName}_NAME);\n";
						validateFields += $"\t\t\tif ({fieldName} == null) Debug.LogError(nameof({fieldName}) + \" is Null\");\n";
						i++;
					}

					enum_ += "\t}\n";
					var template = Resources.Load<TextAsset>("AnimationsPackTemplate.txt");
					if (template != null)
					{
						var generatedContent = template.text
							.Replace("<class_name>", m_animationClipsPackScript.Value)
							.Replace("<enum_>", enum_)
							.Replace("<const>", indexes)
							.Replace("<fieldsName>", fieldsName)
							.Replace("<names>", names)
							.Replace("<paths>", paths)
							.Replace("<arrayElements>", arrayElements)
							.Replace("<validateFields>", validateFields);

						m_animationClipsPackPath.Value = EditorHelper.SaveFilePanel(m_animationClipsPackPath.Value, m_animationClipsPackScript.Value, generatedContent, "cs");
					}
					else
					{
						Debug.LogError("AnimationsPackTemplate.txt not found in Resources folder!");
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}
	}

	public class GenerateCharactersSetTool : RCoreHubTool
	{
		public override string Name => "Generate Characters Set";
		public override string Category => "Generators";
		public override string Description => "Merge Text files and extract a deduplicated character set string for Font generation.";
		public override bool IsQuickAction => false;

		private string m_Characters;
		private List<TextAsset> m_TextFiles = new List<TextAsset>();
		private string m_ContentForCreatingCharacterSet;

		public override void DrawFocusMode()
		{
			if (EditorHelper.ButtonColor("Add Txt File Slot", Color.green))
				m_TextFiles.Add(null);
			
			EditorHelper.DragDropBox<TextAsset>("Text Files Drop Area", objs =>
			{
				foreach (var obj in objs)
					m_TextFiles.Add(obj);
			});

			GUILayout.Label("Extra Context String:", EditorStyles.boldLabel);
			m_ContentForCreatingCharacterSet = EditorHelper.TextArea(m_ContentForCreatingCharacterSet, null);

			for (int i = 0; i < m_TextFiles.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				m_TextFiles[i] = (TextAsset)EditorHelper.ObjectField<TextAsset>(m_TextFiles[i], null);
				if (EditorHelper.ButtonColor("+", Color.green, 23)) m_TextFiles.Insert(i + 1, null);
				if (EditorHelper.ButtonColor("x", Color.red, 23)) m_TextFiles.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
			}

			if (EditorHelper.ButtonColor("Process Duplicates & Generate", Color.cyan))
			{
				string combineStr = "";
				foreach (var textAsset in m_TextFiles)
					if (textAsset != null) combineStr += textAsset.text;
				combineStr += m_ContentForCreatingCharacterSet;

				m_Characters = string.Empty;
				var unique = new HashSet<char>(combineStr);
				foreach (char c in unique) m_Characters += c;
				m_Characters = string.Concat(m_Characters.OrderBy(c => c));
			}

			if (!string.IsNullOrEmpty(m_Characters))
			{
				GUILayout.Label("Resulting Characters:", EditorStyles.boldLabel);
				EditorHelper.TextArea(m_Characters, null);
				if (EditorHelper.ButtonColor("Save Characters Set (.txt)", Color.cyan))
					EditorHelper.SaveFilePanel(null, "combined_text", m_Characters, "txt");
			}
		}
	}

	public class GenerateJsonFileListTool : RCoreHubTool
	{
		public override string Name => "JSON File List";
		public override string Category => "Generators";
		public override string Description => "Extract filenames of the currently selected files and export them to a JSON file.";
		public override bool IsQuickAction => true;

		public override void DrawCard()
		{
			if (Selection.objects == null || Selection.objects.Length == 0)
			{
				GUILayout.Label("Select any files in Project window");
				return;
			}

			if (EditorHelper.ButtonColor("Generate JSON from Selection", Color.cyan))
			{
				var fileNames = new List<string>();
				string folderPath = "";
				foreach (var t in Selection.objects)
				{
					var path = AssetDatabase.GetAssetPath(t);
					if (string.IsNullOrEmpty(path)) continue;
					var fileName = Path.GetFileName(path);
					fileNames.Add(fileName);
					if (string.IsNullOrEmpty(folderPath))
						folderPath = Path.GetDirectoryName(path);
				}
				
				var json = JsonHelper.ToJson(fileNames);
				EditorHelper.SaveFilePanel(folderPath, "all_files", json, "json");
			}
		}
	}

	public class GeneratorScreenshotTool : RCoreHubTool
	{
		public override string Name => "Screenshot Taker";
		public override string Category => "Generators";
		public override string Description => "Capture the Game View or a custom Camera with configurable resolution and optional transparent background.";
		public override bool IsQuickAction => false;

		private ScreenshotTakerDrawer m_drawer;

		public override void Initialize()
		{
			m_drawer = new ScreenshotTakerDrawer();
		}

		public override void DrawFocusMode()
		{
			m_drawer.DrawOnGUI();
		}
	}
}
