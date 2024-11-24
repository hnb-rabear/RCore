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
	[InitializeOnLoad]
	public class PlayAssetDeliveryFilter
	{
		private static Dictionary<string, Color> m_Colors;
		private static Dictionary<string, Color> m_Directories;
		private static HashSet<string> m_ignoreGuids;
		private static REditorPrefBool m_Active;
		private static Dictionary<string, string> m_PathCache = new Dictionary<string, string>();

		[MenuItem(RMenu.R_TOOLS + "Toggle Play Asset Delivery Filter")]
		private static void ToggleActive()
		{
			Init(); // Reinitialize when toggled
			m_Active.Value = !m_Active.Value;
		}

		[MenuItem(RMenu.R_TOOLS + "Toggle Play Asset Delivery Filter", true)]
		private static bool ToggleActiveValidate()
		{
			Menu.SetChecked(RMenu.R_TOOLS + "Toggle Play Asset Delivery Filter", m_Active.Value);
			return true;
		}

		static PlayAssetDeliveryFilter()
		{
			Init();
			EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
			if (AddressableAssetSettingsDefaultObject.Settings != null)
				AddressableAssetSettingsDefaultObject.Settings.OnModification += OnSettingsModificationCustom;
		}

		private static void Init()
		{
			m_Active = new REditorPrefBool(nameof(PlayAssetDeliveryFilter), true);
			m_ignoreGuids = new HashSet<string>();
			m_Colors = new Dictionary<string, Color>();
			m_Directories = new Dictionary<string, Color>();
			m_PathCache = new Dictionary<string, string>();
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
			if (!m_Active.Value || string.IsNullOrEmpty(guid) || Event.current.type != EventType.Repaint || m_ignoreGuids.Contains(guid))
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
					m_ignoreGuids.Add(guid);
					return;
				}
				var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
				if (aaSettings == null)
					return;
				var entry = aaSettings.FindAssetEntry(guid, true);
				if (entry != null && entry.parentGroup != null)
				{
					string lbl = entry.parentGroup.name;
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
						m_ignoreGuids.Add(guid);
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
		
		public static Color GetColorForString(string input)
		{
			// Use a hash function to get a unique integer based on the input string
			int hash = input.GetHashCode();
        
			// Use the hash to generate pseudo-random RGB values
			float r = (hash & 0xFF0000) >> 16;
			float g = (hash & 0x00FF00) >> 8;
			float b = (hash & 0x0000FF);

			// Normalize the RGB values to be between 0 and 1
			r /= 255.0f;
			g /= 255.0f;
			b /= 255.0f;

			return new Color(r, g, b);
		}
	}
}
#endif