using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor.AssetCleaner
{
    public static class RAssetCleaner
    {
        public struct FolderStats
        {
            public int unusedFilesCount;
            public long unusedSize;
        }

        public static HashSet<string> UnusedAssetsCache = new HashSet<string>();
        public static Dictionary<string, FolderStats> FolderStatsCache = new Dictionary<string, FolderStats>();
        public static Dictionary<string, List<string>> ReferenceCache = new Dictionary<string, List<string>>();

        
        public static List<string> FindUnusedAssets(List<string> ignorePaths)
        {
            var unusedAssets = new List<string>();
            var allAssets = AssetDatabase.GetAllAssetPaths();
            
            // Build Cache first
            BuildCache();
            
            var projectAssets = allAssets.Where(p => p.StartsWith("Assets/")).ToArray();
            int index = 0;
            int total = projectAssets.Length;

            // 2. Identify Unused Assets
            foreach (var assetPath in projectAssets)


            {
                if (index % 100 == 0)
                    EditorUtility.DisplayProgressBar("Scanning Assets", $"Checking usage: {Path.GetFileName(assetPath)}", (float)index / total);
                index++;
                
                if (AssetDatabase.IsValidFolder(assetPath)) continue;
                if (IsIgnored(assetPath, ignorePaths)) continue;

                bool isRoot = IsRootAsset(assetPath);
                
                // If it is NOT in the ReferenceCache, it means no one depends on it.
                if (!ReferenceCache.ContainsKey(assetPath))
                {
                   if (!isRoot)
                        unusedAssets.Add(assetPath);
                }
            }

            EditorUtility.ClearProgressBar();
            
            UnusedAssetsCache = new HashSet<string>(unusedAssets);
            CalculateFolderStats(unusedAssets);
            
            return unusedAssets;
        }

        public static void BuildCache()
        {
            ReferenceCache.Clear();
            var allAssets = AssetDatabase.GetAllAssetPaths();
            // Include ProjectSettings to find references like App Icon, Splash Screen, etc.
            var projectAssets = allAssets.Where(p => p.StartsWith("Assets/") || p.StartsWith("ProjectSettings/")).ToArray();
            int index = 0;
            int total = projectAssets.Length;

            foreach (var assetPath in projectAssets)
            {
                if (index % 50 == 0) 
                    EditorUtility.DisplayProgressBar("Building Cache", $"Analyzing dependencies: {Path.GetFileName(assetPath)}", (float)index / total);
                index++;

                // Skip directories
                if (AssetDatabase.IsValidFolder(assetPath)) continue;

                var dependencies = AssetDatabase.GetDependencies(assetPath, false);
                foreach (var dep in dependencies)
                {
                    if (dep == assetPath) continue; // Self dependency

                    if (!ReferenceCache.ContainsKey(dep))
                    {
                        ReferenceCache[dep] = new List<string>();
                    }
                    if (!ReferenceCache[dep].Contains(assetPath))
                        ReferenceCache[dep].Add(assetPath);
                }
            }
            EditorUtility.ClearProgressBar();
        }




        private static void CalculateFolderStats(List<string> unusedAssets)
        {
            FolderStatsCache.Clear();
            foreach (var path in unusedAssets)
            {
                long size = GetAssetSize(path);
                string dir = Path.GetDirectoryName(path).Replace("\\", "/");
                
                while (!string.IsNullOrEmpty(dir) && dir.StartsWith("Assets"))
                {
                    if (!FolderStatsCache.ContainsKey(dir))
                    {
                        FolderStatsCache[dir] = new FolderStats();
                    }
                    
                    var stats = FolderStatsCache[dir];
                    stats.unusedFilesCount++;
                    stats.unusedSize += size;
                    FolderStatsCache[dir] = stats; // Struct copy back
                    
                    dir = Path.GetDirectoryName(dir).Replace("\\", "/");
                    if (dir == "Assets") break; // Stop at root
                }
            }
        }

        private static bool IsRootAsset(string path)
        {
             // Scenes, Resources, StreamingAssets, EditorDefaultResources are roots
             if (path.EndsWith(".unity")) return true;
             if (path.Contains("/Resources/")) return true;
             if (path.Contains("/Editor/")) return true; // Editor scripts often not referenced but used
             if (path.Contains("/StreamingAssets/")) return true;
             if (path.Contains("/Plugins/")) return true; // Plugins are often entry points
             return false;
        }

        private static bool IsIgnored(string path, List<string> ignorePaths)
        {
            if (ignorePaths == null) return false;
            foreach (var ignore in ignorePaths)
            {
                if (path.Contains(ignore)) return true;
            }
            return false;
        }

        public static List<string> FindReferences(string targetPath, bool useCache = true)
        {
            if (useCache)
            {
                if (ReferenceCache.ContainsKey(targetPath))
                    return ReferenceCache[targetPath];
                // If not in cache, either we are out of sync OR it really is not used.
                // But we don't know if we are out of sync unless we check everything.
                // For "Fast" mode, we trust the cache.
                // If ReferenceCache is empty, assume we haven't built it.
                if (ReferenceCache.Count == 0)
                {
                     // Warn? or fallback? Let's fallback.
                     Debug.LogWarning("Reference Cache is empty. Falling back to slow search.");
                     useCache = false;
                }
                else
                {
                    return new List<string>();
                }
            }
            
            if (!useCache)
            {
                var references = new List<string>();
                var allAssets = AssetDatabase.GetAllAssetPaths();
                
                int index = 0;
                int total = allAssets.Length;

                foreach (var assetPath in allAssets)
                {
                    if (index % 100 == 0) // Update progress every 100 items
                        EditorUtility.DisplayProgressBar("Finding References", $"Checking: {Path.GetFileName(assetPath)}", (float)index / total);
                    index++;

                    if (assetPath == targetPath) continue;
                    if (AssetDatabase.IsValidFolder(assetPath)) continue;

                    var deps = AssetDatabase.GetDependencies(assetPath, false);
                    if (deps.Contains(targetPath))
                    {
                        references.Add(assetPath);
                    }
                }
                EditorUtility.ClearProgressBar();
                return references;
            }
            return new List<string>();
        }
        
        public static long GetAssetSize(string path)
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            return 0;
        }
        
        public static string GetTotalSizeFormatted(List<string> paths)
        {
            long total = 0;
            foreach (var p in paths) total += GetAssetSize(p);
            return EditorUtility.FormatBytes(total);
        }

        public static List<string> FindReferencesByGuid(string guid)
        {
            var references = new System.Collections.Concurrent.ConcurrentBag<string>();
            var allAssets = AssetDatabase.GetAllAssetPaths();
            
            // Files that can store text/guid references
            // Use configurable extensions from settings
            var textExtensions = new HashSet<string>(RAssetCleanerSettings.Instance.deepSearchExtensions.Select(e => e.ToLower()));

            // Filter on Main Thread
            var candidatePaths = new List<string>();
            foreach (var path in allAssets)
            {
                if (AssetDatabase.IsValidFolder(path)) continue;
                string ext = Path.GetExtension(path).ToLower();
                if (textExtensions.Contains(ext))
                {
                    candidatePaths.Add(path);
                }
            }
            
            // Parallel Scan
            EditorUtility.DisplayProgressBar("Deep Search", $"Scanning {candidatePaths.Count} text assets...", 0f);
            
            try
            {
                // Use Parallel.ForEach to utilize all CPU cores
                // Note: File.ReadAllText is thread-safe for reading.
                System.Threading.Tasks.Parallel.ForEach(candidatePaths, path => 
                {
                    try
                    {
                        string content = File.ReadAllText(path);
                        if (content.Contains(guid))
                        {
                            references.Add(path);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore read errors (e.g. file locked)
                    }
                });
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            return references.ToList();
        }
    }
}
