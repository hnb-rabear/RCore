using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Provides easy access to standard Unity editor icons.
	/// </summary>
	public static class EditorIcon
	{
		public enum Icon
		{
			Refresh,
			Help,
			Settings,
			Menu,
			Error,
			Info,
			Warning,
			VisibilityOn,
			VisibilityOff,
			AssetStore,
			Update,
			Installed,
			Download,
			AddFolder,
			Import,
			PackageManager,
			Linked,
			Unlinked,
			Valid,
			UpArrow,
			GreenLight,
			OrangeLight,
			RedLight,
			PlayButton,
			PauseButton,
			StepButton,
			Folder,
			FolderEmpty,
			FolderFavorite,
			FolderOpened,
			Favorite,
			FrameCapture,
			CollabChanges,
			CollabChangesConflict,
			CollabChangesDeleted,
			CollabConflict,
			CollabCreate,
			CollabDeleted,
			CollabEdit,
			CollabExclude,
			CollabMoved,
			Search,
			AddedLocal,
			AddedRemote,
			CheckOutLocal,
			CheckOutRemote,
			Conflicted,
			DeletedLocal,
			DeletedRemote,
			ToolbarPlus,
			ToolbarMinus,
			SaveAs,
			TestPassed,
			Edit,
			Selected,
			DefaultAsset,
		}

		public static readonly Dictionary<Icon, string> IconDictionary = new Dictionary<Icon, string>
		{
			{ Icon.Refresh, "Refresh@2x" },
			{ Icon.Help, "_Help@2x" },
			{ Icon.Settings, "SettingsIcon@2x" },
			{ Icon.Menu, "_Menu@2x" },
			{ Icon.Error, "console.erroricon@2x" },
			{ Icon.Info, "console.infoicon@2x" },
			{ Icon.Warning, "console.warnicon@2x" },
			{ Icon.VisibilityOn, "animationvisibilitytoggleon@2x" },
			{ Icon.VisibilityOff, "animationvisibilitytoggleoff@2x" },
			{ Icon.AssetStore, "Asset Store@2x" },
			{ Icon.Update, "Update-Available@2x" },
			{ Icon.Installed, "Installed@2x" },
			{ Icon.Download, "Download-Available@2x" },
			{ Icon.AddFolder, "Add-Available@2x" },
			{ Icon.Import, "Import-Available@2x" },
			{ Icon.PackageManager, "Package Manager@2x" },
			{ Icon.Linked, "Linked@2x" },
			{ Icon.Unlinked, "UnLinked@2x" },
			{ Icon.Valid, "Valid@2x" },
			{ Icon.UpArrow, "UpArrow" },
			{ Icon.GreenLight, "greenLight" },
			{ Icon.OrangeLight, "orangeLight" },
			{ Icon.RedLight, "redLight" },
			{ Icon.PlayButton, "PlayButton@2x" },
			{ Icon.PauseButton, "PauseButton@2x" },
			{ Icon.StepButton, "StepButton@2x" },
			{ Icon.Folder, "Folder Icon" },
			{ Icon.FolderEmpty, "FolderEmpty Icon" },
			{ Icon.FolderFavorite, "FolderFavorite Icon" },
			{ Icon.FolderOpened, "FolderOpened Icon" },
			{ Icon.Favorite, "Favorite Icon" },
			{ Icon.FrameCapture, "FrameCapture@2x" },
			{ Icon.CollabChanges, "CollabChanges Icon" },
			{ Icon.CollabChangesConflict, "CollabChangesConflict Icon" },
			{ Icon.CollabChangesDeleted, "CollabChangesDeleted Icon" },
			{ Icon.CollabConflict, "CollabConflict Icon" },
			{ Icon.CollabCreate, "CollabCreate Icon" },
			{ Icon.CollabDeleted, "CollabDeleted Icon" },
			{ Icon.CollabEdit, "CollabEdit Icon" },
			{ Icon.CollabExclude, "CollabExclude Icon" },
			{ Icon.CollabMoved, "CollabMoved Icon" },
			{ Icon.Search, "Search Icon" },
			{ Icon.AddedLocal, "P4_AddedLocal@2x" },
			{ Icon.AddedRemote, "P4_AddedRemote@2x" },
			{ Icon.CheckOutLocal, "P4_CheckOutLocal@2x" },
			{ Icon.CheckOutRemote, "P4_CheckOutRemote@2x" },
			{ Icon.Conflicted, "P4_Conflicted@2x" },
			{ Icon.DeletedLocal, "P4_DeletedLocal@2x" },
			{ Icon.DeletedRemote, "P4_DeletedRemote@2x" },
			{ Icon.ToolbarPlus, "Toolbar Plus@2x" },
			{ Icon.ToolbarMinus, "Toolbar Minus@2x" },
			{ Icon.SaveAs, "SaveAs@2x" },
			{ Icon.TestPassed, "TestPassed" },
			{ Icon.Edit, "editicon.sml" },
			{ Icon.Selected, "FilterSelectedOnly@2x" },
			{ Icon.DefaultAsset, "DefaultAsset Icon" },
		};

		private static Dictionary<Icon, Texture2D> m_Texture2Ds = new Dictionary<Icon, Texture2D>();

		/// <summary>
		/// Retrieves the texture for a specific named icon type, caching it for future use.
		/// </summary>
		public static Texture2D GetIcon(Icon pIcon)
		{
			if (m_Texture2Ds.TryGetValue(pIcon, out var icon1))
				return icon1;
			var iconContent = EditorGUIUtility.IconContent(IconDictionary[pIcon]);
			m_Texture2Ds[pIcon] = (Texture2D)iconContent.image;
			return m_Texture2Ds[pIcon];
		}

		/// <summary>
		/// Retrieves all available editor icons as a list of textures.
		/// </summary>
		public static List<Texture2D> GetAllIcons()
		{
			var icons = new List<Texture2D>();
			foreach (Icon value in Enum.GetValues(typeof(Icon)))
			{
				var icon = GetIcon(value);
				if (icon == null)
					Debug.LogError(value.ToString());
				icons.Add(icon);
			}
			return icons;
		}
	}
}