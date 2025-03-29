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

		public static string GetRandomCountryCode()
		{
			string[] countryCodes =
			{
				"US", "GB", "CA", "AU", "NZ", "IE", "PH", "ZA", "NG", "IN",
				"RU", "BY", "BG", "UA", "RS", "MK", "KZ", "KG", "MN",
				"CN", "HK", "TW", "SG",
				"JP",
				"KR",
				"SA", "AE", "EG", "IQ", "SY", "JO", "LB", "OM", "QA", "KW", "DZ", "LY",
				"VN",
				"TH"
			};
			return countryCodes[Random.Range(0, countryCodes.Length)];
		}
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
				case SystemLanguage.Unknown: return "US"; // Fallback
				default: return "US";
			}
		}
		public static string GetCharacterSet(string countryCode)
		{
			if (string.IsNullOrEmpty(countryCode))
				return "English";
			switch (countryCode.ToUpper())
			{
				case "RU":
				case "BY":
				case "BG":
				case "UA":
				case "RS":
				case "MK":
				case "KZ":
				case "KG":
				case "MN":
					return "Cyrillic";
				case "CN":
				case "HK":
				case "TW":
				case "SG":
					return "Chinese";
				case "JP":
					return "Japanese";
				case "KR":
					return "Korean";
				case "SA":
				case "AE":
				case "EG":
				case "IQ":
				case "SY":
				case "JO":
				case "LB":
				case "OM":
				case "QA":
				case "KW":
				case "DZ":
				case "LY":
					return "Arabic";
				case "VN":
					return "Vietnamese";
				case "TH":
					return "Thai";
				case "US":
				case "GB":
				case "CA":
				case "AU":
				case "NZ":
				case "IE":
				case "PH":
				case "ZA":
				case "NG":
				case "IN":
					return "English";
				default:
					return "Latin";
			}
		}
		public static string GenerateDisplayName(string countryCode)
		{
			if (Random.value < 0.11f)
				return GenerateLatinDisplayName();
			string characterSet = GetCharacterSet(countryCode);
			switch (characterSet)
			{
				case "Cyrillic": return GenerateCyrillicDisplayName();
				case "Chinese": return GenerateChineseDisplayName();
				case "Japanese": return GenerateJapaneseDisplayName();
				case "Korean": return GenerateKoreanDisplayName();
				case "Arabic": return GenerateArabicDisplayName();
				case "Vietnamese": return GenerateVietnameseDisplayName();
				case "Thai": return GenerateThailandDisplayName();
				case "English": return GenerateEnglishDisplayName();
				default: return GenerateLatinDisplayName(); // Default to Latin
			}
		}
		private static string GenerateCyrillicDisplayName()
		{
			string[] firstPart =
			{
				"Алекс", "Влади", "Конста", "Михай", "Серге", "Дими", "Анато", "Евге", "Никола", "Ива",
				"Оле", "Паве", "Фёдо", "Яро", "Рост", "Григо", "Андре", "Бори", "Леони", "Дани",
				"Тимо", "Арка", "Игна", "Васи", "Кири", "Генна", "Степа", "Матве", "Плато", "Свя",
				"Русла", "Тара", "Фило", "Вене", "Зах", "Саве", "Арсе", "Ереме", "Трофи", "Яков",
				"Эдуа", "Вита", "Эми", "Роди", "Елисе", "Авер", "Агата", "Кла", "Оре", "Про",
				"Мари", "Рема", "Дороф", "Ники", "Ефи", "Гаври", "Лазар", "Фадде", "Миро", "Ови",
				"Кузь", "Добри", "Дени", "Лука", "Мал", "Виль", "Ефре", "Капи", "Стан", "Алекси",
				"Симе", "Владими", "Сама", "Ани", "Вени", "Ил", "Нат", "Станис", "Ярос", "Глеб",
				"Юли", "Юр", "Макси", "Лавре", "Фило", "Рос", "Реми", "Тихо", "Усти", "Пара",
				"Оле", "Осе", "Исаа", "Изи", "Сте", "Каро", "Адр", "Вла", "Оре", "Дав"
			};
			string[] secondPart =
			{
				"ндр", "мир", "нтин", "лов", "евич", "дрей", "ий", "евич", "ич", "рий",
				"гей", "кий", "дор", "слав", "тан", "гор", "ей", "рий", "ий", "лей",
				"тий", "дий", "ций", "фей", "рий", "ей", "тий", "нат", "сей", "нтий",
				"рос", "рий", "тий", "нат", "сим", "фей", "стан", "лев", "мак", "нец",
				"фей", "ский", "ний", "кин", "дор", "мей", "лев", "рин", "рат", "мей",
				"тий", "ван", "сей", "гин", "мит", "якин", "лин", "сь", "вик", "лов",
				"дров", "ник", "пин", "мор", "фин", "ний", "рей", "слин", "дар", "рий",
				"ван", "кий", "гай", "риев", "чен", "чук", "сен", "ндий", "гор", "сий",
				"ров", "лий", "мец", "фей", "рис", "дрик", "кле", "ран", "тин", "рок",
				"то", "мон", "кел", "мей", "дуй", "дот", "чук", "лук", "вец", "кин"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateChineseDisplayName()
		{
			string[] firstPart =
			{
				"张伟", "王芳", "李强", "刘杰", "陈磊", "杨涛", "赵敏", "孙超", "周军", "吴霞",
				"郑波", "冯涛", "蒋静", "韩磊", "杨丽", "朱敏", "高翔", "黄涛", "何军", "罗丽",
				"胡斌", "梁伟", "宋磊", "谢静", "唐涛", "许杰", "邹波", "崔丽", "程超", "马军",
				"曹敏", "董强", "田丽", "任涛", "钟磊", "谭军", "贺静", "邱超", "余波", "陆杰",
				"夏涛", "蔡敏", "潘磊", "杜静", "黎军", "常涛", "康丽", "龚超", "范军", "孔磊",
				"石敏", "孟涛", "邵杰", "桂丽", "章超", "钱军", "白磊", "江静", "牛涛", "尤军",
				"严磊", "华静", "左涛", "戴军", "龙超", "陶磊", "韦静", "贾涛", "顾军", "毛磊",
				"郝静", "邢涛", "邬军", "安超", "常磊", "段静", "雷涛", "武军", "谭磊", "狄静",
				"滕涛", "温军", "姬磊", "封静", "桑涛", "党军", "宫磊", "路静", "米涛", "乔军",
				"齐磊", "廉静", "车涛", "侯军", "连磊", "漆静", "邰涛", "仲军", "屠磊", "阮静"
			};
			string[] secondPart =
			{
				"俊豪", "晓明", "丽娜", "国鹏", "建华", "雪梅", "志强", "美玲", "海涛", "玉兰",
				"春华", "嘉伟", "秋萍", "晨曦", "志华", "思雨", "志杰", "秀兰", "俊峰", "艳红",
				"小龙", "桂英", "振华", "海霞", "成龙", "丽华", "新宇", "秀珍", "子豪", "海燕",
				"志鹏", "俊英", "少华", "金凤", "浩然", "秀娟", "建国", "静怡", "家豪", "志玲",
				"俊宇", "彩霞", "向东", "玉珍", "浩宇", "晓燕", "光明", "桂芳", "一鸣", "晓莉",
				"志伟", "桂香", "子轩", "淑英", "俊杰", "美珍", "天宇", "红梅", "鸿飞", "春霞",
				"伟杰", "玉华", "志勇", "秀华", "浩翔", "海丽", "子涵", "慧敏", "世杰", "丽红",
				"伟明", "晓辉", "家俊", "婉婷", "国栋", "婷婷", "嘉豪", "玉梅", "昊然", "静茹",
				"志涛", "春燕", "伟宏", "美娜", "文博", "玉凤", "志宁", "月华", "志鑫", "莉莉",
				"文轩", "燕玲", "旭东", "雪芳", "俊楠", "红霞", "鹏飞", "静雯", "世豪", "小霞"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateJapaneseDisplayName()
		{
			string[] firstPart =
			{
				"田中", "鈴木", "高橋", "山本", "井上", "松本", "佐藤", "中村", "小林", "加藤",
				"吉田", "山田", "渡辺", "藤田", "石井", "原田", "清水", "竹内", "林", "森",
				"池田", "橋本", "山口", "岡田", "坂本", "杉山", "村上", "西村", "大野", "大谷",
				"近藤", "菅原", "内田", "福田", "川上", "安田", "小川", "千葉", "松井", "片山",
				"中川", "柴田", "水野", "木村", "藤井", "長谷川", "坂井", "青木", "浜田", "栗原",
				"三浦", "久保", "石田", "遠藤", "永井", "堀", "平井", "横山", "新井", "市川",
				"丸山", "宮田", "杉本", "中西", "土屋", "金子", "今井", "宇野", "片岡", "河野",
				"大塚", "藤原", "野村", "岡本", "富田", "吉岡", "村田", "高木", "高田", "長田",
				"岩田", "宮本", "高野", "北村", "川村", "島田", "大島", "松田", "古川", "福島",
				"今村", "野口", "川崎", "望月", "田村", "関口", "三宅", "伊藤", "赤坂", "樋口"
			};
			string[] secondPart =
			{
				"一郎", "美咲", "翔太", "優奈", "健吾", "直樹", "香織", "達也", "真由美", "太郎",
				"陽菜", "悠斗", "彩花", "大輔", "千尋", "涼介", "真理", "慎太郎", "玲奈", "悠真",
				"麻衣", "陽翔", "翔子", "颯太", "杏奈", "和也", "由美", "圭佑", "さくら", "拓海",
				"愛理", "陽太", "美穂", "悠人", "友美", "大和", "紗希", "優斗", "恵子", "康太",
				"真紀", "俊介", "美優", "亮介", "陽子", "陽樹", "梨花", "直也", "理奈", "優輝",
				"奈緒", "裕也", "結衣", "悠", "萌", "佑介", "聡子", "剛", "菜々", "誠",
				"彩乃", "雄太", "愛", "真琴", "光", "美咲", "優太", "香奈", "勇輝", "咲",
				"弘樹", "さゆり", "慎", "綾乃", "龍太郎", "千夏", "仁", "玲子", "圭吾", "友香",
				"雅人", "琴音", "篤志", "奈々子", "琢磨", "葉月", "達郎", "悠希", "祐輔", "琴美",
				"義人", "美帆", "大樹", "沙織", "隼人", "莉奈", "拓真", "陽菜", "宗一郎", "陽葵"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateKoreanDisplayName()
		{
			string[] firstPart =
			{
				"김준", "이현", "박민", "최성", "정우", "강호", "조영", "한결", "윤서", "신태",
				"손혁", "서진", "배수", "송혁", "남건", "임규", "문찬", "권도", "홍기", "안빈",
				"백훈", "심재", "양태", "노경", "조운", "전범", "강윤", "유빈", "장혁", "석호",
				"표진", "황성", "제훈", "명진", "길현", "은찬", "민수", "도현", "기태", "상우",
				"영민", "준혁", "우진", "성훈", "태윤", "진서", "수호", "동현", "정빈", "건우",
				"상진", "재훈", "시후", "석준", "지환", "현우", "희준", "태호", "도영", "규현",
				"현빈", "유찬", "형준", "시온", "동우", "태민", "성준", "우석", "윤호", "규민",
				"찬우", "수민", "정환", "영훈", "승현", "지훈", "도준", "세준", "유현", "석훈",
				"현준", "민혁", "시영", "재민", "건호", "명훈", "진우", "태경", "승우", "혁진",
				"규태", "진혁", "지용", "선우", "태우", "찬혁", "상민", "하늘", "윤재", "재욱"
			};
			string[] secondPart =
			{
				"호", "哲", "英", "美", "泰", "昊", "浩", "然", "宇", "赫",
				"俊", "誠", "熙", "真", "成", "基", "元", "哲", "烈", "濟",
				"炫", "昱", "順", "潤", "圭", "天", "昌", "政", "榮", "佑",
				"泰", "建", "希", "民", "燦", "和", "勇", "恒", "一", "承",
				"厚", "祥", "俊", "澤", "秀", "宰", "善", "弘", "尚", "洙",
				"範", "佑", "鎮", "東", "弼", "誠", "恩", "載", "宣", "俊",
				"玟", "祥", "赫", "錫", "弘", "慶", "權", "洙", "辰", "峰",
				"弘", "彬", "煥", "鍾", "植", "宸", "珉", "翼", "賢", "德",
				"燦", "楨", "基", "善", "弦", "昊", "智", "翰", "潤", "勇",
				"恒", "鎬", "東", "宙", "淳", "洙", "燦", "玟", "誠", "熙"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateArabicDisplayName()
		{
			string[] firstPart =
			{
				"عبد", "محمد", "أحمد", "يوسف", "سليم", "إبري", "خالد", "علي", "حسن", "راشد",
				"سعد", "عمر", "طارق", "مروان", "بدر", "نواف", "فيصل", "ماجد", "زياد", "فهد",
				"أنس", "لؤي", "أكرم", "أوس", "تامر", "هاشم", "هيثم", "ثامر", "بلال", "سامر",
				"فراس", "خليل", "شادي", "مالك", "يزيد", "براء", "كريم", "محسن", "بشار", "طلال",
				"زياد", "حمزة", "رمزي", "عادل", "وسيم", "معتز", "شريف", "رائد", "داود", "إياد",
				"يحيى", "مراد", "عماد", "مجد", "أيمن", "مهند", "ربيع", "علاء", "ضياء", "حارث",
				"نزار", "جواد", "سراج", "أنور", "عباس", "مكرم", "صفوان", "وائل", "حاتم", "مازن",
				"بهي", "كاظم", "زاهر", "إيهاب", "شاكر", "راغب", "حكمت", "وسام", "نعيم", "إدريس"
			};
			string[] secondPart =
			{
				"الله", "سعد", "حسن", "رحم", "مهدي", "صدي", "كري", "هادي", "شري", "عاب",
				"مبا", "منص", "فتح", "لطيف", "حميد", "عظي", "غفو", "رشيد", "حكي", "وال",
				"معي", "راش", "مجيد", "رؤو", "صبر", "بشي", "ماجد", "متو", "مهيم", "سعي",
				"شاكر", "رزاق", "علي", "نور", "قدير", "ودود", "رفيق", "منت", "عدل", "ولي",
				"شاف", "ظاهر", "باسط", "منير", "متين", "شهيد", "غني", "قدوس", "سمي", "مصور"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateVietnameseDisplayName()
		{
			string[] firstPart =
			{
				"Nguyen", "Tran", "Le", "Pham", "Hoang", "Bui", "Dang", "Do", "Dinh", "Duong",
				"Huynh", "Luu", "Ly", "Mac", "Ngo", "Ngoc", "Ninh", "Quach", "Thai", "Trinh",
				"Truong", "Tu", "Vu", "An", "Bach", "Cao", "Chau", "Chu", "Cong", "Cuong",
				"Danh", "Diep", "Gia", "Giang", "Ha", "Hanh", "Hien", "Hong", "Huu", "Khai",
				"Khanh", "Khieu", "Kim", "Kinh", "Lai", "Lam", "Lang", "Linh", "Long", "Mai",
				"Minh", "Nam", "Nghia", "Nhan", "Nhat", "Phan", "Phong", "Phu", "Quoc", "Quyen",
				"Sang", "Son", "Sy", "Tai", "Tam", "Tan", "Thach", "Thanh", "Thao", "Thien",
				"Thong", "Thuan", "Tien", "Toan", "Trac", "Tri", "Trieu", "Trong", "Tuong", "Uy",
				"Van", "Vi", "Viet", "Vinh", "Xuan", "Yen", "Hau", "Lien", "Loan", "Nga",
				"Nguyet", "Oanh", "Phuong", "Quynh", "Tham", "Thuy", "Trang", "Truc", "Tuyet", "Vy"
			};
			string[] secondPart =
			{
				"Thanh", "Quang", "Minh", "Anh", "Tuan", "Bao", "Binh", "Cao", "Chinh", "Cong",
				"Cuong", "Dai", "Dang", "Dat", "Dinh", "Duc", "Duy", "Giang", "Ha", "Hao",
				"Hiep", "Hieu", "Hoang", "Hong", "Huu", "Khanh", "Khoa", "Kien", "Lam", "Linh",
				"Long", "Luan", "Luong", "Mai", "Manh", "Nam", "Nghia", "Nhan", "Nhat", "Phong",
				"Phuc", "Phuoc", "Quoc", "Quy", "Sang", "Son", "Sy", "Tai", "Tam", "Tan",
				"Thach", "Thang", "Thao", "Thien", "Thinh", "Thong", "Thu", "Thuan", "Tien", "Toan",
				"Trieu", "Trong", "Tuan", "Tung", "Tuong", "Uy", "Van", "Vi", "Viet", "Vinh",
				"Xuan", "Yen", "An", "Bach", "Chau", "Diep", "Dong", "Hanh", "Hau", "Hoan",
				"Huyen", "Khiem", "Lan", "Lien", "Loan", "Mai", "Nga", "Ngoc", "Nhi", "Nu",
				"Oanh", "Phuong", "Quyen", "Quynh", "Tam", "Tham", "Thuy", "Trang", "Trinh", "Tuyet"
			};
			string first = firstPart[Random.Range(0, firstPart.Length)];
			string second = secondPart[Random.Range(0, secondPart.Length)];
			if (first.Length <= 3 || second.Length <= 3)
				return $"{first} {second}";
			var rad = Random.value;
			return rad switch
			{
				< 0.4f => Random.value > 0.5f ? first : first.ToLower(),
				< 0.8f => Random.value > 0.5f ? second : second.ToLower(),
				_ => Random.value > 0.5f ? $"{first} {second}" : $"{first.ToLower()} {second.ToLower()}"
			};
		}
		private static string GenerateThailandDisplayName()
		{
			string[] firstPart =
			{
				"นัท", "บีม", "ตูน", "ฟ้า", "จูน", "แก้ม", "ดิว", "กาย", "ต้น", "เบียร์",
				"โอ๊ต", "ไนซ์", "มิกซ์", "พล", "ก้อง", "เนย", "โอ", "โต้ง", "เจน", "แนน",
				"แพท", "กุ้ง", "บัว", "แทน", "คิว", "แอม", "จุ๊บ", "เป้", "ท๊อป", "หมิว",
				"พีช", "ต้น", "หมอก", "จิ๋ว", "ปอ", "ตั้ม", "ปิง", "ยิ้ม", "ซัน", "เอิร์ธ",
				"นิค", "เจ", "ส้ม", "จอย", "พลอย", "เมย์", "ฟาง", "ชม", "เบล", "บอส",
				"กาย", "โอ", "แชมป์", "ฟิล์ม", "โอม", "ซาร่า", "โต๋", "บูม", "เอ็ม", "มาร์ค",
				"ไอซ์", "เนส", "อาร์ม", "เอม", "บอม", "มิว", "เป้", "เฟิร์น", "กิ๊ฟ", "ตาล",
				"บุ๊ค", "จิ๊บ", "ฝน", "ปัน", "ปอ", "จีจี้", "แพรว", "ขิม", "แอน", "โอ๋",
				"เกด", "กุ้ง", "พลอย", "พิม", "ฝ้าย", "นุ่น", "แพน", "ข้าว", "จิน", "เมย์",
				"กี้", "นิว", "บิ๊ก", "เติ้ล", "แจ๊ค", "หมอ", "ภพ", "เล็ก", "โตโต้", "ซิน"
			};
			string[] secondPart =
			{
				"นี", "โต", "ชัย", "ดา", "บี", "โอ", "นะ", "แจ", "ติ", "ริ",
				"ฟู", "จู", "ดี้", "พล", "แซม", "แท", "กิ๊", "มิว", "ซู", "โซ",
				"ขวัญ", "หลิน", "ฝัน", "หมวย", "นก", "โบ", "หงส์", "อิง", "ฟ้า", "ขจี",
				"กัน", "แจน", "เดย์", "เต๋อ", "ปิง", "ขนุน", "หนู", "โม", "รัน", "พี",
				"บัว", "พง", "ข้าว", "นา", "ติ๋ว", "นัท", "โอ๊ต", "หนิง", "ทอม", "เฟิร์น",
				"โจ๊ก", "แพน", "ก้อย", "จีจี้", "พร", "กาย", "ส้ม", "ขจร", "บุ๋ม", "โอ๋",
				"นิว", "ต้น", "ฟิล์ม", "ซิน", "เมย์", "เบน", "เติ้ล", "โท", "แจ๊ค", "เพชร",
				"ภู", "แก้ม", "ปุ้ย", "เคน", "เพียว", "แอม", "หนู", "ขิม", "ชมพู่", "มิ้น",
				"โบว์", "แพรว", "จีจี้", "ฝ้าย", "แอน", "ดิว", "มิว", "อั้ม", "บุ๊ค", "บิว",
				"บิ๊ก", "โม", "ตาล", "บาส", "ตั้ม", "หมอ", "ภพ", "เมฆ", "เล็ก", "ซัน"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateLatinDisplayName()
		{
			string[] firstPart =
			{
				"Luca", "Gino", "Brio", "Hugo", "Nico", "Paco", "Tino", "Rico", "Leo", "Tito",
				"Milo", "Dino", "Vito", "Lino", "Enzo", "Ciro", "Filo", "Remo", "Elio", "Simo",
				"Javi", "Beni", "Davi", "Ludo", "Reno", "Caro", "Gabe", "Juli", "Talo", "Ramo",
				"Manu", "Jano", "Miro", "Pipo", "Teo", "Ugo", "Tano", "Otto", "Raul", "Emil",
				"Alba", "Cleo", "Lila", "Mila", "Rita", "Tara", "Vera", "Zara", "Luna", "Nina",
				"Rosa", "Ines", "Aida", "Luci", "Gala", "Maia", "Noel", "Cira", "Leta", "Vina",
				"Juno", "Zeno", "Orla", "Nilo", "Vina", "Arlo", "Iker", "Omar", "Pere", "Quim",
				"Saul", "Uri", "Xavi", "Yago", "Zeno", "Biel", "Gus", "Ivo", "Max", "Ned",
				"Oto", "Rex", "Sil", "Tom", "Ugo", "Val", "Wil", "Xan", "Yan", "Zac"
			};
			string[] secondPart =
			{
				"Vas", "Rui", "San", "Leo", "Paz", "Sol", "Mar", "Lux", "Cruz", "Fel",
				"Tiz", "Jul", "Hugo", "Luca", "Dino", "Enzo", "Rico", "Milo", "Otto", "Nico",
				"Raul", "Ugo", "Tito", "Beni", "Ciro", "Reno", "Tano", "Remo", "Gino", "Javi",
				"Lino", "Simo", "Talo", "Manu", "Davi", "Jano", "Miro", "Pipo", "Teo", "Elio",
				"Ludo", "Gabe", "Juli", "Ramo", "Caro", "Biel", "Filo", "Tito", "Zeno", "Hugo",
				"Lino", "Otto", "Reno", "Sil", "Max", "Ned", "Oto", "Rex", "Tom", "Ugo",
				"Val", "Wil", "Xan", "Yan", "Zac", "Ivo", "Gus", "Saul", "Uri", "Xavi",
				"Yago", "Zeno", "Biel", "Gus", "Ivo", "Max", "Ned", "Oto", "Rex", "Sil",
				"Tom", "Ugo", "Val", "Wil", "Xan", "Yan", "Zac", "Iker", "Omar", "Pere",
				"Quim", "Arlo", "Orla", "Nilo", "Vina", "Juno", "Cira", "Leta", "Vina", "Maia"
			};
			string first = firstPart[Random.Range(0, firstPart.Length)];
			string second = secondPart[Random.Range(0, secondPart.Length)];
			if (first.Length <= 3 || second.Length <= 3)
				return $"{first} {second}";
			var rad = Random.value;
			return rad switch
			{
				< 0.4f => Random.value > 0.5f ? first : first.ToLower(),
				< 0.8f => Random.value > 0.5f ? second : second.ToLower(),
				_ => Random.value > 0.5f ? $"{first} {second}" : $"{first.ToLower()} {second.ToLower()}"
			};
		}
		private static string GenerateEnglishDisplayName()
		{
			string[] firstPart =
			{
				"Bob", "Max", "Tim", "Sam", "Joe", "Tom", "Ben", "Ned", "Jay", "Roy",
				"Leo", "Gus", "Zed", "Lou", "Dex", "Ace", "Bud", "Rex", "Hal", "Jax",
				"Moe", "Sid", "Cal", "Hank", "Buck", "Duke", "Skip", "Biff", "Chip", "Zane",
				"Fizz", "Boom", "Zap", "Buzz", "Taz", "Zig", "Fuzz", "Quip", "Wiz", "Jinx",
				"Dash", "Riff", "Vex", "Zork", "Plop", "Twix", "Flip", "Bam", "Bop", "Zip"
			};
			string[] secondPart =
			{
				"Zap", "Fizz", "Boom", "Buzz", "Bop", "Jinx", "Zork", "Wiz", "Twix", "Dash",
				"Zed", "Moo", "Pug", "Duke", "Rex", "Max", "Taz", "Biff", "Plop", "Fuzz",
				"Quip", "Chomp", "Flip", "Dork", "Zig", "Chop", "Vex", "Bonk", "Skip", "Honk",
				"Gulp", "Funk", "Bash", "Munch", "Jolt", "Twist", "Boop", "Snip", "Womp", "Zonk",
				"Riff", "Thud", "Boing", "Splish", "Blip", "Slap", "Tonk", "Twitch", "Splat", "Clap"
			};
			string first = firstPart[Random.Range(0, firstPart.Length)];
			string second = secondPart[Random.Range(0, secondPart.Length)];
			if (first.Length <= 3 || second.Length <= 3)
				return $"{first} {second}";
			var rad = Random.value;
			return rad switch
			{
				< 0.4f => Random.value > 0.5f ? first : first.ToLower(),
				< 0.8f => Random.value > 0.5f ? second : second.ToLower(),
				_ => Random.value > 0.5f ? $"{first} {second}" : $"{first.ToLower()} {second.ToLower()}"
			};
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