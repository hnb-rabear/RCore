using System.IO;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
#if RCORE_DEV
	public static class SamplesHelper
	{
		private const string PACKAGE_PATH = "Assets/RCore/Main";
		private const string HIDDEN_SAMPLES = "Samples~";
		private const string VISIBLE_SAMPLES = "Samples";

		[MenuItem("RCore/Packages Manager/Expose Samples")]
		public static void ExposeSamples()
		{
			ToggleSamples(true);
		}

		[MenuItem("RCore/Packages Manager/Hide Samples")]
		public static void HideSamples()
		{
			ToggleSamples(false);
		}

		private static void ToggleSamples(bool expose)
		{
			string rootPath = Path.Combine(Application.dataPath, "RCore/Main").Replace("\\", "/");
			string hiddenPath = Path.Combine(rootPath, HIDDEN_SAMPLES).Replace("\\", "/");
			string visiblePath = Path.Combine(rootPath, VISIBLE_SAMPLES).Replace("\\", "/");

			if (expose)
			{
				if (Directory.Exists(hiddenPath))
				{
					Directory.Move(hiddenPath, visiblePath);
					if (File.Exists(hiddenPath + ".meta"))
						File.Move(hiddenPath + ".meta", visiblePath + ".meta");
					AssetDatabase.Refresh();
					Debug.Log($"Samples exposed at: {visiblePath}");
				}
				else if (Directory.Exists(visiblePath))
				{
					Debug.Log("Samples are already exposed.");
				}
				else
				{
					Debug.LogError($"Could not find Samples folder at {hiddenPath}");
				}
			}
			else
			{
				if (Directory.Exists(visiblePath))
				{
					Directory.Move(visiblePath, hiddenPath);
					if (File.Exists(visiblePath + ".meta"))
						File.Move(visiblePath + ".meta", hiddenPath + ".meta");
					AssetDatabase.Refresh();
					Debug.Log($"Samples hidden at: {hiddenPath}");
				}
				else if (Directory.Exists(hiddenPath))
				{
					Debug.Log("Samples are already hidden.");
				}
				else
				{
					Debug.LogError($"Could not find Samples folder at {visiblePath}");
				}
			}
		}

		[MenuItem("RCore/Packages Manager/Expose Samples", true)]
		private static bool ExposeSamplesValidate()
		{
			string rootPath = Path.Combine(Application.dataPath, "RCore/Main");
			string hiddenPath = Path.Combine(rootPath, HIDDEN_SAMPLES);
			return Directory.Exists(hiddenPath);
		}

		[MenuItem("RCore/Packages Manager/Hide Samples", true)]
		private static bool HideSamplesValidate()
		{
			string rootPath = Path.Combine(Application.dataPath, "RCore/Main");
			string visiblePath = Path.Combine(rootPath, VISIBLE_SAMPLES);
			return Directory.Exists(visiblePath);
		}
	}
#endif
}
