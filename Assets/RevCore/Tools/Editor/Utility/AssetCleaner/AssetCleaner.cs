using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace RevCore.Tools.Editor
{
    public static class AssetCleaner
    {
        public struct FolderStats
        {
            public int unusedFilesCount;
            public long unusedSize;
        }

        public static HashSet<string> UnusedAssetsCache = new();
        public static Dictionary<string, FolderStats> FolderStatsCache = new();
        public static Dictionary<string, List<string>> ReferenceCache = new();
        public static Dictionary<string, long> SizeCache = new();

        public static List<string> FindUnusedAssets(List<string> ignorePaths)
        {
            var unusedAssets = new List<string>();
            BuildCache();

            string[] projectAssets = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/")).ToArray();
            int total = projectAssets.Length;

            for (int i = 0; i < total; i++)
            {
                string assetPath = projectAssets[i];
                if (i % 100 == 0)
                    EditorUtility.DisplayProgressBar("Scanning Assets", $"Checking: {Path.GetFileName(assetPath)}", (float)i / total);

                if (AssetDatabase.IsValidFolder(assetPath)) continue;
                if (IsIgnored(assetPath, ignorePaths)) continue;
                if (IsRootAsset(assetPath)) continue;
                if (!ReferenceCache.ContainsKey(assetPath))
                    unusedAssets.Add(assetPath);
            }

            EditorUtility.ClearProgressBar();
            UnusedAssetsCache = new HashSet<string>(unusedAssets);
            CalculateFolderStats(unusedAssets);
            return unusedAssets;
        }

        public static void BuildCache()
        {
            ReferenceCache.Clear();
            SizeCache.Clear();
            string[] projectAssets = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/") || p.StartsWith("ProjectSettings/")).ToArray();
            int total = projectAssets.Length;

            for (int i = 0; i < total; i++)
            {
                string assetPath = projectAssets[i];
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Building Cache", $"Analyzing: {Path.GetFileName(assetPath)}", (float)i / total);

                if (AssetDatabase.IsValidFolder(assetPath)) continue;
                string[] deps = AssetDatabase.GetDependencies(assetPath, false);
                foreach (string dep in deps)
                {
                    if (dep == assetPath) continue;
                    if (!ReferenceCache.ContainsKey(dep))
                        ReferenceCache[dep] = new List<string>();
                    if (!ReferenceCache[dep].Contains(assetPath))
                        ReferenceCache[dep].Add(assetPath);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public static List<string> FindReferences(string targetPath)
        {
            if (ReferenceCache.TryGetValue(targetPath, out List<string> refs))
                return refs;
            return new List<string>();
        }

        public static long GetAssetSize(string path)
        {
            if (SizeCache.TryGetValue(path, out long cached))
                return cached;
            long size = File.Exists(path) ? new FileInfo(path).Length : 0;
            SizeCache[path] = size;
            return size;
        }

        public static string GetTotalSizeFormatted(List<string> paths)
        {
            long total = 0;
            foreach (string p in paths) total += GetAssetSize(p);
            return EditorUtility.FormatBytes(total);
        }

        private static void CalculateFolderStats(List<string> unusedAssets)
        {
            FolderStatsCache.Clear();
            foreach (string path in unusedAssets)
            {
                long size = GetAssetSize(path);
                string dir = Path.GetDirectoryName(path)?.Replace("\\", "/");
                while (!string.IsNullOrEmpty(dir) && dir.StartsWith("Assets"))
                {
                    if (!FolderStatsCache.ContainsKey(dir))
                        FolderStatsCache[dir] = new FolderStats();
                    var stats = FolderStatsCache[dir];
                    stats.unusedFilesCount++;
                    stats.unusedSize += size;
                    FolderStatsCache[dir] = stats;
                    dir = Path.GetDirectoryName(dir)?.Replace("\\", "/");
                    if (dir == "Assets") break;
                }
            }
        }

        private static bool IsRootAsset(string path)
        {
            if (path.EndsWith(".unity")) return true;
            if (path.Contains("/Resources/")) return true;
            if (path.Contains("/Editor/")) return true;
            if (path.Contains("/StreamingAssets/")) return true;
            if (path.Contains("/Plugins/")) return true;
            return false;
        }

        private static bool IsIgnored(string path, List<string> ignorePaths)
        {
            if (ignorePaths == null) return false;
            foreach (string ignore in ignorePaths)
                if (path.Contains(ignore)) return true;
            return false;
        }
    }
}
