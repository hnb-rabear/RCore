using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RCore.RAssetFilter.Editor
{
    [InitializeOnLoad]
    public class RAssetFilterOverlay
    {
        private struct LabelEntry
        {
            public string label;
            public bool highlight;
        }

        private static readonly Dictionary<string, LabelEntry> LabelCache = new Dictionary<string, LabelEntry>();
        private static readonly StringBuilder LabelBuilder = new StringBuilder(32);
        private static readonly GUIContent MeasureContent = new GUIContent();

        private static GUIStyle m_gridStyle;
        private static GUIStyle m_gridHighlightStyle;
        private static GUIStyle m_listHighlightStyle;
        private static Color m_styleUnusedColor;
        private static int m_labelCacheGeneration = -1;
        private static bool m_labelCacheShowSize;
        private static bool m_labelCacheShowReferenceCount;

        static RAssetFilterOverlay()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect rect)
        {
            if (!RAssetFilterWindow.IsOpen) return;

            var settings = RAssetFilterSettings.Instance;
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Only Assets/ paths are scanned, so only they can carry meaningful cache results.
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) return;

            // Draw Unused Overlay
            if (settings.showRedOverlay && RAssetFilter.UnusedAssetsCache.Contains(path))
            {
                var originalColor = GUI.color;
                GUI.color = settings.unusedColor;
                GUI.Box(rect, GUIContent.none);
                GUI.color = originalColor;
            }

            if (!settings.showSize && !settings.showReferenceCount)
                return;

            var entry = GetLabelEntry(path, settings);
            DrawLabel(rect, entry.label, entry.highlight, settings);
        }

        private static LabelEntry GetLabelEntry(string path, RAssetFilterSettings settings)
        {
            // Label text depends only on cached scan data and the two display toggles.
            if (m_labelCacheGeneration != RAssetFilter.CacheGeneration ||
                m_labelCacheShowSize != settings.showSize ||
                m_labelCacheShowReferenceCount != settings.showReferenceCount)
            {
                LabelCache.Clear();
                m_labelCacheGeneration = RAssetFilter.CacheGeneration;
                m_labelCacheShowSize = settings.showSize;
                m_labelCacheShowReferenceCount = settings.showReferenceCount;
            }

            if (LabelCache.TryGetValue(path, out var cached))
                return cached;

            var entry = AssetDatabase.IsValidFolder(path)
                ? BuildFolderEntry(path, settings)
                : BuildAssetEntry(path, settings);
            LabelCache[path] = entry;
            return entry;
        }

        private static LabelEntry BuildAssetEntry(string path, RAssetFilterSettings settings)
        {
            var entry = new LabelEntry();
            LabelBuilder.Length = 0;

            if (settings.showSize)
            {
                long size = RAssetFilter.GetAssetSize(path);
                if (size > 0)
                    LabelBuilder.Append(EditorUtility.FormatBytes(size));
            }

            if (settings.showReferenceCount && RAssetFilter.HasReferenceData)
            {
                if (LabelBuilder.Length > 0)
                    LabelBuilder.Append(" · ");

                int referenceCount = RAssetFilter.GetReferenceCount(path);
                LabelBuilder.Append(referenceCount).Append(" refs");
                entry.highlight = referenceCount == 0;
            }

            entry.label = LabelBuilder.Length > 0 ? LabelBuilder.ToString() : string.Empty;
            return entry;
        }

        private static LabelEntry BuildFolderEntry(string path, RAssetFilterSettings settings)
        {
            var entry = new LabelEntry();
            if (!RAssetFilter.FolderStatsCache.TryGetValue(path, out var stats))
                return entry;

            // Folder labels keep reporting unused-file stats, never summed child references.
            if (settings.showSize)
                entry.label = $"{stats.unusedFilesCount} ({EditorUtility.FormatBytes(stats.unusedSize)})";
            else if (settings.showReferenceCount)
                entry.label = $"{stats.unusedFilesCount} unused";

            entry.highlight = !string.IsNullOrEmpty(entry.label);
            return entry;
        }

        private static void DrawLabel(Rect rect, string label, bool highlight, RAssetFilterSettings settings)
        {
            if (string.IsNullOrEmpty(label))
                return;

            EnsureStyles(settings);

            if (rect.height > 20) // Grid View: centered at bottom
            {
                var gridRect = new Rect(rect.x, rect.y + rect.height - 15, rect.width, 15);
                GUI.Label(gridRect, label, highlight ? m_gridHighlightStyle : m_gridStyle);
                return;
            }

            // List View: right aligned
            var style = EditorStyles.miniLabel;
            MeasureContent.text = label;
            var labelSize = style.CalcSize(MeasureContent);
            var listRect = new Rect(rect.x + rect.width - labelSize.x - 2, rect.y, labelSize.x, rect.height);
            GUI.Label(listRect, label, highlight ? m_listHighlightStyle : style);
        }

        private static void EnsureStyles(RAssetFilterSettings settings)
        {
            // EditorStyles are only valid during GUI calls, so build the styles lazily and reuse them.
            if (m_gridStyle != null && m_styleUnusedColor == settings.unusedColor &&
                m_gridStyle.font == EditorStyles.miniLabel.font)
                return;

            var baseStyle = EditorStyles.miniLabel;
            m_gridStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            m_gridHighlightStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = settings.unusedColor }
            };
            m_listHighlightStyle = new GUIStyle(baseStyle)
            {
                normal = { textColor = settings.unusedColor }
            };
            m_styleUnusedColor = settings.unusedColor;
        }
    }
}
