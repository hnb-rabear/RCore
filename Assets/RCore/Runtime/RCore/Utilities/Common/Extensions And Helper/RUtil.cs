/**
 * Author RadBear - nbhung71711 @gmail.com - 2017 - 2020
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace RCore.Common
{
	public delegate void VoidDelegate();
	public delegate void IntDelegate(int value);
	public delegate void BoolDelegate(bool value);
	public delegate void FloatDelegate(float value);
	public delegate bool ConditionalDelegate();

	public static class RUtil
	{
		public static void SeparateStringAndNum(string pStr, out string pNumberPart, out string pStringPart)
		{
			pNumberPart = "";
			var regexObj = new Regex(@"[-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?", RegexOptions.IgnorePatternWhitespace);
			var match = regexObj.Match(pStr);
			pNumberPart = match.ToString();

			pStringPart = pStr.Replace(pNumberPart, "");
		}

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


		public static string JoinString(string separator, params string[] strs)
		{
			var list = new List<string>();
			foreach (var str in strs)
			{
				if (!string.IsNullOrEmpty(str))
					list.Add(str);
			}
			return string.Join(separator, list.ToArray());
		}

		public static void Reverse(StringBuilder sb)
		{
			char t;
			int end = sb.Length - 1;
			int start = 0;

			while (end - start > 0)
			{
				t = sb[end];
				sb[end] = sb[start];
				sb[start] = t;
				start++;
				end--;
			}
		}

		public static void CollectGC()
		{
#if UNITY_EDITOR
			string log = "BEFORE\n" + LogMemoryUsages(false);
#endif
			GC.Collect();
#if UNITY_EDITOR
			log += "\nAFTER\n" + LogMemoryUsages(false);
			UnityEngine.Debug.Log(log);
#endif
		}

		public static string LogMemoryUsages(bool printLog = true)
		{
			string str = "" +
				$"\nTotal Reserved memory by Unity [GetTotalReservedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576}mb" +
				$"\n - Allocated memory by Unity [GetTotalAllocatedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576}mb" +
				$"\n - Reserved but not allocated [GetTotalUnusedReservedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong() / 1048576}mb" +
				$"\n - Mono Used Size [GetMonoUsedSizeLong]: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576}mb" +
				$"\n - Mono Heap Size [GetMonoHeapSizeLong]: {UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / 1048576}mb";
			if (printLog)
				UnityEngine.Debug.Log(str);
			return str;
		}

		public static void LogScreenSafeArea()
		{
			var sWidth = Screen.currentResolution.width;
			var sHeight = Screen.currentResolution.height;
			var oWidthTop = (Screen.currentResolution.width - Screen.safeArea.width - Screen.safeArea.x) / 2f;
			var oHeightTop = (Screen.currentResolution.height - Screen.safeArea.height - Screen.safeArea.y) / 2f;
			var oWidthBot = -Screen.safeArea.x / 2f;
			var oHeightBot = -Screen.safeArea.y / 2f;
			Debug.Log($"Screen size: (width:{sWidth}, height:{sHeight})" +
				$"\nSafe area: {Screen.safeArea}" +
				$"\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})" +
				$"\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");
		}

		public static void CreateBackup(string pContent, string pFileName = null)
		{
			pFileName ??= $"{Application.productName}_{DateTime.Now:yyyyMMdd_HHmm}";
			pFileName = pFileName.RemoveSpecialCharacters();
			string folder = Application.persistentDataPath + Path.DirectorySeparatorChar + "Backup" + Path.DirectorySeparatorChar;
#if UNITY_EDITOR
			folder = Application.dataPath + Path.DirectorySeparatorChar + "Backup" + Path.DirectorySeparatorChar;
#endif
			if (!string.IsNullOrEmpty(pContent) && pContent != "{}")
			{
				var directoryPath = Path.GetDirectoryName(folder);
				if (!Directory.Exists(directoryPath))
					Directory.CreateDirectory(directoryPath);

				string path = folder + pFileName + ".json";
				File.WriteAllText(path, pContent);
				Debug.Log($"Created Backup successfully {path}", Color.green);
#if UNITY_EDITOR
				UnityEditor.EditorApplication.delayCall += () =>
				{
					System.Diagnostics.Process.Start(folder);
				};
#endif
			}
		}

		public static string DictToString(IDictionary<string, object> d)
		{
			return "{ " + d.Select(kv => "(" + kv.Key + ", " + kv.Value + ")").Aggregate("", (current, next) => current + next + ", ") + "}";
		}

		public static IEnumerator SendWebRequest(string url, WWWForm form, Action<string> pCallback = null)
		{
			using var w = form == null ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, form);
			yield return w.SendWebRequest();
			while (w.isDone == false)
				yield return null;
			pCallback?.Invoke(w.result == UnityWebRequest.Result.Success ? w.downloadHandler.text : null);
		}

		public static int GetVersionInt(string appVersion)
		{
			int versionInt = 0;
			string[] numbers = appVersion.Split('.');
			for (int i = 0; i < numbers.Length; i++)
				versionInt += int.Parse(numbers[i]) * (int)Mathf.Pow(10, numbers.Length - i);
			return versionInt;
		}

		public static int CompareVersionNames(string currentVersion, string remoteVersion)
		{
			int res = 0;

			string[] oldNumbers = currentVersion.Split('.');
			string[] newNumbers = remoteVersion.Split('.');

			// To avoid IndexOutOfBounds
			int maxIndex = Mathf.Min(oldNumbers.Length, newNumbers.Length);

			for (int i = 0; i < maxIndex; i++)
			{
				int oldVersionPart = int.Parse(oldNumbers[i]);
				int newVersionPart = int.Parse(newNumbers[i]);

				if (oldVersionPart < newVersionPart)
				{
					res = -1;
					break;
				}
				if (oldVersionPart > newVersionPart)
				{
					res = 1;
					break;
				}
			}

			// If versions are the same so far, but they have different length...
			if (res == 0 && oldNumbers.Length != newNumbers.Length)
			{
				res = (oldNumbers.Length > newNumbers.Length) ? 1 : -1;
			}

			return res;
		}

		public static int CompareVersion(string version1, string version2)
		{
			if (!int.TryParse(version1.Last().ToString(), out _))
				version1 = version1.Remove(version1.Length - 1);
			if (!int.TryParse(version2.Last().ToString(), out _))
				version2 = version2.Remove(version2.Length - 1);
			string[] version1s = version1.Split('.');
			string[] version2s = version2.Split('.');
			int maxIndex = Mathf.Min(version1s.Length, version2s.Length);
			int p1 = 0;
			int p2 = 0;
			for (int i = 0; i < maxIndex; i++)
			{
				p1 += int.Parse(version1s[i]) * (int)Mathf.Pow(100, maxIndex - i - 1);
				p2 += int.Parse(version2s[i]) * (int)Mathf.Pow(100, maxIndex - i - 1);
			}

			// 2.10.22 = 2*100^2 + 10*100^1 + 22*100^0 = 20000+1000+22 = 21022
			// 2.10.26 = 2*100^2 + 10*100^1 + 26*100^0 = 20000+1000+26 = 21026
			// -4

			// 2.9.22 = 2*100^2 + 9*100^1 + 22*100^0 = 20000+900+22 = 20922
			// 2.10.26 = 2*100^2 + 10*100^1 + 26*100^0 = 20000+1000+26 = 21026
			// -104

			return (p1 - p2) % 100;
		}

		public static void PerfectRatioImagesByWidth(params GameObject[] gameObjects)
		{
			foreach (var g in gameObjects)
			{
				var images = g.FindComponentsInChildren<UnityEngine.UI.Image>();
				foreach (var image in images)
					PerfectRatioImagesByWidth(image);
			}
		}

		public static void PerfectRatioImagesByWidth(UnityEngine.UI.Image image)
		{
			if (image != null && image.sprite != null && image.type == UnityEngine.UI.Image.Type.Sliced)
			{
				var nativeSize = image.sprite.NativeSize();
				var rectSize = image.rectTransform.sizeDelta;
				if (rectSize.x > 0 && rectSize.x < nativeSize.x)
				{
					var ratio = nativeSize.x * 1f / rectSize.x;
					image.pixelsPerUnitMultiplier = ratio;
				}
				else
					image.pixelsPerUnitMultiplier = 1;

				Debug.Log($"Perfect ratio {image.name}");
			}
		}

		public static void PerfectRatioImagesByHeight(params GameObject[] gameObjects)
		{
			foreach (var g in gameObjects)
			{
				var images = g.FindComponentsInChildren<UnityEngine.UI.Image>();
				foreach (var image in images)
					PerfectRatioImageByHeight(image);
			}
		}

		public static void PerfectRatioImageByHeight(UnityEngine.UI.Image image)
		{
			if (image != null && image.sprite != null && image.type == UnityEngine.UI.Image.Type.Sliced)
			{
				var nativeSize = image.sprite.NativeSize();
				var rectSize = image.rectTransform.sizeDelta;
				if (rectSize.y > 0 && rectSize.y < nativeSize.y)
				{
					var ratio = nativeSize.y * 1f / rectSize.y;
					image.pixelsPerUnitMultiplier = ratio;
				}
				else
					image.pixelsPerUnitMultiplier = 1;

				Debug.Log($"Perfect ratio {image.name}");
			}
		}

		public static Vector3[] MakeSmoothCurve(Vector3[] arrayToCurve, float smoothness)
		{
			if (smoothness < 1.0f) smoothness = 1.0f;

			int pointsLength = arrayToCurve.Length;

			int curvedLength = (pointsLength * Mathf.RoundToInt(smoothness)) - 1;
			var curvedPoints = new List<Vector3>(curvedLength);

			for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
			{
				float t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);

				var points = new List<Vector3>(arrayToCurve);

				for (int j = pointsLength - 1; j > 0; j--)
				{
					for (int i = 0; i < j; i++)
					{
						points[i] = (1 - t) * points[i] + t * points[i + 1];
					}
				}

				curvedPoints.Add(points[0]);
			}
			return (curvedPoints.ToArray());
		}

		public static int GetStableHashCode(string str)
		{
			unchecked
			{
				int hash1 = 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i == str.Length - 1 || str[i + 1] == '\0')
						break;
					hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return hash1 + (hash2 * 1566083941);
			}
		}

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

		public static string GetRandomString(int pLength)
		{
			const string chars = "abcdefghijklmnopqrstuvwxyz";
			var stringChars = new char[pLength];
			var random = new System.Random();
			for (int i = 0; i < stringChars.Length; i++)
				stringChars[i] = chars[random.Next(chars.Length)];
			var finalString = new String(stringChars);
			return finalString;
		}

		public static string GetMacAddress()
		{
			foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
				if (nic.OperationalStatus == OperationalStatus.Up)
					return nic.GetPhysicalAddress().ToString();
			return null;
		}

		public static string GetFirstWords(string text, int length)
		{
			string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			int wordCount = Math.Min(words.Length, length);
			return string.Join(" ", words, 0, wordCount);
		}

		public static string GetFirstLine(string text)
		{
			char[] sentenceEnders = { '\n', '\r' };
			int index = text.IndexOfAny(sentenceEnders);
			return index >= 0 ? text.Substring(0, index) : text;
		}
		
		public static string GetFirstSentence(string text)
		{
			char[] sentenceEnders = { '.', '?', '!', '\n', '\r' };
			int index = text.IndexOfAny(sentenceEnders);
			return index >= 0 ? text.Substring(0, index + 1) : text;
		}
	}

	public static class RUtilExtension
	{
		public static string ToSentenceCase(this string pString)
		{
			// MatchEvaluator delegate defines replacement of sentence starts to uppercase
			var result = Regex.Replace(pString, @"(^|\.\s+)([a-z])", match => match.Value.ToUpper());
			return result;
		}

		public static string ToCapitalizeEachWord(this string pString)
		{
			return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pString);
		}

		public static string ToLowerCaseFirstChar(this string pString)
		{
			if (string.IsNullOrEmpty(pString) || char.IsLower(pString[0]))
				return pString;
			return char.ToLowerInvariant(pString[0]) + pString.Substring(1);
		}

		public static bool InsideBounds(this Vector2 pPosition, Bounds pBounds)
		{
			if (pPosition.x < pBounds.min.x)
				return false;
			if (pPosition.x > pBounds.max.x)
				return false;
			if (pPosition.y < pBounds.min.y)
				return false;
			if (pPosition.y > pBounds.max.y)
				return false;
			return true;
		}

		public static void Raise(this Action pAction)
		{
			pAction?.Invoke();
		}

		public static void Raise(this UnityAction pAction)
		{
			pAction?.Invoke();
		}

		public static void Raise<T>(this Action<T> pAction, T pParam)
		{
			pAction?.Invoke(pParam);
		}

		public static void Raise<T>(this UnityAction<T> pAction, T pParam)
		{
			pAction?.Invoke(pParam);
		}

		public static void Raise<T, M>(this Action<T, M> pAction, T pParam1, M pParam2)
		{
			pAction?.Invoke(pParam1, pParam2);
		}

		public static void Raise<T, M>(this UnityAction<T, M> pAction, T pParam1, M pParam2)
		{
			pAction?.Invoke(pParam1, pParam2);
		}

		public static List<T> ToList<T>(this T[] pArray)
		{
			var list = new List<T>();
			foreach (var a in pArray)
				list.Add(a);
			return list;
		}

		public static bool Contain(this int[] pArray, int pObj)
		{
			for (int i = 0; i < pArray.Length; i++)
				if (pArray[i] == pObj)
					return true;
			return false;
		}

		public static T[] Add<T>(this T[] pArray, T pObj)
		{
			var newArray = new T[pArray.Length + 1];
			for (int i = 0; i < pArray.Length; i++)
				newArray[i] = pArray[i];
			newArray[pArray.Length] = pObj;
			return newArray;
		}

		public static T[] AddRange<T>(this T[] pArray, T[] pObj)
		{
			var newArray = new T[pArray.Length + pObj.Length];

			for (int i = 0; i < pArray.Length; i++)
				newArray[i] = pArray[i];

			for (int i = 0; i < pObj.Length; i++)
				newArray[pArray.Length + i] = pObj[i];

			return newArray;
		}

		public static T[] Remove<T>(this T[] pArray, T pObj)
		{
			int count = 0;
			for (int i = 0; i < pArray.Length; i++)
				if (EqualityComparer<T>.Default.Equals(pArray[i], pObj))
					count++;
			int j = 0;
			var newArray = new T[pArray.Length - count];
			for (int i = 0; i < pArray.Length; i++)
				if (!EqualityComparer<T>.Default.Equals(pArray[i], pObj))
				{
					newArray[j] = pArray[i];
					j++;
				}
			return newArray;
		}

		public static int IndexOf<T>(this T[] pArray, T pObj)
		{
			for (int i = 0; i < pArray.Length; i++)
				if (EqualityComparer<T>.Default.Equals(pArray[i], pObj))
					return i;
			return -1;
		}

		public static T[] RemoveAt<T>(this T[] pArray, int pIndex)
		{
			int j = 0;
			var newArray = new T[pArray.Length - 1];
			for (int i = 0; i < pArray.Length; i++)
				if (i != pIndex)
				{
					newArray[j] = pArray[i];
					j++;
				}
			return newArray;
		}

		public static List<T> RemoveNull<T>(this List<T> pList)
		{
			for (int i = pList.Count - 1; i >= 0; i--)
			{
				if (pList[i].ToString() == "null")
					pList.RemoveAt(i);
			}
			return pList;
		}

		public static List<T> RemoveDuplicate<T>(this List<T> pList) where T : UnityEngine.Object
		{
			var duplicate = new List<int>();
			for (int i = 0; i < pList.Count; i++)
			{
				int count = 0;
				for (int j = pList.Count - 1; j >= 0; j--)
				{
					if (pList[j] == pList[i])
					{
						count++;
						if (count > 1)
							duplicate.Add(j);
					}
				}
			}
			for (int j = pList.Count - 1; j >= 0; j--)
			{
				if (duplicate.Contains(j))
					pList.Remove(pList[j]);
			}

			return pList;
		}

		public static void Add(this Dictionary<int, int> pSource, Dictionary<int, int> pDict)
		{
			foreach (var item in pDict)
			{
				if (pSource.ContainsKey(item.Key))
					pSource[item.Key] += item.Value;
				else
					pSource.Add(item.Key, item.Value);
			}
		}

		public static void MinusAndRemove(this Dictionary<int, int> pSource, Dictionary<int, int> pDict)
		{
			var removedKeys = new List<int>();
			foreach (var item in pDict)
			{
				if (pSource.ContainsKey(item.Key))
				{
					pSource[item.Key] -= item.Value;
					if (pSource[item.Key] <= 0)
						removedKeys.Add(item.Key);
				}
			}
			if (removedKeys.Count > 0)
			{
				foreach (var key in removedKeys)
					pSource.Remove(key);
			}
		}

		public static void AddOrSet<K, V>(this Dictionary<K, V> pSource, K pKey, V pVal)
		{
			if (pSource.ContainsKey(pKey))
				pSource[pKey] = pVal;
			else
				pSource.Add(pKey, pVal);
		}

		public static void Remove<K, V>(this Dictionary<K, V> pSource, List<K> pKeys)
		{
			for (int i = 0; i < pKeys.Count; i++)
				pSource.Remove(pKeys[i]);
		}

		public static T1 RandomKey<T1, T2>(this Dictionary<T1, T2> pSource)
		{
			var keys = new T1[pSource.Count];
			int index = 0;
			foreach (var item in pSource)
			{
				keys[index] = item.Key;
				index++;
			}
			return keys[Random.Range(0, keys.Length)];
		}

		public static List<KeyValuePair<T1, T2>> ToKeyValuePairs<T1, T2>(this Dictionary<T1, T2> pSource)
		{
			var list = new List<KeyValuePair<T1, T2>>();
			foreach (var item in pSource)
				list.Add(new KeyValuePair<T1, T2>(item.Key, item.Value));
			return list;
		}

		public static T1 RandomKeyHasLowestValue<T1, T2>(this Dictionary<T1, T2> pSource) where T2 : IComparable<T2>
		{
			var minValue = default(T2);
			var keys = new List<T1>();
			foreach (var item in pSource)
			{
				if (item.Value.CompareTo(minValue) < 0)
					minValue = item.Value;
			}
			foreach (var item in pSource)
			{
				if (item.Value.CompareTo(minValue) == 0)
					keys.Add(item.Key);
			}
			return keys[Random.Range(0, keys.Count)];
		}

		public static T1 RandomKey<T1, T2>(this Dictionary<T1, T2> pSource, T2 pMinVal) where T2 : IComparable<T2>
		{
			var keys = new List<T1>();
			foreach (var item in pSource)
			{
				if (item.Value.CompareTo(pMinVal) < 0)
					keys.Add(item.Key);
			}
			if (keys.Count == 0)
				return default(T1);
			return keys[Random.Range(0, keys.Count)];
		}

		public static bool ContainsKey<T>(this List<KeyValuePair<int, T>> pList, int pKey)
		{
			for (int i = 0; i < pList.Count; i++)
				if (pList[i].Key == pKey)
					return true;
			return false;
		}

		public static void Remove<T>(this List<KeyValuePair<int, T>> pList, int pKey)
		{
			for (int i = pList.Count - 1; i >= 0; i--)
				if (pList[i].Key == pKey)
				{
					pList.RemoveAt(i);
					break;
				}
		}

		public static void RemoveLast<T>(this List<T> list)
		{
			if (list.Count != 0) list.RemoveAt(list.Count - 1);
		}

		public static string RemoveSpecialCharacters(this string str, string replace = "")
		{
			return Regex.Replace(str, "[^a-zA-Z0-9_.]+", replace, RegexOptions.Compiled);
		}

		public static Vector3 ToVector3(this Vector3Int pVal)
		{
			return new Vector3(pVal.x, pVal.y, pVal.z);
		}

		public static Vector2 ToVector3(this Vector2Int pVal)
		{
			return new Vector3(pVal.x, pVal.y);
		}

		public static void SortByDistanceToPosition(this List<Vector3> pPositions, Vector3 pPosition)
		{
			for (int i = 0; i < pPositions.Count - 1; i++)
			{
				var iSpawnPos = pPositions[i];
				float iDistance = Vector3.Distance(pPosition, iSpawnPos);
				for (int j = i + 1; j < pPositions.Count; j++)
				{
					var jSpawnPos = pPositions[j];
					float jDistance = Vector3.Distance(pPosition, jSpawnPos);
					if (iDistance > jDistance)
					{
						var temp = pPositions[i];
						pPositions[i] = pPositions[j];
						pPositions[j] = temp;
					}
				}
			}
		}
		
		public static void Swap<T>(this List<T> list, int index1, int index2)
		{
			if (list.Count <= 0)
				return;

			if (index1 < 0 || index2 < 0 || index1 == index2 || index1 >= list.Count || index2 >= list.Count)
				return;

			var value = list[index1];
			list[index1] = list[index2];
			list[index2] = value;
		}
		
		public static T Clone<T>(this T self)
		{
			var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(self);
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized);
		}

		public static bool IsChildOfParent(this Transform pItem, Transform pParent)
		{
			while (true)
			{
				if (pItem.parent == null)
					return false;
				if (pItem.parent == pParent)
					return true;
				pItem = pItem.parent;
			}
		}
	}
}