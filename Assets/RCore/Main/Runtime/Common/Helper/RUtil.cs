/**
 * Author HNB-RaBear - 2017
 **/

using Cysharp.Threading.Tasks;
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
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace RCore
{
	public delegate void VoidDelegate();

	public delegate void IntDelegate(int value);

	public delegate void BoolDelegate(bool value);

	public delegate void FloatDelegate(float value);

	public delegate bool ConditionalDelegate();

	public delegate bool ConditionalDelegate<in T>(T pComponent) where T : Component;

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
			int end = sb.Length - 1;
			int start = 0;

			while (end - start > 0)
			{
				char t = sb[end];
				sb[end] = sb[start];
				sb[start] = t;
				start++;
				end--;
			}
		}

		public static void CollectGC()
		{
#if UNITY_EDITOR
			string log = $"BEFORE\n{LogMemoryUsages(false)}";
#endif
			GC.Collect();
#if UNITY_EDITOR
			log += $"\nAFTER\n{LogMemoryUsages(false)}";
			UnityEngine.Debug.Log(log);
#endif
		}

		public static string LogMemoryUsages(bool printLog = true)
		{
			string str =
				$"\nTotal Reserved memory by Unity [GetTotalReservedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576}mb\n - Allocated memory by Unity [GetTotalAllocatedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576}mb\n - Reserved but not allocated [GetTotalUnusedReservedMemoryLong]: {UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong() / 1048576}mb\n - Mono Used Size [GetMonoUsedSizeLong]: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576}mb\n - Mono Heap Size [GetMonoHeapSizeLong]: {UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / 1048576}mb";
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
			Debug.Log(
				$"Screen size: (width:{sWidth}, height:{sHeight})\nSafe area: {Screen.safeArea}\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");
		}

		public static string DictToString(IDictionary<string, object> d)
		{
			return $"{{ {d.Select(kv => $"({kv.Key}, {kv.Value})").Aggregate("", (current, next) => $"{current}{next}, ")}}}";
		}

		[Obsolete]
		public static void CombineMeshes(List<Transform> pMeshObjects, Material pMat, ref GameObject combinedMesh, bool pDestroyOriginal)
		{
			if (combinedMesh == null)
			{
				combinedMesh = new GameObject();
				combinedMesh.GetOrAddComponent<MeshRenderer>();
				combinedMesh.GetOrAddComponent<MeshFilter>();
			}

			var meshFilters = new MeshFilter[pMeshObjects.Count];
			var combine = new CombineInstance[meshFilters.Length];
			for (int i = 0; i < pMeshObjects.Count; i++)
			{
				meshFilters[i] = pMeshObjects[i].GetComponent<MeshFilter>();
				combine[i].mesh = meshFilters[i].sharedMesh;
				combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
				pMeshObjects[i].gameObject.SetActive(false);
				if (pDestroyOriginal)
					UnityEngine.Object.DestroyImmediate(pMeshObjects[i].gameObject);
			}

			combinedMesh.GetOrAddComponent<MeshFilter>().sharedMesh = new Mesh();
			combinedMesh.GetOrAddComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
			combinedMesh.GetComponent<MeshRenderer>().sharedMaterial = pMat;
		}

		public static int GetVersionInt(string version)
		{
			if (!int.TryParse(version.Last().ToString(), out _))
				version = version.Remove(version.Length - 1);
			string[] split = version.Split('.');
			int length = split.Length;
			int versionInt = 0;
			for (int i = 0; i < length; i++)
				versionInt += int.Parse(split[i]) * (int)Mathf.Pow(100, length - i - 1);
			return versionInt;
		}

		public static int CompareVersion(string curVersion, string newVersion, int maxSubVersionLength = 2)
		{
			if (!int.TryParse(curVersion.Last().ToString(), out _))
				curVersion = curVersion.Remove(curVersion.Length - 1);
			if (!int.TryParse(newVersion.Last().ToString(), out _))
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

			// 2.1.2 = 2*10^2 + 1*10^1 + 2*10^0 = 200+10+2 = 212
			// 2.2.6 = 2*10^2 + 2*10^1 + 6*10^0 = 200+20+6 = 226
			// -4 

			// 2.10.22 = 2*100^2 + 10*100^1 + 22*100^0 = 20000+1000+22 = 21022
			// 2.10.26 = 2*100^2 + 10*100^1 + 26*100^0 = 20000+1000+26 = 21026
			// -4

			// 2.9.22 = 2*100^2 + 9*100^1 + 22*100^0 = 20000+900+22 = 20922
			// 2.10.26 = 2*100^2 + 10*100^1 + 26*100^0 = 20000+1000+26 = 21026
			// -104 % 100 = -4

			return (p1 - p2) % maxSubVersion;
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

			int curvedLength = pointsLength * Mathf.RoundToInt(smoothness) - 1;
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
			return curvedPoints.ToArray();
		}

		public static int GetStableHashCode(string str)
		{
			unchecked
			{
				int hash1 = 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
				{
					hash1 = (hash1 << 5) + hash1 ^ str[i];
					if (i == str.Length - 1 || str[i + 1] == '\0')
						break;
					hash2 = (hash2 << 5) + hash2 ^ str[i + 1];
				}

				return hash1 + hash2 * 1566083941;
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

		public static T Clone<T>(T source)
		{
			if (!typeof(T).IsSerializable)
			{
				throw new ArgumentException("The type must be serializable.", "source");
			}

			// Don't serialize a null object, simply return the default for that object
			if (ReferenceEquals(source, null))
			{
				return default;
			}

			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			using (stream)
			{
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(stream);
			}
		}

		public static int GetVersionCode()
		{
#if UNITY_ANDROID
			try
			{
				var contextCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				var context = contextCls.GetStatic<AndroidJavaObject>("currentActivity");
				var packageMngr = context.Call<AndroidJavaObject>("getPackageManager");
				string packageName = context.Call<string>("getPackageName");
				var packageInfo = packageMngr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
				return packageInfo.Get<int>("versionCode");
			}
			catch
#endif
			{
				return 0;
			}
		}

		public static string LoadTextFile(string pPath, IEncryption pEncryption)
		{
			var textAsset = Resources.Load<TextAsset>(pPath);
			if (textAsset != null)
			{
				string content = "";
				if (pEncryption != null)
					content = pEncryption.Decrypt(textAsset.text);
				else
					content = textAsset.text;
				Resources.UnloadAsset(textAsset);
				return content;
			}
			Debug.LogError($"File {pPath} not found");
			return "";
		}

		public static string LoadTextFile(string pPath, bool pEncrypt = false)
		{
			if (pEncrypt)
				return LoadTextFile(pPath, Encryption.Singleton);
			return LoadTextFile(pPath, null);
		}

		public static List<Vector2> GenerateRandomPositions(int count, float radius, float minDistance, int safeLoops = 100)
		{
			var positions = new List<Vector2>();
			for (int i = 0; i < count; i++)
			{
				Vector2 newPos;
				int attempts = 0;
				bool validPosition;
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
			while (positions.Count < count)
			{
				var pos = Random.insideUnitCircle * radius;
				positions.Add(pos);
			}
			return positions;
		}

		private static readonly string[] m_Consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "y", "z" };
		private static readonly string[] m_Vowels = { "a", "e", "i", "o", "u" };
		private static readonly string[] m_MeaningfulNames = new[]
		{
			"Aria", "Kai", "Luna", "Leo", "Nina", "Zara",
			"Aurora", "Caleb", "Dahlia", "Elara", "Felix", "Giselle",
			"Hugo", "Isla", "Jasper", "Kiera", "Liam", "Mila",
			"Nico", "Orion", "Piper", "Quinn", "Rhea", "Silas",
			"Theo", "Una", "Vera", "Willow", "Xander", "Yara", "Zane",
			"Adrian", "Beatrix", "Cassian", "Delilah", "Ezra", "Freya",
			"Gideon", "Hazel", "Iris", "Jonah", "Kian", "Lyra",
			"Magnus", "Noelle", "Octavia", "Phoenix", "Rowan", "Sienna",
			"Tobias", "Uma", "Valeria", "Wren", "Xavier", "Yasmine", "Zayden",
			"Aspen", "Bodhi", "Celeste", "Dante", "Elio", "Fiona",
			"Griffin", "Harlow", "Indigo", "Juno", "Kairos", "Liora",
			"Maverick", "Nova", "Oberon", "Persephone", "Quentin", "Ronan",
			"Selene", "Talia", "Uriah", "Vesper", "Wesley", "Xyla",
			"Yvette", "Zephyr", "Alaric", "Briar", "Caspian", "Daphne",
			"Ember", "Finnian", "Gaia", "Halcyon", "Isolde", "Jace",
			"Kaida", "Leander", "Mirabel", "Nyx", "Orla", "Phaedra",
			"Quorra", "Rex", "Seraphina", "Thalia"
		};
		private static readonly string[] m_NonLatinNames = new[]
		{
			"Лев", "Айхан", "Ясин", "सिया", "明辉", "다온", "Ταξιάρχης", "محمد", "俊豪", "Татьяна",
			"נאור", "सूरज", "신우", "آیلا", "Θεοδώρα", "กานต์", "จารุวรรณ", "अर्जुन", "רפאל", "阳阳",
			"Антон", "Алина", "Николай", "Оксана", "Дмитрий", "Екатерина", "Василий", "Мария", "Александр", "Ирина",
			"عباس", "فاطمة", "ياسر", "زهراء", "حسن", "نور", "عبدالله", "سارة", "علي", "مريم",
			"मनीष", "प्रिया", "अमित", "नेहा", "रवि", "पूनम", "कृष्ण", "स्नेहा", "संदीप", "अनिता",
			"王伟", "李娜", "张敏", "陈杰", "刘洋", "杨芳", "赵磊", "孙梅", "周涛", "吴霞",
			"정우", "지민", "하준", "서연", "도윤", "윤아", "민준", "수진", "태현", "지원",
			"Αναστάσιος", "Ελένη", "Δημήτριος", "Αικατερίνη", "Χριστόφορος", "Αγγελική", "Θεόδωρος", "Σοφία", "Γεώργιος", "Μαρία",
		};
		private static string GenerateStupidUserName(int syllables)
		{
			string userName = "";
			bool useConsonant = Random.value > 0.5f;
			for (int i = 0; i < syllables; i++)
			{
				if (useConsonant)
				{
					userName += m_Consonants[Random.Range(0, m_Consonants.Length)];
					useConsonant = false;
				}
				else
				{
					userName += m_Vowels[Random.Range(0, m_Vowels.Length)];
					useConsonant = true;
				}
			}
			return char.ToUpper(userName[0]) + userName.Substring(1);
		}
		public static string GenerateUserName()
		{
			string userName = "";
			if (Random.value < 0.22f) // Choose meaningful name
			{
				if (Random.value < 0.66) // Choose latin name
				{
					userName = m_MeaningfulNames[Random.Range(0, m_MeaningfulNames.Length)];
					if (Random.value < 0.22f)
						userName += Random.Range(80, 100);
				}
				else
					userName = m_NonLatinNames[Random.Range(0, m_NonLatinNames.Length)];
			}
			else
			{
				userName = GenerateStupidUserName(Random.Range(3, 7));
				if (Random.value < 0.22f)
					userName += Random.Range(80, 100);
			}
			return userName;
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

		public static void Raise<T>(this Action<T> pAction, T pParam)
		{
			pAction?.Invoke(pParam);
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

#region Array Extensions

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
			var indexes = new List<int>();
			for (int i = 0; i < pArray.Length; i++)
				if (match(pArray[i]))
					indexes.Add(i);
			var result = new T[indexes.Count];
			for (int i = 0; i < indexes.Count; i++)
				result[i] = pArray[indexes[i]];
			return result;
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
			var newArray = new T[pArray.Length + 1];
			Array.Copy(pArray, newArray, pArray.Length);
			newArray[pArray.Length] = pObj;
			output = newArray;
		}

		public static void AddRange<T>(this T[] pArray1, T[] pArray2, out T[] output)
		{
			var newArray = new T[pArray1.Length + pArray2.Length];
			Array.Copy(pArray1, 0, newArray, 0, pArray1.Length);
			Array.Copy(pArray2, 0, newArray, pArray1.Length, pArray2.Length);
			output = newArray;
		}

		public static void Remove<T>(this T[] pArray, T pObj, out T[] output)
		{
			var comparer = EqualityComparer<T>.Default;
			int count = 0;

			// First pass: count how many elements need to be removed
			for (int i = 0; i < pArray.Length; i++)
				if (comparer.Equals(pArray[i], pObj))
					count++;

			// If no elements need to be removed, return the original array
			if (count == 0)
				output = (T[])pArray.Clone();

			var newArray = new T[pArray.Length - count];
			int j = 0;

			// Second pass: copy the elements that are not equal to pObj
			for (int i = 0; i < pArray.Length; i++)
				if (!comparer.Equals(pArray[i], pObj))
					newArray[j++] = pArray[i];

			output = newArray;
		}

		public static void RemoveAll<T>(this T[] pArray, Predicate<T> match, out T[] output)
		{
			// Count the number of elements that don't match the condition
			int count = 0;
			for (int i = 0; i < pArray.Length; i++)
			{
				if (!match(pArray[i]))
					count++;
			}

			// Create a new array with the non-matching elements
			var result = new T[count];
			int index = 0;
			for (int i = 0; i < pArray.Length; i++)
			{
				if (!match(pArray[i]))
				{
					result[index] = pArray[i];
					index++;
				}
			}
			output = result;
		}

		public static void RemoveAt<T>(this T[] pArray, int pIndex, out T[] output)
		{
			int j = 0;
			var newArray = new T[pArray.Length - 1];
			for (int i = 0; i < pArray.Length; i++)
				if (i != pIndex)
				{
					newArray[j] = pArray[i];
					j++;
				}
			output = newArray;
		}

		public static int IndexOf<T>(this T[] pArray, T pObj)
		{
			var comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < pArray.Length; i++)
				if (comparer.Equals(pArray[i], pObj))
					return i;
			return -1;
		}

		public static int FindIndex<T>(this T[] pArray, Predicate<T> match)
		{
			for (int i = 0; i < pArray.Length; i++)
				if (match(pArray[i]))
					return i;
			return -1;
		}

#endregion

		public static List<T> RemoveNull<T>(this List<T> pList) where T : class
		{
			pList.RemoveAll(item => item == null);
			return pList;
		}

		public static void RemoveDuplicated<T>(this List<T> list)
		{
			var seenItems = new HashSet<T>();
			var duplicates = new List<T>();

			foreach (var item in list)
				if (!seenItems.Add(item))
					duplicates.Add(item);

			foreach (var duplicate in duplicates)
				list.Remove(duplicate);
		}

		public static void RemoveDuplicatedKey<TKey, TValue>(this List<SerializableKeyValue<TKey, TValue>> list)
		{
			var seenKeys = new HashSet<TKey>();
			var duplicates = new List<SerializableKeyValue<TKey, TValue>>();

			foreach (var item in list)
				if (seenKeys.Contains(item.k))
					duplicates.Add(item);
				else
					seenKeys.Add(item.k);

			foreach (var duplicate in duplicates)
				list.Remove(duplicate);
		}

		public static void RemoveDuplicatedValue<TKey, TValue>(this List<SerializableKeyValue<TKey, TValue>> list)
		{
			var seenValues = new HashSet<TValue>();
			var duplicates = new List<SerializableKeyValue<TKey, TValue>>();

			foreach (var item in list)
				if (seenValues.Contains(item.v))
					duplicates.Add(item);
				else
					seenValues.Add(item.v);

			foreach (var duplicate in duplicates)
				list.Remove(duplicate);
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
						(pPositions[i], pPositions[j]) = (pPositions[j], pPositions[i]);
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

			(list[index1], list[index2]) = (list[index2], list[index1]);
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