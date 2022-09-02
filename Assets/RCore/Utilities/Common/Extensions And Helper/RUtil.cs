/**
 * Author RadBear - nbhung71711 @gmail.com - 2017 - 2020
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
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

		public static string JoinString(string seperator, params string[] strs)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < strs.Length; i++)
			{
				if (!string.IsNullOrEmpty(strs[i]))
					list.Add(strs[i]);
			}
			return string.Join(seperator, list.ToArray());
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
			if (pFileName == null)
				pFileName = $"{Application.productName}_{DateTime.Now.ToString("yyyyMMdd_HHmm")}";
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

		public static T NullArgumentTest<T>(T value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(typeof(T).ToString());
			}

			return value;
		}

		public static string DictToString(IDictionary<string, object> d)
		{
			return "{ " + d
				.Select(kv => "(" + kv.Key + ", " + kv.Value + ")")
				.Aggregate("", (current, next) => current + next + ", ")
				+ "}";
		}

		public static void CombineMeshs(List<Transform> pMeshobjects, Material pMat, ref GameObject m_CombinedMesh, bool pDetroyOrginal)
		{
			if (m_CombinedMesh == null)
			{
				m_CombinedMesh = new GameObject();
				m_CombinedMesh.GetOrAddComponent<MeshRenderer>();
				m_CombinedMesh.GetOrAddComponent<MeshFilter>();
			}

			var meshFilters = new MeshFilter[pMeshobjects.Count];
			var combine = new CombineInstance[meshFilters.Length];
			for (int i = 0; i < pMeshobjects.Count; i++)
			{
				meshFilters[i] = pMeshobjects[i].GetComponent<MeshFilter>();
				combine[i].mesh = meshFilters[i].sharedMesh;
				combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
				pMeshobjects[i].gameObject.SetActive(false);
				if (pDetroyOrginal)
					GameObject.DestroyImmediate(pMeshobjects[i].gameObject);
			}

			m_CombinedMesh.GetOrAddComponent<MeshFilter>().sharedMesh = new Mesh();
			m_CombinedMesh.GetOrAddComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
			m_CombinedMesh.GetComponent<MeshRenderer>().sharedMaterial = pMat;
		}
	}

	public static class RUtilExtension
	{
		public static string ToSentenceCase(this string pString)
		{
			var lowerCase = pString.ToLower();
			// matches the first sentence of a string, as well as subsequent sentences
			var r = new Regex(@"(^[a-z])|\.\s+(.)", RegexOptions.ExplicitCapture);
			// MatchEvaluator delegate defines replacement of setence starts to uppercase
			var result = r.Replace(lowerCase, s => s.Value.ToUpper());
			return result;
		}

		public static string ToCapitalizeEachWord(this string pString)
		{
			// Creates a TextInfo based on the "en-US" culture.
			TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
			return textInfo.ToTitleCase(pString);
		}

		public static string ToLowerCaseFirstChar(this string pString)
		{
			if (string.IsNullOrEmpty(pString) || char.IsLower(pString[0]))
				return pString;

			return char.ToLower(pString[0]) + pString.Substring(1);
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
			if (pAction != null) pAction();
		}

		public static void Raise(this UnityAction pAction)
		{
			if (pAction != null) pAction();
		}

		public static void Raise<T>(this Action<T> pAction, T pParam)
		{
			if (pAction != null) pAction(pParam);
		}

		public static void Raise<T>(this UnityAction<T> pAction, T pParam)
		{
			if (pAction != null) pAction(pParam);
		}

		public static void Raise<T, M>(this Action<T, M> pAction, T pParam1, M pParam2)
		{
			if (pAction != null) pAction(pParam1, pParam2);
		}

		public static void Raise<T, M>(this UnityAction<T, M> pAction, T pParam1, M pParam2)
		{
			if (pAction != null) pAction(pParam1, pParam2);
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

		public static List<T> RemoveDupplicate<T>(this List<T> pList) where T : UnityEngine.Object
		{
			List<int> duplicate = new List<int>();
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

		public static Dictionary<int, int> Add(this Dictionary<int, int> pSource, Dictionary<int, int> pDict)
		{
			foreach (var item in pDict)
			{
				if (pSource.ContainsKey(item.Key))
					pSource[item.Key] += item.Value;
				else
					pSource.Add(item.Key, item.Value);
			}
			return pSource;
		}

		public static Dictionary<int, int> Remove(this Dictionary<int, int> pSource, Dictionary<int, int> pDict)
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
			return pSource;
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
			T2 minValue = default(T2);
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
	}
}