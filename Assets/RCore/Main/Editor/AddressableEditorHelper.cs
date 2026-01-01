#if ADDRESSABLES
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Helper class for managing Addressable assets in the Unity Editor.
	/// </summary>
	public static class AddressableEditorHelper
	{
        /// <summary>
        /// Removes asset bundles from the list that have an empty asset GUID.
        /// </summary>
        public static void RemoveEmptyAssetBundles<M>(List<AssetBundleWithIntKey<M>> list) where M : UnityEngine.Object
        {
            for (int i = 0; i < list.Count; i++)
            {
                var assetBundle = list[i];
                string guiId = assetBundle.reference.AssetGUID;
                if (string.IsNullOrEmpty(guiId))
                {
                    list.RemoveAt(i);
                    i--;
                }
            }
        }
		/// <summary>
		/// Creates or updates an addressable asset entry for the specified source object.
		/// </summary>
		public static AddressableAssetEntry CreateAssetEntry<T>(T source, string groupName, string label) where T : Object
		{
			var entry = CreateAssetEntry(source, groupName, false);
			if (source != null)
				source.AddAddressableAssetLabel(label,out _);

			return entry;
		}
		/// <summary>
		/// Creates or updates an addressable asset entry for the specified source object, with options for group and address naming.
		/// </summary>
		public static AddressableAssetEntry CreateAssetEntry<T>(T source, string groupName, bool fileNameAsAddress, string prefix = null) where T : Object
		{
			if (source == null || string.IsNullOrEmpty(groupName) || !AssetDatabase.Contains(source))
				return null;

			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
			var sourcePath = AssetDatabase.GetAssetPath(source);
			var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
			var group = !GroupExists(groupName) ? CreateGroup(groupName) : GetGroup(groupName);
			var entry = addressableSettings.CreateOrMoveEntry(sourceGuid, group);
			if (fileNameAsAddress)
			{
				string[] split = sourcePath.Split("/");
				entry.address = split[split.Length - 1];
			}
			else entry.address = sourcePath;
			if (prefix != null)
				entry.address = prefix + entry.address;

			addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

			return entry;
		}

		/// <summary>
		/// Creates or updates an addressable asset entry in the default group.
		/// </summary>
		public static AddressableAssetEntry CreateAssetEntry<T>(T source, bool fileNameAsAddress, string prefix = null) where T : Object
		{
			if (source == null || !AssetDatabase.Contains(source))
				return null;

			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
			var sourcePath = AssetDatabase.GetAssetPath(source);
			var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
			var entry = addressableSettings.CreateOrMoveEntry(sourceGuid, addressableSettings.DefaultGroup);
			if (fileNameAsAddress)
			{
				string[] split = sourcePath.Split("/");
				entry.address = split[split.Length - 1];
			}
			else entry.address = sourcePath;
			if (prefix != null)
				entry.address = prefix + entry.address;

			addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

			return entry;
		}
		/// <summary>
		/// Finds an existing addressable asset group by name.
		/// </summary>
		public static AddressableAssetGroup GetGroup(string groupName)
		{
			if (string.IsNullOrEmpty(groupName))
				return null;

			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
			return addressableSettings.FindGroup(groupName);
		}
		/// <summary>
		/// Creates a new addressable asset group.
		/// </summary>
		public static AddressableAssetGroup CreateGroup(string groupName)
		{
			if (string.IsNullOrEmpty(groupName))
				return null;

			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
			var group = addressableSettings.CreateGroup(groupName, false, false, false, addressableSettings.DefaultGroup.Schemas);

			addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, group, true);

			return group;
		}
		/// <summary>
		/// Checks if an addressable asset group exists.
		/// </summary>
		public static bool GroupExists(string groupName)
		{
			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
			return addressableSettings.FindGroup(groupName) != null;
		}
		/// <summary>
		/// Sets the address and optional labels for an addressable asset identified by its GUID.
		/// </summary>
		public static bool SetAddressableAssetAddress(string guid, string pAddress, params string[] pLabels)
		{
			var assetEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
			if (assetEntry != null)
			{
				assetEntry.address = pAddress;
				if (pLabels != null && pLabels.Length > 0)
				{
					assetEntry.labels.Clear();
					foreach (var label in pLabels)
					{
						if (!string.IsNullOrEmpty(label))
						{
							AddressableAssetSettingsDefaultObject.Settings.AddLabel(label);
							assetEntry.labels.Add(label);
						}
					}
				}
				return true;
			}
			return false;
		}
        
        /// <summary>
        /// Checks if the object is included in the Addressables build (i.e., has an entry and is not in the "Excluded Content" group).
        /// </summary>
        public static bool IncludedInBuild(Object obj)
        {
            if (obj == null)
                return false;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            var assetEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid, true);
            if (assetEntry == null)
                return false;
            var excludedGroup = AddressableAssetSettingsDefaultObject.Settings.FindGroup("Excluded Content");
            if (excludedGroup != null && excludedGroup.GetAssetEntry(guid, true) != null)
                return false;
            return true;
        }
        public static bool IncludedInBuild(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return false;
            var assetEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid, true);
            if (assetEntry == null)
                return false;
            var excludedGroup = AddressableAssetSettingsDefaultObject.Settings.FindGroup("Excluded Content");
            if (excludedGroup != null && excludedGroup.GetAssetEntry(guid, true) != null)
                return false;
            return true;
        }
    }

	/// <summary>
	/// Extension methods for interacting with Addressable assets directly from Unity Objects.
	/// </summary>
	public static class AddressableEditorExtension
	{
		public static void RemoveAddressableAssetLabel(this Object source, string label, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return;

			var entry = source.GetAddressableAssetEntry(out guid);
			if (entry != null && entry.labels.Contains(label))
			{
				entry.labels.Remove(label);

				AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.LabelRemoved, entry, true);
			}
		}
		public static void AddAddressableAssetLabel(this Object source, string label, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return;

			var entry = source.GetAddressableAssetEntry(out guid);
			if (entry != null && entry.labels.Add(label))
				AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.LabelAdded, entry, true);
		}
		public static void SetAddressableAssetAddress(this Object source, string address, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return;

			var entry = source.GetAddressableAssetEntry(out guid);
			if (entry != null)
			{
				entry.address = address;
			}
		}
		public static void SetAddressableAssetGroup(this Object source, string groupName, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return;

			var group = !AddressableEditorHelper.GroupExists(groupName) ? AddressableEditorHelper.CreateGroup(groupName) : AddressableEditorHelper.GetGroup(groupName);
			source.SetAddressableAssetGroup(group, out guid);
		}
		public static void SetAddressableAssetGroup(this Object source, AddressableAssetGroup group, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return;

			var entry = source.GetAddressableAssetEntry(out _);
			if (entry != null && !source.IsInAddressableAssetGroup(group.Name, out guid))
			{
				entry.parentGroup = group;
			}
		}
		public static HashSet<string> GetAddressableAssetLabels(this Object source, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return null;

			var entry = source.GetAddressableAssetEntry(out guid);
			return entry?.labels;
		}
		public static string GetAddressableAssetPath(this Object source, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return string.Empty;

			var entry = source.GetAddressableAssetEntry(out guid);
			return entry != null ? entry.address : string.Empty;
		}
		public static bool IsInAddressableAssetGroup(this Object source, string groupName, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return false;

			var group = source.GetCurrentAddressableAssetGroup(out guid);
			return group != null && group.Name == groupName;
		}
		public static AddressableAssetGroup GetCurrentAddressableAssetGroup(this Object source, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return null;

			var entry = source.GetAddressableAssetEntry(out guid);
			return entry?.parentGroup;
		}
		public static AddressableAssetEntry GetAddressableAssetEntry(this Object source, out string guid)
		{
			guid = "";
			if (source == null || !AssetDatabase.Contains(source))
				return null;

			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
			var path = AssetDatabase.GetAssetPath(source);
			guid = AssetDatabase.AssetPathToGUID(path);
			return addressableSettings.FindAssetEntry(guid);
		}
		public static bool SetAddressableAssetAddress(Object source, string pAddress, params string[] pLabels)
		{
			var assetEntry = source.GetAddressableAssetEntry(out _);
			if (assetEntry != null)
			{
				assetEntry.address = pAddress;
				if (pLabels != null && pLabels.Length > 0)
				{
					assetEntry.labels.Clear();
					foreach (var label in pLabels)
					{
						if (!string.IsNullOrEmpty(label))
						{
							AddressableAssetSettingsDefaultObject.Settings.AddLabel(label);
							assetEntry.labels.Add(label);
						}
					}
				}
				return true;
			}
			return false;
		}
	}
}
#endif