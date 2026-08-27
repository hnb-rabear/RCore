/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Checks for SheetX package updates by fetching the remote package.json from GitHub
	/// and comparing its version against the locally installed version.
	/// </summary>
	internal static class SheetXUpdateChecker
	{
		private const string REMOTE_PACKAGE_JSON_URL =
			"https://raw.githubusercontent.com/hnb-rabear/RCore/main/Assets/RCore.SheetX/package.json";
		private const string GIT_URL =
			"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore.SheetX";

		private const string REMOTE_VERSION_CACHE_KEY = "SheetXUpdateChecker_RemoteVersion";
		private const string LAST_CHECK_TIME_KEY = "SheetXUpdateChecker_LastCheckTime";

		private static UnityWebRequest s_pendingRequest;
		private static bool s_isChecking;

		[InitializeOnLoadMethod]
		private static void RegisterCleanup()
		{
			AssemblyReloadEvents.beforeAssemblyReload -= CancelPendingRequest;
			AssemblyReloadEvents.beforeAssemblyReload += CancelPendingRequest;
		}

		private static void CancelPendingRequest()
		{
			UnityWebRequest request = s_pendingRequest;
			s_pendingRequest = null;
			s_isChecking = false;
			if (request == null) return;

			request.Abort();
			request.Dispose();
		}

		/// <summary>Whether a remote check is currently in progress.</summary>
		internal static bool IsChecking => s_isChecking;

		/// <summary>Cached remote version from last successful check, or null.</summary>
		internal static string CachedRemoteVersion
		{
			get
			{
				string v = EditorPrefs.GetString(REMOTE_VERSION_CACHE_KEY, "");
				return string.IsNullOrEmpty(v) ? null : v;
			}
		}

		/// <summary>Git URL used to install/update SheetX via UPM.</summary>
		internal static string GitUrl => GIT_URL;

		/// <summary>Returns package metadata for the assembly currently running SheetX.</summary>
		internal static PackageInfo GetInstalledPackageInfo()
		{
			return PackageInfo.FindForAssembly(typeof(SheetXUpdateChecker).Assembly);
		}

		/// <summary>Returns true when UPM can refresh this Git or registry installation.</summary>
		internal static bool CanUpdate(PackageInfo packageInfo)
		{
			return packageInfo != null && CanUpdate(packageInfo.source);
		}

		internal static bool CanUpdate(PackageSource source)
		{
			return source == PackageSource.Git || source == PackageSource.Registry;
		}

		/// <summary>Returns the installed SheetX version from UPM metadata or the source package.json.</summary>
		internal static string GetInstalledVersion()
		{
			PackageInfo packageInfo = GetInstalledPackageInfo();
			if (!string.IsNullOrEmpty(packageInfo?.version))
				return packageInfo.version;

			string packageJsonPath = Path.Combine(Application.dataPath, "RCore.SheetX", "package.json");
			return File.Exists(packageJsonPath) ? ReadVersionFromPackageJson(packageJsonPath) : null;
		}

		/// <summary>
		/// Returns true when the cached remote version is newer than the installed version.
		/// </summary>
		internal static bool HasUpdate(string installedVersion, string remoteVersion)
		{
			return !string.IsNullOrEmpty(installedVersion)
				&& !string.IsNullOrEmpty(remoteVersion)
				&& installedVersion != remoteVersion
				&& CompareVersions(remoteVersion, installedVersion) > 0;
		}

		/// <summary>
		/// Fetches the remote package.json version from GitHub and caches it in EditorPrefs.
		/// </summary>
		/// <param name="onCompleted">
		/// Called with true if a new remote version was fetched, false on failure or no change.
		/// </param>
		internal static void CheckRemoteVersion(Action<bool> onCompleted = null)
		{
			if (s_isChecking)
			{
				onCompleted?.Invoke(false);
				return;
			}

			s_isChecking = true;

			UnityWebRequest request = null;
			UnityWebRequestAsyncOperation op;
			try
			{
				request = UnityWebRequest.Get(REMOTE_PACKAGE_JSON_URL);
				op = request.SendWebRequest();
			}
			catch (Exception e)
			{
				request?.Dispose();
				Debug.LogWarning($"[SheetXUpdateChecker] Failed to start remote version request: {e.Message}");
				s_isChecking = false;
				onCompleted?.Invoke(false);
				return;
			}

			s_pendingRequest = request;
			op.completed += _ =>
			{
				if (s_pendingRequest != request)
					return;

				s_pendingRequest = null;
				s_isChecking = false;
				bool success = false;

				try
				{
					if (request.result == UnityWebRequest.Result.Success)
					{
						var wrapper = JsonUtility.FromJson<PackageJsonVersion>(request.downloadHandler.text);
						if (!string.IsNullOrEmpty(wrapper?.version))
						{
							EditorPrefs.SetString(REMOTE_VERSION_CACHE_KEY, wrapper.version);
							EditorPrefs.SetString(LAST_CHECK_TIME_KEY, DateTime.UtcNow.ToString("o"));
							success = true;
						}
					}
					else
					{
						Debug.LogWarning($"[SheetXUpdateChecker] Remote version check failed: {request.error}");
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[SheetXUpdateChecker] Failed to parse remote version: {e.Message}");
				}
				finally
				{
					request.Dispose();
					onCompleted?.Invoke(success);
				}
			};
		}

		/// <summary>Returns a human-readable elapsed time since the last successful check.</summary>
		internal static string GetLastCheckTimeDisplay()
		{
			string timeStr = EditorPrefs.GetString(LAST_CHECK_TIME_KEY, "");
			if (string.IsNullOrEmpty(timeStr)) return "Never";

			try
			{
				var lastCheck = DateTime.Parse(timeStr).ToLocalTime();
				var elapsed = DateTime.Now - lastCheck;
				if (elapsed.TotalMinutes < 1) return "Just now";
				if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
				if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
				return $"{(int)elapsed.TotalDays}d ago";
			}
			catch
			{
				return "Unknown";
			}
		}

		internal static int CompareVersions(string a, string b)
		{
			if (!TryParseSemVersion(a, out int[] aCore, out string[] aPrerelease)
				|| !TryParseSemVersion(b, out int[] bCore, out string[] bPrerelease))
				return string.Compare(a, b, StringComparison.Ordinal);

			for (int i = 0; i < aCore.Length; i++)
			{
				int coreComparison = aCore[i].CompareTo(bCore[i]);
				if (coreComparison != 0)
					return coreComparison;
			}

			if (aPrerelease.Length == 0 || bPrerelease.Length == 0)
				return aPrerelease.Length == bPrerelease.Length ? 0 : aPrerelease.Length == 0 ? 1 : -1;

			int count = Math.Min(aPrerelease.Length, bPrerelease.Length);
			for (int i = 0; i < count; i++)
			{
				int prereleaseComparison = ComparePrereleaseIdentifier(aPrerelease[i], bPrerelease[i]);
				if (prereleaseComparison != 0)
					return prereleaseComparison;
			}
			return aPrerelease.Length.CompareTo(bPrerelease.Length);
		}

		private static bool TryParseSemVersion(string value, out int[] core, out string[] prerelease)
		{
			core = null;
			prerelease = Array.Empty<string>();
			if (string.IsNullOrEmpty(value))
				return false;

			int buildIndex = value.IndexOf('+');
			string withoutBuild = buildIndex >= 0 ? value.Substring(0, buildIndex) : value;
			int prereleaseIndex = withoutBuild.IndexOf('-');
			string coreText = prereleaseIndex >= 0 ? withoutBuild.Substring(0, prereleaseIndex) : withoutBuild;
			string prereleaseText = prereleaseIndex >= 0 ? withoutBuild.Substring(prereleaseIndex + 1) : null;

			string[] parts = coreText.Split('.');
			if (parts.Length != 3)
				return false;

			core = new int[3];
			for (int i = 0; i < parts.Length; i++)
			{
				if (!TryParseNumericIdentifier(parts[i], out core[i]))
					return false;
			}

			if (prereleaseText == null)
				return true;
			if (prereleaseText.Length == 0)
				return false;

			prerelease = prereleaseText.Split('.');
			return Array.TrueForAll(prerelease, part => part.Length > 0);
		}

		private static int ComparePrereleaseIdentifier(string a, string b)
		{
			bool aNumeric = IsNumeric(a);
			bool bNumeric = IsNumeric(b);
			if (aNumeric != bNumeric)
				return aNumeric ? -1 : 1;
			if (!aNumeric)
				return string.Compare(a, b, StringComparison.Ordinal);

			string aTrimmed = a.TrimStart('0');
			string bTrimmed = b.TrimStart('0');
			if (aTrimmed.Length == 0) aTrimmed = "0";
			if (bTrimmed.Length == 0) bTrimmed = "0";
			int lengthComparison = aTrimmed.Length.CompareTo(bTrimmed.Length);
			return lengthComparison != 0
				? lengthComparison
				: string.Compare(aTrimmed, bTrimmed, StringComparison.Ordinal);
		}

		private static bool IsNumeric(string value)
		{
			for (int i = 0; i < value.Length; i++)
			{
				if (value[i] < '0' || value[i] > '9')
					return false;
			}
			return value.Length > 0;
		}

		private static bool TryParseNumericIdentifier(string value, out int number)
		{
			number = 0;
			if (!IsNumeric(value))
				return false;
			for (int i = 0; i < value.Length; i++)
			{
				int digit = value[i] - '0';
				if (number > (int.MaxValue - digit) / 10)
					return false;
				number = number * 10 + digit;
			}
			return true;
		}

		private static string ReadVersionFromPackageJson(string path)
		{
			try
			{
				string json = File.ReadAllText(path);
				var wrapper = JsonUtility.FromJson<PackageJsonVersion>(json);
				return wrapper?.version;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[SheetXUpdateChecker] Failed to read {path}: {e.Message}");
				return null;
			}
		}

		[Serializable]
		private class PackageJsonVersion
		{
#pragma warning disable CS0649 // JsonUtility assigns serialized fields.
			public string version;
#pragma warning restore CS0649
		}
	}
}
