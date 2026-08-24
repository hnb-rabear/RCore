/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Loads selected collection assets before Play Mode and cancels entry when baking fails.
	/// </summary>
	[InitializeOnLoad]
	internal static class SheetXCollectionPlayModeLoader
	{
		static SheetXCollectionPlayModeLoader()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		/// <summary>
		/// Determines whether current editor transition needs collection loading.
		/// </summary>
		internal static bool ShouldLoadBeforePlay(
			SheetXSettings settings, bool isPlayingOrWillChangePlaymode)
		{
			return settings != null
				&& settings.enableCollections
				&& settings.autoLoadBeforePlay
				&& !isPlayingOrWillChangePlaymode;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.ExitingEditMode)
				return;

			var settings = SheetXSettings.Init();
			if (!ShouldLoadBeforePlay(settings, Application.isPlaying))
				return;
			if (SheetXCollectionBaker.TryLoadData(settings, autoLoadOnly: true, out string error))
				return;

			EditorApplication.isPlaying = false;
			Debug.LogError(error);
		}
	}
}
