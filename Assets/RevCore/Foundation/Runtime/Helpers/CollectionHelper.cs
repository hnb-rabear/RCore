using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RevCore
{
	public static class CollectionHelper
	{
		public static string DictToString(IDictionary<string, object> d)
		{
			if (d == null) return "{}";
			return "{ " + string.Join(", ", d.Select(kv => $"({kv.Key}, {kv.Value})")) + " }";
		}
	}

	public static class CollectionExtension
	{
		public static List<T> ToList<T>(this T[] array)
		{
			var list = new List<T>(array.Length);
			list.AddRange(array);
			return list;
		}

		public static bool Contain(this int[] array, int obj) => Array.IndexOf(array, obj) >= 0;
		public static bool Exists<T>(this T[] array, Predicate<T> match) { for (int i = 0; i < array.Length; i++) if (match(array[i])) return true; return false; }
		public static T Find<T>(this T[] array, Predicate<T> match) { for (int i = 0; i < array.Length; i++) if (match(array[i])) return array[i]; return default; }
		public static T FindLast<T>(this T[] array, Predicate<T> match) { for (int i = array.Length - 1; i >= 0; i--) if (match(array[i])) return array[i]; return default; }
		public static T[] FindAll<T>(this T[] array, Predicate<T> match) { var list = new List<T>(); for (int i = 0; i < array.Length; i++) if (match(array[i])) list.Add(array[i]); return list.ToArray(); }
		public static int Count<T>(this T[] array, Predicate<T> match) { int count = 0; for (int i = 0; i < array.Length; i++) if (match(array[i])) count++; return count; }
		public static void Add<T>(this T[] array, T obj, out T[] output) { output = new T[array.Length + 1]; Array.Copy(array, output, array.Length); output[array.Length] = obj; }
		public static void AddRange<T>(this T[] array1, T[] array2, out T[] output) { output = new T[array1.Length + array2.Length]; Array.Copy(array1, 0, output, 0, array1.Length); Array.Copy(array2, 0, output, array1.Length, array2.Length); }
		public static void Remove<T>(this T[] array, T obj, out T[] output) => output = array.Where(e => !EqualityComparer<T>.Default.Equals(e, obj)).ToArray();
		public static void RemoveAll<T>(this T[] array, Predicate<T> match, out T[] output) => output = array.Where(e => !match(e)).ToArray();
		public static void RemoveAt<T>(this T[] array, int index, out T[] output)
		{
			if (index < 0 || index >= array.Length) { output = (T[])array.Clone(); return; }
			output = new T[array.Length - 1];
			int j = 0;
			for (int i = 0; i < array.Length; i++) if (i != index) output[j++] = array[i];
		}

		public static int IndexOf<T>(this T[] array, T obj) => Array.IndexOf(array, obj);
		public static int FindIndex<T>(this T[] array, Predicate<T> match) => Array.FindIndex(array, match);
		public static List<T> RemoveNull<T>(this List<T> list) where T : class { list.RemoveAll(item => item == null); return list; }
		public static void RemoveDuplicated<T>(this List<T> list) { var seen = new HashSet<T>(); list.RemoveAll(item => !seen.Add(item)); }
		public static void Swap<T>(this List<T> list, int index1, int index2) { if (list == null || list.Count <= 1 || index1 == index2 || index1 < 0 || index1 >= list.Count || index2 < 0 || index2 >= list.Count) return; (list[index1], list[index2]) = (list[index2], list[index1]); }
		public static bool ContainsKey<T>(this List<KeyValuePair<int, T>> list, int key) => list.Exists(kvp => kvp.Key == key);
		public static void Remove<T>(this List<KeyValuePair<int, T>> list, int key) => list.RemoveAll(kvp => kvp.Key == key);
		public static void Add(this Dictionary<int, int> source, Dictionary<int, int> dict) { foreach (var (key, value) in dict) { source.TryGetValue(key, out int current); source[key] = current + value; } }
		public static void MinusAndRemove(this Dictionary<int, int> source, Dictionary<int, int> dict) { var removed = new List<int>(); foreach (var (key, value) in dict) if (source.TryGetValue(key, out int current)) { source[key] = current - value; if (source[key] <= 0) removed.Add(key); } foreach (var key in removed) source.Remove(key); }
		public static void AddOrSet<K, V>(this Dictionary<K, V> source, K key, V val) => source[key] = val;
		public static void Remove<K, V>(this Dictionary<K, V> source, List<K> keys) { foreach (var key in keys) source.Remove(key); }
		public static TKey RandomKey<TKey, TValue>(this Dictionary<TKey, TValue> source) { var keys = source.Keys.ToArray(); return keys[Random.Range(0, keys.Length)]; }
		public static List<KeyValuePair<TKey, TValue>> ToKeyValuePairs<TKey, TValue>(this Dictionary<TKey, TValue> source) => source.ToList();
		public static TKey RandomKeyHasLowestValue<TKey, TValue>(this Dictionary<TKey, TValue> source) where TValue : IComparable<TValue> { if (source.Count == 0) return default; var min = source.Values.Min(); var keys = source.Where(kvp => kvp.Value.CompareTo(min) == 0).Select(kvp => kvp.Key).ToList(); return keys[Random.Range(0, keys.Count)]; }
		public static TKey RandomKey<TKey, TValue>(this Dictionary<TKey, TValue> source, TValue minVal) where TValue : IComparable<TValue> { var keys = source.Where(item => item.Value.CompareTo(minVal) < 0).Select(item => item.Key).ToList(); return keys.Count > 0 ? keys[Random.Range(0, keys.Count)] : default; }
		public static void Shuffle<T>(this T[] array) { for (int n = array.Length; n > 1;) { n--; int k = UnityEngine.Random.Range(0, n + 1); (array[k], array[n]) = (array[n], array[k]); } }
		public static void Shuffle<T>(this List<T> list) { for (int n = list.Count; n > 1;) { n--; int k = UnityEngine.Random.Range(0, n + 1); (list[k], list[n]) = (list[n], list[k]); } }
	}
}
