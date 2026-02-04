using UnityEditor;
using UnityEngine;

namespace RCore.Editor.AssetCleaner
{
    [InitializeOnLoad]
    public class RAssetCleanerOverlay
    {
        static RAssetCleanerOverlay()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect rect)
        {
            var settings = RAssetCleanerSettings.Instance;
            if (!RAssetCleanerWindow.IsOpen) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Draw Unused Overlay
            if (settings.showRedOverlay && RAssetCleaner.UnusedAssetsCache.Contains(path))
            {
                var originalColor = GUI.color;
                GUI.color = settings.unusedColor;
                GUI.Box(rect, GUIContent.none);
                GUI.color = originalColor;
            }
            
             // Draw Size Label (Right aligned)
             if (settings.showSize)
             {
                 if (!AssetDatabase.IsValidFolder(path))
                 {
                     long size = RAssetCleaner.GetAssetSize(path);
                     if (size > 0)
                     {
                         string sizeStr = EditorUtility.FormatBytes(size);
                         var style = EditorStyles.miniLabel;
                         var sizeSize = style.CalcSize(new GUIContent(sizeStr));
                         var sizeRect = new Rect(rect.x + rect.width - sizeSize.x - 2, rect.y, sizeSize.x, rect.height);
                         
                         if (rect.height > 20) // Grid View
                         {
                             // Draw at bottom
                             sizeRect = new Rect(rect.x, rect.y + rect.height - 15, rect.width, 15);
                             var centeredStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                             GUI.Label(sizeRect, sizeStr, centeredStyle);
                         }
                         else // List View
                         {
                             GUI.Label(sizeRect, sizeStr, style);
                         }
                     }
                 }
                 else
                 {
                     // Draw Folder Stats
                     if (RAssetCleaner.FolderStatsCache.TryGetValue(path, out var stats))
                     {
                         string statStr = $"{stats.unusedFilesCount} ({EditorUtility.FormatBytes(stats.unusedSize)})";
                         var style = EditorStyles.miniLabel;
                         var sizeSize = style.CalcSize(new GUIContent(statStr));
                         var sizeRect = new Rect(rect.x + rect.width - sizeSize.x - 2, rect.y, sizeSize.x, rect.height);
                         
                         if (rect.height > 20) // Grid View
                         {
                              sizeRect = new Rect(rect.x, rect.y + rect.height - 15, rect.width, 15);
                              var centeredStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = settings.unusedColor } };
                              GUI.Label(sizeRect, statStr, centeredStyle);
                         }
                         else
                         {
                              // Color the text to indicate "unused stuff inside"
                              var coloredStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = settings.unusedColor } };
                              GUI.Label(sizeRect, statStr, coloredStyle);
                         }
                     }
                 }
             }
        }
    }
}
