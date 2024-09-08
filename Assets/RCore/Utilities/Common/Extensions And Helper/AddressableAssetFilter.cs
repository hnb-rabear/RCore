#if UNITY_EDITOR && ADDRESSABLES
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace RCore.Common
{
	[InitializeOnLoad]
	public class AddressableAssetFilter
	{
		private static Dictionary<string, Color> m_Colors;
		private static Dictionary<string, Color> m_Directories;
		private static List<string> m_ignoreGuids;
		private static EditorPrefsBool m_Active;

		public static bool Active
		{
			get => m_Active.Value;
			set
			{
				if (m_Active.Value != value)
					Init();
				m_Active.Value = value;
			}
		}

		static AddressableAssetFilter()
		{
			Init();
			EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
			if (AddressableAssetSettingsDefaultObject.Settings != null)
				AddressableAssetSettingsDefaultObject.Settings.OnModification += OnSettingsModificationCustom;
		}

		private static void Init()
		{
			m_Active = new EditorPrefsBool(nameof(AddressableAssetFilter), true);
			m_ignoreGuids = new List<string>();
			m_Colors = new Dictionary<string, Color>();
			m_Directories = new Dictionary<string, Color>();
		}

		private static void OnSettingsModificationCustom(AddressableAssetSettings arg1, AddressableAssetSettings.ModificationEvent arg2, object arg3)
		{
			Init();
		}

		private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
		{
			if (!m_Active.Value || string.IsNullOrEmpty(guid) || Event.current.type != EventType.Repaint || m_ignoreGuids.Contains(guid))
				return;

			DrawColorMark(guid, selectionRect);
		}

		private static void DrawColorMark(string guid, Rect selectionRect)
		{
			if (!m_Colors.TryGetValue(guid, out Color color))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
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
					else if (lbl.StartsWith("De")) //Default Local Group
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
	}
}
#endif