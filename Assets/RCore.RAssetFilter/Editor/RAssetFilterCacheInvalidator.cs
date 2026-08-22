using System.Collections.Generic;
using UnityEditor;

namespace RCore.RAssetFilter.Editor
{
    public class RAssetFilterCacheInvalidator : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (RAssetFilter.IsEditingAssetsInternally)
                return;

            // A move only renames cache keys and any referencer path string that points at the old
            // location; every dependency edge is unchanged. Diff the paths in place so a move never
            // discards a valid scan. RAssetFilter falls back to a full invalidate itself when the
            // batch is not a clean one-to-one rename (out-of-scope, or duplicates). When the batch is
            // malformed here (null, or mismatched-length arrays) no rename can be read from it, so
            // nothing valid can be updated incrementally and the cache must not survive unchanged
            // against paths it can no longer account for.
            if ((movedAssets == null) != (movedFromAssetPaths == null))
            {
                RAssetFilter.InvalidateCache();
                return;
            }

            if (movedAssets != null && movedFromAssetPaths != null)
            {
                if (movedAssets.Length != movedFromAssetPaths.Length)
                {
                    RAssetFilter.InvalidateCache();
                    return;
                }

                var movedTo = new List<string>();
                var movedFrom = new List<string>();
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    string toPath = movedAssets[i];
                    string fromPath = movedFromAssetPaths[i];
                    if (string.IsNullOrEmpty(toPath) || string.IsNullOrEmpty(fromPath))
                        continue;
                    bool toRelevant = toPath.StartsWith("Assets/") || toPath.StartsWith("ProjectSettings/");
                    bool fromRelevant = fromPath.StartsWith("Assets/") || fromPath.StartsWith("ProjectSettings/");
                    if (!toRelevant && !fromRelevant)
                        continue;
                    if (!toRelevant || !fromRelevant)
                    {
                        // A move crossing in/out of scope (e.g. from Packages/) cannot be diffed as a rename.
                        RAssetFilter.InvalidateCache();
                        return;
                    }
                    movedTo.Add(toPath);
                    movedFrom.Add(fromPath);
                }

                if (movedTo.Count > 0)
                    RAssetFilter.UpdateCacheForMovedAssets(movedFrom, movedTo);
            }

            // A delete only removes one asset's forward/reverse edges; every other cached edge is
            // unchanged. Forget just the deleted paths instead of discarding the whole scan, mirroring
            // the incremental move/import handling above. RAssetFilter.ForgetAssets no-ops when the
            // cache isn't ready, so there's nothing to fall back to invalidating in that case.
            if (HasRelevantPath(deletedAssets))
            {
                RAssetFilter.ForgetAssets(FilterRelevant(deletedAssets).ToArray());
                return;
            }

            if (HasRelevantPath(importedAssets))
            {
                RAssetFilter.UpdateCacheForChangedAssets(FilterRelevant(importedAssets));
            }
        }

        private static List<string> FilterRelevant(string[] paths)
        {
            var result = new List<string>();
            if (paths == null)
                return result;

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;
                if (!path.StartsWith("Assets/") && !path.StartsWith("ProjectSettings/"))
                    continue;
                result.Add(path);
            }

            return result;
        }

        private static bool HasRelevantPath(string[] paths)
        {
            if (paths == null)
                return false;

            foreach (var path in paths)
            {
                if (!string.IsNullOrEmpty(path) &&
                    (path.StartsWith("Assets/") || path.StartsWith("ProjectSettings/")))
                    return true;
            }

            return false;
        }
    }
}
