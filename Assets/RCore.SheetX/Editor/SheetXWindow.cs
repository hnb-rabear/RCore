using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// The main editor window for the SheetX tool, providing tabs for Excel, Google Sheets, and Settings.
	/// </summary>
	public class SheetXWindow : EditorWindow
	{
		private const string NAME = "SheetX: Sheets Exporter";
		private const string MENU = "SheetX";

		private Vector2 m_scrollPosition;

		private SheetXSettings m_settings;
		private ExcelSheetXWindow m_excelSheetXWindow;
		private GoogleSheetXWindow m_googleSheetXWindow;
		private SheetXSettingsWindow m_settingsWindow;

		private void OnEnable()
		{
			m_settings = SheetXSettings.Init();
			m_excelSheetXWindow ??= new ExcelSheetXWindow();
			m_excelSheetXWindow.OnEnable();
			m_excelSheetXWindow.editorWindow = this;
			m_googleSheetXWindow ??= new GoogleSheetXWindow();
			m_googleSheetXWindow.OnEnable();
			m_googleSheetXWindow.editorWindow = this;
			m_settingsWindow ??= new SheetXSettingsWindow();
			m_settingsWindow.OnEnable(this);
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);

			GUILayout.BeginHorizontal();
			var iconSave = EditorIcon.GetIcon(EditorIcon.Icon.SaveAs);
			if (EditorHelper.Button(null, iconSave, default, 30, 30))
				m_settingsWindow.Save();
			var iconLoad = EditorIcon.GetIcon(EditorIcon.Icon.FolderOpened);
			if (EditorHelper.Button(null, iconLoad, default, 30, 30))
				m_settingsWindow.Load();

			GUILayout.FlexibleSpace();
			var iconDocument = EditorIcon.GetIcon(EditorIcon.Icon.Document);
			if (DocumentButton("Docs (EN)", iconDocument, 110,
				    "Open the English SheetX manual on GitHub."))
				Application.OpenURL(SheetXConstants.DOCUMENT_EN_URL);
			if (DocumentButton("Tài liệu (VI)", iconDocument, 130,
				    "Mở tài liệu SheetX tiếng Việt trên GitHub."))
				Application.OpenURL(SheetXConstants.DOCUMENT_VI_URL);
			GUILayout.EndHorizontal();

			var tab = EditorHelper.Tabs($"{nameof(SheetXWindow)}", "Excel Spreadsheets", "Google Spreadsheets", "Settings");
			switch (tab)
			{
				case "Excel Spreadsheets":
					m_excelSheetXWindow.OnGUI();
					break;
				case "Settings":
					m_settingsWindow.OnGUI();
					break;
				case "Google Spreadsheets":
					m_googleSheetXWindow.OnGUI();
					break;
			}

			GUILayout.EndScrollView();
		}

		// A button whose icon is smaller than its own height. GUIStyle scales the icon to the
		// content rect, so the only way to keep the button at 30 and the icon below it is to
		// pad the content rect back down — imagePosition alone would still draw it at full height.
		private static bool DocumentButton(string label, Texture icon, int width, string tooltip)
		{
			var style = new GUIStyle(GUI.skin.button)
			{
				fixedWidth = width,
				fixedHeight = 30,
				// 30 tall, 21 of it icon: the remaining 9 is split top and bottom.
				padding = new RectOffset(6, 6, 5, 4),
				imagePosition = ImagePosition.ImageLeft,
				alignment = TextAnchor.MiddleCenter
			};
			return GUILayout.Button(new GUIContent(label, icon, tooltip), style, GUILayout.MinHeight(30));
		}

		// Every tab mutates m_settings in place, so flush once on focus loss / close rather than
		// after each individual edit. Both callbacks also fire during assembly reload, where an
		// AssetDatabase write is unsafe — mark dirty here and let delayCall do the actual write.
		private void OnLostFocus() => FlushSettings();

		private void OnDisable()
		{
			m_settingsWindow?.OnDisable();
			FlushSettings();
		}

		private void FlushSettings()
		{
			if (m_settings == null)
				return;
			var settings = m_settings;
			EditorUtility.SetDirty(settings);
			EditorApplication.delayCall += () =>
			{
				if (settings != null)
					AssetDatabase.SaveAssetIfDirty(settings);
			};
		}

#if !IKIT_SHEETX
#if ASSETS_STORE
		[MenuItem("Window/" + MENU)]
#else
		[MenuItem("RCore/" + MENU, priority = 24)]
#endif
#endif
		/// <summary>
		/// Opens the SheetX editor window.
		/// </summary>
		public static void ShowWindow()
		{
			var window = GetWindow<SheetXWindow>(NAME, true);
			window.Show();
		}

		// A sibling, not a child: "RCore/SheetX" is a leaf command, and nesting under it would turn
		// it into a submenu and hide the window itself.
#if !IKIT_SHEETX
#if ASSETS_STORE
		[MenuItem("Window/" + MENU + ": Restore Migration Snapshot")]
#else
		[MenuItem("RCore/" + MENU + ": Restore Migration Snapshot", priority = 25)]
#endif
#endif
		/// <summary>
		/// Puts back the generated collection sources a failed depth migration replaced, then
		/// refreshes so the restored sources recompile. Greyed out when nothing is pending.
		/// Names the collections first: a snapshot an abandoned migration left behind is the one
		/// Capture keeps, so a later restore can revert sources the user no longer expects.
		/// </summary>
		private static void RestoreMigrationSnapshot()
		{
			if (!SheetXMigrationSnapshot.TryPeek(out var collections, out _))
			{
				Debug.LogError("No migration snapshot to restore.");
				return;
			}

			string names = collections.Count == 0 ? "(none recorded)" : string.Join(", ", collections);
			int choice = EditorUtility.DisplayDialogComplex(
				"Restore Migration Snapshot",
				$"This reverts the generated sources for: {names}.\n\n"
				+ "If that is not the storage change you just made, the snapshot is left over "
				+ "from an earlier migration and restoring it would undo the wrong export. "
				+ "Discard throws the snapshot away without touching any file.",
				"Restore", "Cancel", "Discard");
			if (choice == 2)
			{
				SheetXMigrationSnapshot.Clear();
				Debug.LogWarning("SheetX: migration snapshot discarded without restoring anything. "
					+ "Future migrations can now capture their own snapshot.");
				return;
			}
			if (choice != 0)
				return;

			if (!SheetXMigrationSnapshot.TryRestore(out string error))
			{
				Debug.LogError(error);
				return;
			}
			AssetDatabase.Refresh();
			// Global's field changed from object reference to inline value and back across the
			// reloads, so Unity dropped the reference. The .asset survives with its GUID; only a
			// bake repoints at it, and restore itself does not bake.
			Debug.LogWarning("SheetX: previous collection sources restored. Global's reference to the "
				+ "restored collection is empty until you re-bake — use Manage Collections > Load All "
				+ "Collections, or export again.");
		}

#if !IKIT_SHEETX
#if ASSETS_STORE
		[MenuItem("Window/" + MENU + ": Restore Migration Snapshot", true)]
#else
		[MenuItem("RCore/" + MENU + ": Restore Migration Snapshot", true)]
#endif
#endif
		private static bool RestoreMigrationSnapshotEnabled() => SheetXMigrationSnapshot.Exists;
	}
}