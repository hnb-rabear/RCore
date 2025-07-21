/**
 * Author HNB-RaBear - 2017
 **/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RCore
{
#region Delegates

	public delegate void VoidDelegate();

	public delegate void IntDelegate(int value);

	public delegate void BoolDelegate(bool value);

	public delegate void FloatDelegate(float value);

	public delegate bool ConditionalDelegate();

	public delegate bool ConditionalDelegate<in T>(T pComponent) where T : Component;

#endregion

	/// <summary>
	/// A large utility class containing various helper methods for randomization,
	/// string manipulation, Unity-specific tasks, and more.
	/// </summary>
	public static class RUtil
	{
#region Randomization

		/// <summary>
		/// Return a random index from an array of chances. Total of chances does not need to be 100.
		/// </summary>
		public static int GetRandomIndexOfChances(float[] chances)
		{
			float totalRatios = 0;
			for (int i = 0; i < chances.Length; i++)
				totalRatios += chances[i];

			float random = Random.Range(0, totalRatios);
			float temp2 = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				if (chances[i] <= 0) continue;
				temp2 += chances[i];
				if (temp2 > random) return i;
			}
			return 0;
		}

		/// <summary>
		/// Return a random index from an array of chances. Total of chances does not need to be 100.
		/// </summary>
		public static int GetRandomIndexOfChances(int[] chances)
		{
			int totalRatios = 0;
			for (int i = 0; i < chances.Length; i++)
				totalRatios += chances[i];

			int random = Random.Range(0, totalRatios + 1);
			int temp2 = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				if (chances[i] <= 0) continue;
				temp2 += chances[i];
				if (temp2 > random) return i;
			}
			return 0;
		}

		/// <summary>
		/// Return a random index from a list of chances. Total of chances does not need to be 100.
		/// </summary>
		public static int GetRandomIndexOfChances(List<float> chances)
		{
			float totalRatios = 0;
			for (int i = 0; i < chances.Count; i++)
				totalRatios += chances[i];

			float random = Random.Range(0, totalRatios);
			float temp2 = 0;
			for (int i = 0; i < chances.Count; i++)
			{
				if (chances[i] <= 0) continue;
				temp2 += chances[i];
				if (temp2 > random) return i;
			}
			return 0;
		}

		/// <summary>
		/// Return a random index from a list of chances. Total of chances does not need to be 100.
		/// </summary>
		public static int GetRandomIndexOfChances(List<int> chances)
		{
			int totalRatios = 0;
			for (int i = 0; i < chances.Count; i++)
				totalRatios += chances[i];

			int random = Random.Range(0, totalRatios + 1);
			int temp2 = 0;
			for (int i = 0; i < chances.Count; i++)
			{
				if (chances[i] <= 0) continue;
				temp2 += chances[i];
				if (temp2 > random) return i;
			}
			return 0;
		}

		/// <summary>
		/// Return a random index from a list of chances with a pre-calculated total.
		/// </summary>
		public static int GetRandomIndexOfChances(List<int> chances, int totalRatios)
		{
			int random = Random.Range(0, totalRatios);
			int temp2 = 0;
			for (int i = 0; i < chances.Count; i++)
			{
				if (chances[i] <= 0) continue;
				temp2 += chances[i];
				if (temp2 > random) return i;
			}
			return 0;
		}

		/// <summary>
		/// Gets a random integer value from an enum type.
		/// </summary>
		public static int GetRandomEnum(Type type)
		{
			var values = Enum.GetValues(type);
			return (int)values.GetValue(Random.Range(0, values.Length));
		}

		/// <summary>
		/// Generates a list of random positions within a circle, ensuring a minimum distance between them.
		/// </summary>
		public static List<Vector2> GetRandomPositions(int count, float radius, float minDistance, int safeLoops = 100)
		{
			var positions = new List<Vector2>();
			for (int i = 0; i < count; i++)
			{
				int attempts = 0;
				bool validPosition;
				Vector2 newPos;
				do
				{
					newPos = Random.insideUnitCircle * radius;
					validPosition = true;
					foreach (var pos in positions)
					{
						if (Vector2.Distance(newPos, pos) < minDistance)
						{
							validPosition = false;
							break;
						}
					}

					attempts++;
					if (attempts > safeLoops)
					{
#if UNITY_EDITOR
						Debug.LogWarning("Max attempts reached, some points may not be placed.");
#endif
						break;
					}
				} while (!validPosition);

				if (validPosition)
					positions.Add(newPos);
			}
			// Fill remaining positions if safe loop limit was reached
			while (positions.Count < count)
			{
				positions.Add(Random.insideUnitCircle * radius);
			}
			return positions;
		}

		/// <summary>
		/// Returns a random country code from a predefined list.
		/// </summary>
		public static string GetRandomCountryCode()
		{
			string[] countryCodes =
			{
				"US", "GB", "CA", "AU", "NZ", "IE", "PH", "ZA", "NG", "IN",
				"RU", "BY", "BG", "UA", "RS", "MK", "KZ", "KG", "MN",
				"CN", "HK", "TW", "SG", "JP", "KR",
				"SA", "AE", "EG", "IQ", "SY", "JO", "LB", "OM", "QA", "KW", "DZ", "LY",
				"VN", "TH"
			};
			return countryCodes[Random.Range(0, countryCodes.Length)];
		}

		/// <summary>
		/// Generates a random string of a specified length.
		/// </summary>
		public static string GetRandomString(int pLength)
		{
			const string chars = "abcdefghijklmnopqrstuvwxyz";
			var stringChars = new char[pLength];
			var random = new System.Random();
			for (int i = 0; i < stringChars.Length; i++)
				stringChars[i] = chars[random.Next(chars.Length)];
			return new String(stringChars);
		}

#endregion

#region String Manipulation

		/// <summary>
		/// Separates the numeric and non-numeric parts of a string.
		/// </summary>
		public static void SeparateStringAndNum(string pStr, out string pNumberPart, out string pStringPart)
		{
			var regexObj = new Regex(@"[-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?", RegexOptions.IgnorePatternWhitespace);
			pNumberPart = regexObj.Match(pStr).ToString();
			pStringPart = pStr.Replace(pNumberPart, "");
		}

		/// <summary>
		/// Converts a PascalCase or camelCase string to lower_underscore_case.
		/// </summary>
		public static string ToLowerUnderscore(string input)
		{
			if (string.IsNullOrEmpty(input))
				return string.Empty;

			var result = new StringBuilder(input.Length * 2);
			char prevChar = input[0];
			result.Append(char.ToLower(prevChar));
			for (int i = 1; i < input.Length; i++)
			{
				char currentChar = input[i];
				if (char.IsUpper(currentChar) || currentChar == ' ' || currentChar == '-')
					if (prevChar != ' ' && prevChar != '-')
						result.Append('_');
				result.Append(char.ToLower(currentChar));
				prevChar = currentChar;
			}
			return result.ToString().Replace("__", "_");
		}

		/// <summary>
		/// Joins non-empty strings with a separator.
		/// </summary>
		public static string JoinString(string separator, params string[] strs)
		{
			return string.Join(separator, strs.Where(s => !string.IsNullOrEmpty(s)).ToArray());
		}

		/// <summary>
		/// Reverses the characters in a StringBuilder in-place.
		/// </summary>
		public static void Reverse(StringBuilder sb)
		{
			int end = sb.Length - 1;
			int start = 0;
			while (end - start > 0)
			{
				(sb[end], sb[start]) = (sb[start], sb[end]);
				start++;
				end--;
			}
		}

		/// <summary>
		/// Gets the first N words from a text.
		/// </summary>
		public static string GetFirstWords(string text, int length)
		{
			string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			int wordCount = Math.Min(words.Length, length);
			return string.Join(" ", words, 0, wordCount);
		}

		/// <summary>
		/// Gets the first line of a text.
		/// </summary>
		public static string GetFirstLine(string text)
		{
			int index = text.IndexOfAny(new[] { '\n', '\r' });
			return index >= 0 ? text.Substring(0, index) : text;
		}

		/// <summary>
		/// Gets the first sentence of a text.
		/// </summary>
		public static string GetFirstSentence(string text)
		{
			int index = text.IndexOfAny(new[] { '.', '?', '!', '\n', '\r' });
			return index >= 0 ? text.Substring(0, index + 1) : text;
		}

		/// <summary>
		/// Computes a stable, non-cryptographic hash code for a string.
		/// </summary>
		public static int GetStableHashCode(string str)
		{
			unchecked
			{
				int hash1 = 5381;
				int hash2 = hash1;
				for (int i = 0; i < str.Length; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i == str.Length - 1) break;
					hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}
				return hash1 + (hash2 * 1566083941);
			}
		}

#endregion

#region Unity & Engine

		/// <summary>
		/// Manually triggers garbage collection and logs memory usage in the editor.
		/// </summary>
		public static void CollectGC()
		{
#if UNITY_EDITOR
			string log = $"BEFORE\n{LogMemoryUsages(false)}";
#endif

			GC.Collect();

#if UNITY_EDITOR
			log += $"\nAFTER\n{LogMemoryUsages(false)}";
			Debug.Log(log);
#endif
		}

		/// <summary>
		/// Returns a formatted string with current Unity memory usage statistics.
		/// </summary>
		public static string LogMemoryUsages(bool printLog = true)
		{
			string str =
				$"\nTotal Reserved memory by Unity [GetTotalReservedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576}mb"
				+ $"\n - Allocated memory by Unity [GetTotalAllocatedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576}mb"
				+ $"\n - Reserved but not allocated [GetTotalUnusedReservedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong() / 1048576}mb"
				+ $"\n - Mono Used Size [GetMonoUsedSizeLong]: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576}mb"
				+ $"\n - Mono Heap Size [GetMonoHeapSizeLong]: {UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / 1048576}mb";
			if (printLog)
				Debug.Log(str);
			return str;
		}

		/// <summary>
		/// Logs the current screen resolution, safe area, and calculated offsets.
		/// </summary>
		public static void LogScreenSafeArea()
		{
			var sWidth = Screen.currentResolution.width;
			var sHeight = Screen.currentResolution.height;
			var safeArea = Screen.safeArea;
			var oWidthTop = (sWidth - safeArea.width - safeArea.x) / 2f;
			var oHeightTop = (sHeight - safeArea.height - safeArea.y) / 2f;
			var oWidthBot = -safeArea.x / 2f;
			var oHeightBot = -safeArea.y / 2f;
			Debug.Log(
				$"Screen size: (width:{sWidth}, height:{sHeight})\nSafe area: {safeArea}\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");
		}

		/// <summary>
		/// Converts a version string (e.g., "1.2.3") into a comparable integer.
		/// </summary>
		public static int GetVersionInt(string version)
		{
			if (!char.IsDigit(version.Last()))
				version = version.Remove(version.Length - 1);

			string[] split = version.Split('.');
			int versionInt = 0;
			for (int i = 0; i < split.Length; i++)
				versionInt += int.Parse(split[i]) * (int)Mathf.Pow(100, split.Length - i - 1);
			return versionInt;
		}

		/// <summary>
		/// Compares two version strings. Returns a value indicating their relationship.
		/// </summary>
		public static int CompareVersion(string curVersion, string newVersion, int maxSubVersionLength = 2)
		{
			if (!char.IsDigit(curVersion.Last()))
				curVersion = curVersion.Remove(curVersion.Length - 1);
			if (!char.IsDigit(newVersion.Last()))
				newVersion = newVersion.Remove(newVersion.Length - 1);

			string[] version1s = curVersion.Split('.');
			string[] version2s = newVersion.Split('.');
			int maxIndex = Mathf.Min(version1s.Length, version2s.Length);
			int p1 = 0;
			int p2 = 0;
			int maxSubVersion = (int)Mathf.Pow(10, maxSubVersionLength);

			for (int i = 0; i < maxIndex; i++)
			{
				p1 += int.Parse(version1s[i]) * (int)Mathf.Pow(maxSubVersion, maxIndex - i - 1);
				p2 += int.Parse(version2s[i]) * (int)Mathf.Pow(maxSubVersion, maxIndex - i - 1);
			}
			return (p1 - p2) % maxSubVersion;
		}

		/// <summary>
		/// Gets the Android application's version code. Returns 0 on other platforms or if an error occurs.
		/// </summary>
		public static int GetVersionCode()
		{
#if UNITY_ANDROID
			try
			{
				using (var contextCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
				using (var context = contextCls.GetStatic<AndroidJavaObject>("currentActivity"))
				using (var packageMngr = context.Call<AndroidJavaObject>("getPackageManager"))
				{
					string packageName = context.Call<string>("getPackageName");
					using (var packageInfo = packageMngr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0))
					{
						return packageInfo.Get<int>("versionCode");
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to get Android version code: " + ex.Message);
				return 0;
			}
#else
			return 0;
#endif
		}

		/// <summary>
		/// Adjusts the pixelsPerUnitMultiplier for all child Images to maintain a perfect aspect ratio based on width.
		/// </summary>
		public static void PerfectRatioImagesByWidth(params GameObject[] gameObjects)
		{
			foreach (var g in gameObjects)
			{
				var images = g.GetComponentsInChildren<UnityEngine.UI.Image>(true);
				foreach (var image in images)
					PerfectRatioImageByWidth(image);
			}
		}

		/// <summary>
		/// Adjusts the pixelsPerUnitMultiplier for an Image to maintain a perfect aspect ratio based on width.
		/// </summary>
		public static void PerfectRatioImageByWidth(UnityEngine.UI.Image image)
		{
			if (image != null && image.sprite != null && image.type == UnityEngine.UI.Image.Type.Sliced)
			{
				var nativeSize = image.sprite.NativeSize();
				var rectSize = image.rectTransform.sizeDelta;
				image.pixelsPerUnitMultiplier = (rectSize.x > 0 && rectSize.x < nativeSize.x) ? (nativeSize.x / rectSize.x) : 1;
				Debug.Log($"Perfect ratio {image.name}");
			}
		}

		/// <summary>
		/// Adjusts the pixelsPerUnitMultiplier for all child Images to maintain a perfect aspect ratio based on height.
		/// </summary>
		public static void PerfectRatioImagesByHeight(params GameObject[] gameObjects)
		{
			foreach (var g in gameObjects)
			{
				var images = g.GetComponentsInChildren<UnityEngine.UI.Image>(true);
				foreach (var image in images)
					PerfectRatioImageByHeight(image);
			}
		}

		/// <summary>
		/// Adjusts the pixelsPerUnitMultiplier for an Image to maintain a perfect aspect ratio based on height.
		/// </summary>
		public static void PerfectRatioImageByHeight(UnityEngine.UI.Image image)
		{
			if (image != null && image.sprite != null && image.type == UnityEngine.UI.Image.Type.Sliced)
			{
				var nativeSize = image.sprite.NativeSize();
				var rectSize = image.rectTransform.sizeDelta;
				image.pixelsPerUnitMultiplier = (rectSize.y > 0 && rectSize.y < nativeSize.y) ? (nativeSize.y / rectSize.y) : 1;
				Debug.Log($"Perfect ratio {image.name}");
			}
		}

#endregion

#region Math

		/// <summary>
		/// Creates a smooth curve through an array of points using Bezier interpolation.
		/// </summary>
		public static Vector3[] MakeSmoothCurve(Vector3[] arrayToCurve, float smoothness)
		{
			if (smoothness < 1.0f) smoothness = 1.0f;

			int pointsLength = arrayToCurve.Length;
			int curvedLength = pointsLength * Mathf.RoundToInt(smoothness) - 1;
			var curvedPoints = new List<Vector3>(curvedLength);

			for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
			{
				float t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);
				var points = new List<Vector3>(arrayToCurve);

				for (int j = pointsLength - 1; j > 0; j--)
					for (int i = 0; i < j; i++)
						points[i] = (1 - t) * points[i] + t * points[i + 1];

				curvedPoints.Add(points[0]);
			}
			return curvedPoints.ToArray();
		}

#endregion

#region System & IO

		/// <summary>
		/// Gets the MAC address of the first operational network interface.
		/// </summary>
		public static string GetMacAddress()
		{
			return NetworkInterface.GetAllNetworkInterfaces()
				.FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up)?
				.GetPhysicalAddress().ToString();
		}

		/// <summary>
		/// Performs a deep copy of a serializable object.
		/// </summary>
		public static T Clone<T>(T source)
		{
			if (!typeof(T).IsSerializable)
				throw new ArgumentException("The type must be serializable.", nameof(source));

			if (ReferenceEquals(source, null))
				return default;

			IFormatter formatter = new BinaryFormatter();
			using (Stream stream = new MemoryStream())
			{
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(stream);
			}
		}

		/// <summary>
		/// Loads a text asset from the Resources folder.
		/// </summary>
		public static string LoadResourceTextAsset(string pPath)
		{
			var textAsset = Resources.Load<TextAsset>(pPath);
			if (textAsset != null)
			{
				string content = textAsset.text;
				Resources.UnloadAsset(textAsset);
				return content;
			}
			Debug.LogError($"File {pPath} not found");
			return "";
		}

		/// <summary>
		/// Loads a text file from Resources and optionally decrypts it.
		/// </summary>
		public static string LoadTextFile(string pPath, IEncryption pEncryption)
		{
			var textAsset = Resources.Load<TextAsset>(pPath);
			if (textAsset != null)
			{
				string content = pEncryption != null ? pEncryption.Decrypt(textAsset.text) : textAsset.text;
				Resources.UnloadAsset(textAsset);
				return content;
			}
			Debug.LogError($"File {pPath} not found");
			return "";
		}

		/// <summary>
		/// Loads a text file from Resources and optionally decrypts it using a default encryption handler.
		/// </summary>
		public static string LoadTextFile(string pPath, bool pEncrypt = false)
		{
			return LoadTextFile(pPath, pEncrypt ? Encryption.Singleton : null);
		}

#endregion

#region Collections

		/// <summary>
		/// Converts a dictionary to a string representation.
		/// </summary>
		public static string DictToString(IDictionary<string, object> d)
		{
			return $"{{ {d.Select(kv => $"({kv.Key}, {kv.Value})").Aggregate("", (current, next) => $"{current}{next}, ")}}}";
		}

#endregion

#region Localization

		/// <summary>
		/// Gets a two-letter ISO country code from a SystemLanguage.
		/// </summary>
		public static string GetCountryCode(SystemLanguage language)
		{
			switch (language)
			{
				case SystemLanguage.Afrikaans: return "ZA";
				case SystemLanguage.Arabic: return "SA";
				case SystemLanguage.Basque: return "ES";
				case SystemLanguage.Belarusian: return "BY";
				case SystemLanguage.Bulgarian: return "BG";
				case SystemLanguage.Catalan: return "ES";
				case SystemLanguage.Chinese: return "CN";
				case SystemLanguage.Czech: return "CZ";
				case SystemLanguage.Danish: return "DK";
				case SystemLanguage.Dutch: return "NL";
				case SystemLanguage.English: return "US";
				case SystemLanguage.Estonian: return "EE";
				case SystemLanguage.Faroese: return "FO";
				case SystemLanguage.Finnish: return "FI";
				case SystemLanguage.French: return "FR";
				case SystemLanguage.German: return "DE";
				case SystemLanguage.Greek: return "GR";
				case SystemLanguage.Hebrew: return "IL";
				case SystemLanguage.Icelandic: return "IS";
				case SystemLanguage.Indonesian: return "ID";
				case SystemLanguage.Italian: return "IT";
				case SystemLanguage.Japanese: return "JP";
				case SystemLanguage.Korean: return "KR";
				case SystemLanguage.Latvian: return "LV";
				case SystemLanguage.Lithuanian: return "LT";
				case SystemLanguage.Norwegian: return "NO";
				case SystemLanguage.Polish: return "PL";
				case SystemLanguage.Portuguese: return "PT";
				case SystemLanguage.Romanian: return "RO";
				case SystemLanguage.Russian: return "RU";
				case SystemLanguage.SerboCroatian: return "RS"; // Serbia as most common
				case SystemLanguage.Slovak: return "SK";
				case SystemLanguage.Slovenian: return "SI";
				case SystemLanguage.Spanish: return "ES";
				case SystemLanguage.Swedish: return "SE";
				case SystemLanguage.Thai: return "TH";
				case SystemLanguage.Turkish: return "TR";
				case SystemLanguage.Ukrainian: return "UA";
				case SystemLanguage.Vietnamese: return "VN";
				case SystemLanguage.ChineseSimplified: return "CN";
				case SystemLanguage.ChineseTraditional: return "TW"; // Taiwan as most common
				case SystemLanguage.Hindi: return "IN";
				case SystemLanguage.Unknown:
				default:
					return "US"; // Fallback
			}
		}

#endregion
	}

	/// <summary>
	/// Contains extension methods for various types, including collections, strings, and Unity objects.
	/// </summary>
	public static class RUtilExtension
	{
#region Action Extensions

		/// <summary>
		/// Safely invokes an Action.
		/// </summary>
		public static void Raise(this Action pAction)
		{
			pAction?.Invoke();
		}

		/// <summary>
		/// Safely invokes an Action with one parameter.
		/// </summary>
		public static void Raise<T>(this Action<T> pAction, T pParam)
		{
			pAction?.Invoke(pParam);
		}

#endregion

#region String Extensions

		/// <summary>
		/// Converts a string to Sentence case.
		/// </summary>
		public static string ToSentenceCase(this string pString)
		{
			if (string.IsNullOrEmpty(pString)) return pString;
			return Regex.Replace(pString.ToLower(), @"(^|\.\s+)([a-z])", m => m.Value.ToUpper());
		}

		/// <summary>
		/// Capitalizes the first letter of each word in a string.
		/// </summary>
		public static string ToCapitalizeEachWord(this string pString)
		{
			if (string.IsNullOrEmpty(pString)) return pString;
			return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pString.ToLower());
		}

		/// <summary>
		/// Converts the first character of a string to lowercase.
		/// </summary>
		public static string ToLowerCaseFirstChar(this string pString)
		{
			if (string.IsNullOrEmpty(pString)) return pString;
			return char.ToLowerInvariant(pString[0]) + pString.Substring(1);
		}

		/// <summary>
		/// Removes all characters except letters, numbers, underscores, and periods.
		/// </summary>
		public static string RemoveSpecialCharacters(this string str, string replace = "")
		{
			return Regex.Replace(str, "[^a-zA-Z0-9_.]+", replace, RegexOptions.Compiled);
		}

#endregion

#region Collection Extensions

#region Array Extensions

		public static List<T> ToList<T>(this T[] pArray)
		{
			var list = new List<T>(pArray.Length);
			list.AddRange(pArray);
			return list;
		}

		public static bool Contain(this int[] pArray, int pObj)
		{
			for (int i = 0; i < pArray.Length; i++)
				if (pArray[i] == pObj)
					return true;
			return false;
		}

		public static bool Exists<T>(this T[] pArray, Predicate<T> match)
		{
			for (int i = 0; i < pArray.Length; i++)
				if (match(pArray[i]))
					return true;
			return false;
		}

		public static T Find<T>(this T[] pArray, Predicate<T> match)
		{
			for (int i = 0; i < pArray.Length; i++)
				if (match(pArray[i]))
					return pArray[i];
			return default;
		}

		public static T FindLast<T>(this T[] pArray, Predicate<T> match)
		{
			for (int i = pArray.Length - 1; i >= 0; i--)
				if (match(pArray[i]))
					return pArray[i];
			return default;
		}

		public static T[] FindAll<T>(this T[] pArray, Predicate<T> match)
		{
			var list = new List<T>();
			for (int i = 0; i < pArray.Length; i++)
				if (match(pArray[i]))
					list.Add(pArray[i]);
			return list.ToArray();
		}

		public static int Count<T>(this T[] pArray, Predicate<T> match)
		{
			int count = 0;
			for (int i = 0; i < pArray.Length; i++)
				if (match(pArray[i]))
					count++;
			return count;
		}

		public static void Add<T>(this T[] pArray, T pObj, out T[] output)
		{
			output = new T[pArray.Length + 1];
			Array.Copy(pArray, output, pArray.Length);
			output[pArray.Length] = pObj;
		}

		public static void AddRange<T>(this T[] pArray1, T[] pArray2, out T[] output)
		{
			output = new T[pArray1.Length + pArray2.Length];
			Array.Copy(pArray1, 0, output, 0, pArray1.Length);
			Array.Copy(pArray2, 0, output, pArray1.Length, pArray2.Length);
		}

		public static void Remove<T>(this T[] pArray, T pObj, out T[] output)
		{
			output = pArray.Where(e => !EqualityComparer<T>.Default.Equals(e, pObj)).ToArray();
		}

		public static void RemoveAll<T>(this T[] pArray, Predicate<T> match, out T[] output)
		{
			output = pArray.Where(e => !match(e)).ToArray();
		}

		public static void RemoveAt<T>(this T[] pArray, int pIndex, out T[] output)
		{
			if (pIndex < 0 || pIndex >= pArray.Length)
			{
				output = (T[])pArray.Clone();
				return;
			}
			output = new T[pArray.Length - 1];
			int j = 0;
			for (int i = 0; i < pArray.Length; i++)
				if (i != pIndex)
					output[j++] = pArray[i];
		}

		public static int IndexOf<T>(this T[] pArray, T pObj) => Array.IndexOf(pArray, pObj);

		public static int FindIndex<T>(this T[] pArray, Predicate<T> match) => Array.FindIndex(pArray, match);

#endregion

#region List Extensions

		public static List<T> RemoveNull<T>(this List<T> pList) where T : class
		{
			pList.RemoveAll(item => item == null);
			return pList;
		}

		public static void RemoveDuplicated<T>(this List<T> list)
		{
			list.RemoveAll(new HashSet<T>().Add);
		}

		public static void RemoveDuplicatedKey<TKey, TValue>(this List<SerializableKeyValue<TKey, TValue>> list)
		{
			var seenKeys = new HashSet<TKey>();
			list.RemoveAll(item => !seenKeys.Add(item.k));
		}

		public static void RemoveDuplicatedValue<TKey, TValue>(this List<SerializableKeyValue<TKey, TValue>> list)
		{
			var seenValues = new HashSet<TValue>();
			list.RemoveAll(item => !seenValues.Add(item.v));
		}

		public static void Swap<T>(this List<T> list, int index1, int index2)
		{
			if (list == null || list.Count <= 1 || index1 == index2 || index1 < 0 || index1 >= list.Count || index2 < 0 || index2 >= list.Count)
				return;

			(list[index1], list[index2]) = (list[index2], list[index1]);
		}

		public static bool ContainsKey<T>(this List<KeyValuePair<int, T>> pList, int pKey)
		{
			return pList.Exists(kvp => kvp.Key == pKey);
		}

		public static void Remove<T>(this List<KeyValuePair<int, T>> pList, int pKey)
		{
			pList.RemoveAll(kvp => kvp.Key == pKey);
		}

#endregion

#region Dictionary Extensions

		public static void Add(this Dictionary<int, int> pSource, Dictionary<int, int> pDict)
		{
			foreach (var (key, value) in pDict)
			{
				pSource.TryGetValue(key, out int currentValue);
				pSource[key] = currentValue + value;
			}
		}

		public static void MinusAndRemove(this Dictionary<int, int> pSource, Dictionary<int, int> pDict)
		{
			var removedKeys = new List<int>();
			foreach (var (key, value) in pDict)
			{
				if (pSource.TryGetValue(key, out int currentValue))
				{
					pSource[key] = currentValue - value;
					if (pSource[key] <= 0)
						removedKeys.Add(key);
				}
			}
			foreach (var key in removedKeys)
				pSource.Remove(key);
		}

		public static void AddOrSet<K, V>(this Dictionary<K, V> pSource, K pKey, V pVal)
		{
			pSource[pKey] = pVal;
		}

		public static void Remove<K, V>(this Dictionary<K, V> pSource, List<K> pKeys)
		{
			foreach (var key in pKeys)
				pSource.Remove(key);
		}

		public static TKey RandomKey<TKey, TValue>(this Dictionary<TKey, TValue> pSource)
		{
			var keys = pSource.Keys.ToArray();
			return keys[Random.Range(0, keys.Length)];
		}

		public static List<KeyValuePair<TKey, TValue>> ToKeyValuePairs<TKey, TValue>(this Dictionary<TKey, TValue> pSource)
		{
			return pSource.ToList();
		}

		public static TKey RandomKeyHasLowestValue<TKey, TValue>(this Dictionary<TKey, TValue> pSource) where TValue : IComparable<TValue>
		{
			if (pSource.Count == 0) return default;
			var minValue = pSource.Values.Min();
			var keys = pSource.Where(kvp => kvp.Value.CompareTo(minValue) == 0).Select(kvp => kvp.Key).ToList();
			return keys[Random.Range(0, keys.Count)];
		}

		public static TKey RandomKey<TKey, TValue>(this Dictionary<TKey, TValue> pSource, TValue pMinVal) where TValue : IComparable<TValue>
		{
			var keys = pSource.Where(item => item.Value.CompareTo(pMinVal) < 0).Select(item => item.Key).ToList();
			return keys.Count > 0 ? keys[Random.Range(0, keys.Count)] : default;
		}

#endregion

#endregion

#region Unity Object Extensions

#region Vector Extensions

		/// <summary>
		/// Checks if a 2D position is within the bounds.
		/// </summary>
		public static bool InsideBounds(this Vector2 pPosition, Bounds pBounds)
		{
			return pPosition.x >= pBounds.min.x && pPosition.x <= pBounds.max.x && pPosition.y >= pBounds.min.y && pPosition.y <= pBounds.max.y;
		}

		/// <summary>
		/// Converts a Vector3Int to a Vector3.
		/// </summary>
		public static Vector3 ToVector3(this Vector3Int pVal)
		{
			return new Vector3(pVal.x, pVal.y, pVal.z);
		}

		/// <summary>
		/// Converts a Vector2Int to a Vector2.
		/// </summary>
		public static Vector2 ToVector2(this Vector2Int pVal)
		{
			return new Vector2(pVal.x, pVal.y);
		}

		/// <summary>
		/// Sorts a list of Vector3 positions by their distance to a reference position.
		/// </summary>
		public static void SortByDistanceToPosition(this List<Vector3> pPositions, Vector3 pPosition)
		{
			pPositions.Sort((a, b) =>
				Vector3.Distance(pPosition, a).CompareTo(Vector3.Distance(pPosition, b))
			);
		}

#endregion

#region Transform Extensions

		/// <summary>
		/// Checks if a Transform is a descendant of a specified parent.
		/// </summary>
		public static bool IsChildOfParent(this Transform pItem, Transform pParent)
		{
			var current = pItem;
			while (current != null)
			{
				if (current.parent == pParent)
					return true;
				current = current.parent;
			}
			return false;
		}

#endregion

#endregion
	}

	/// <summary>
	/// Contains extension methods for shuffling collections.
	/// </summary>
	public static class RandomExtension
	{
		private static readonly System.Random _rng = new System.Random();

		/// <summary>
		/// Shuffles an array in-place using the Fisher-Yates algorithm.
		/// </summary>
		public static void Shuffle<T>(this T[] array)
		{
			int n = array.Length;
			while (n > 1)
			{
				n--;
				int k = _rng.Next(n + 1);
				(array[k], array[n]) = (array[n], array[k]);
			}
		}

		/// <summary>
		/// Shuffles a list in-place using the Fisher-Yates algorithm.
		/// </summary>
		public static void Shuffle<T>(this List<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = _rng.Next(n + 1);
				(list[k], list[n]) = (list[n], list[k]);
			}
		}
	}
}