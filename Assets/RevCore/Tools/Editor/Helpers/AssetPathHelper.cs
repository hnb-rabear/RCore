using System.IO;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    internal static class AssetPathHelper
    {
        public static string ToUnityPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath))
                return string.Empty;

            return "Assets" + normalized.Substring(dataPath.Length);
        }

        public static string EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return path;
        }
    }
}
