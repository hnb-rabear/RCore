/**
 * Author HNB-RaBear - 2024
 **/

#if ADDRESSABLES
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Visualizes Addressable Asset Groups in the Project window with color coding.
	/// </summary>
	[InitializeOnLoad]
	public class AddressableAssetsGroupsColorizer
	{
		private const string MENU_ITEM = "Addressable/Toggle Groups Colorizer";
		private const string SETTINGS = "Addressable/Groups Colorizer Settings";
		
		private static Dictionary<string, Color> m_Colors;
		private static Dictionary<string, Color> m_Directories;
		private static HashSet<string> m_IgnoreGuids;
		private static REditorPrefBool m_Active;
		private static Dictionary<string, string> m_PathCache = new Dictionary<string, string>();

		private static AddressableAssetsGroupsColorizerSettings m_Settings;

		[MenuItem(RMenu.R_TOOLS + MENU_ITEM, priority = RMenu.GROUP_12 + 1)]
		private static void ToggleActive()
		{
			Init(); // Reinitialize when toggled
			m_Active.Value = !m_Active.Value;
		}

		[MenuItem(RMenu.R_TOOLS + MENU_ITEM, true)]
		private static bool ToggleActiveValidate()
		{
			Menu.SetChecked(RMenu.R_TOOLS + MENU_ITEM, m_Active.Value);
			return true;
		}
		
		[MenuItem(RMenu.R_TOOLS + SETTINGS, priority = RMenu.GROUP_12 + 2)]
		private static void OpenSettings()
		{
			LoadSettings();
			if (m_Settings != null)
			{
				Selection.activeObject = m_Settings;
				EditorGUIUtility.PingObject(m_Settings);
			}
			else
			{
				if (EditorUtility.DisplayDialog("Settings not found", 
					    "AddressableAssetsGroupsColorizerSettings asset not found in Resources or project.\n\nWould you like to create one now?", "Create", "Cancel"))
				{
					var path = EditorUtility.SaveFilePanelInProject("Save Settings", "AddressableAssetsGroupsColorizerSettings", "asset", "Save AddressableAssetsGroupsColorizerSettings");
					if (!string.IsNullOrEmpty(path))
					{
						m_Settings = ScriptableObject.CreateInstance<AddressableAssetsGroupsColorizerSettings>();
						AssetDatabase.CreateAsset(m_Settings, path);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
						Selection.activeObject = m_Settings;
						EditorGUIUtility.PingObject(m_Settings);
					}
				}
			}
		}

		static AddressableAssetsGroupsColorizer()
		{
			Init();
			EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
			if (AddressableAssetSettingsDefaultObject.Settings != null)
				AddressableAssetSettingsDefaultObject.Settings.OnModification += OnSettingsModificationCustom;
		}

		private static void Init()
		{
			m_Active = new REditorPrefBool(nameof(AddressableAssetsGroupsColorizer), true);
			m_IgnoreGuids = new HashSet<string>();
			m_Colors = new Dictionary<string, Color>();
			m_Directories = new Dictionary<string, Color>();
			m_PathCache = new Dictionary<string, string>();
			LoadSettings();
		}
		
		private static void LoadSettings()
		{
			m_Settings = Resources.Load<AddressableAssetsGroupsColorizerSettings>(nameof(AddressableAssetsGroupsColorizerSettings));
			if (m_Settings == null)
			{
				string[] guids = AssetDatabase.FindAssets("t:" + nameof(AddressableAssetsGroupsColorizerSettings));
				if (guids.Length > 0)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[0]);
					m_Settings = AssetDatabase.LoadAssetAtPath<AddressableAssetsGroupsColorizerSettings>(path);
				}
			}
		}

		private static void OnSettingsModificationCustom(AddressableAssetSettings arg1, AddressableAssetSettings.ModificationEvent arg2, object arg3)
		{
			if (arg2 == AddressableAssetSettings.ModificationEvent.EntryAdded
			    || arg2 == AddressableAssetSettings.ModificationEvent.EntryMoved
			    || arg2 == AddressableAssetSettings.ModificationEvent.EntryCreated
			    || arg2 == AddressableAssetSettings.ModificationEvent.EntryModified
			    || arg2 == AddressableAssetSettings.ModificationEvent.EntryRemoved)
				Init();
		}

		private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
		{
			if (!m_Active.Value || string.IsNullOrEmpty(guid) || Event.current.type != EventType.Repaint || m_IgnoreGuids.Contains(guid))
				return;

			DrawColorMark(guid, selectionRect);
		}

		private static string GetAssetPath(string guid)
		{
			if (!m_PathCache.TryGetValue(guid, out string path))
			{
				path = AssetDatabase.GUIDToAssetPath(guid);
				m_PathCache[guid] = path;
			}
			return path;
		}

		private static void DrawColorMark(string guid, Rect selectionRect)
		{
			if (!m_Colors.TryGetValue(guid, out Color color))
			{
				var path = GetAssetPath(guid);
				if (path.EndsWith(".cs"))
				{
					m_IgnoreGuids.Add(guid);
					return;
				}
				var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
				if (aaSettings == null)
					return;
				var entry = aaSettings.FindAssetEntry(guid, true);
				if (entry != null && entry.parentGroup != null)
				{
					string lbl = entry.parentGroup.name;
					bool colorFound = false;

					if (m_Settings != null && m_Settings.rules != null)
					{
						foreach (var rule in m_Settings.rules)
						{
							if (lbl.StartsWith(rule.prefix))
							{
								color = rule.color;
								colorFound = true;
								break;
							}
						}
					}

					if (!colorFound)
					{
						// Fallback to hardcoded defaults if settings missing or no match found yet
						if (m_Settings == null)
						{
							if (lbl.StartsWith("In")) //InstallTime
								color = Color.green;
							else if (lbl.StartsWith("Fa")) //FastFollow
								color = Color.blue;
							else if (lbl.StartsWith("On")) //OnDemand
								color = Color.cyan;
							else if (lbl.StartsWith("Ex")) //Excluded
								color = Color.red;
							else
								color = Color.yellow;
						}
						else
						{
							color = Color.yellow;
						}
					}
					
					m_Colors.Add(guid, color);

					if (AssetDatabase.IsValidFolder(path))
						m_Directories.Add(path, color);
				}
				else
				{
					bool isSubAsset = false;
					foreach (var directory in m_Directories)
						if (path.StartsWith(directory.Key))
						{
							isSubAsset = true;
							color = directory.Value;
							m_Colors.Add(guid, color);
							break;
						}
					if (!isSubAsset)
						m_IgnoreGuids.Add(guid);
				}
			}
			else
			{
				var oldColor = GUI.color;
				GUI.color = color;
				GUI.Box(selectionRect, "");
				GUI.color = oldColor;
			}
		}
	}
}
#endif