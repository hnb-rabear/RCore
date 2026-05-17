using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RevCore
{
	/// <summary>Static helpers for collection types.</summary>
	public static class CollectionHelper
	{
		/// <summary>Renders a string dictionary in <c>{ (key, value), ... }</c> form. Null input returns <c>"{}"</c>.</summary>
		public static string DictToString(IDictionary<string, object> d)
		{
			if (d == null) return "{}";
			return "{ " + string.Join(", ", d.Select(kv => $"({kv.Key}, {kv.Value})")) + " }";
		}
	}

	/// <summary>Extension methods on arrays, lists, and dictionaries.</summary>
	public static class CollectionExtension
	{
		/// <summary>Allocates a new <see cref="List{T}"/> populated with the array's contents.</summary>
		public static List<T> ToList<T>(this T[] array)
		{
			var list = new List<T>(array.Length);
			list.AddRange(array);
			return list;
		}

		/// <summary>Returns <c>true</c> if <paramref name="array"/> contains <paramref name="obj"/>.</summary>
		public static bool Contain(this int[] array, int obj) => Array.IndexOf(array, obj) >= 0;

		/// <summary>Returns <c>true</c> if any element of <paramref name="array"/> satisfies <paramref name="match"/>.</summary>
		public static bool Exists<T>(this T[] array, Predicate<T> match) { for (int i = 0; i < array.Length; i++) if (match(array[i])) return true; return false; }

		/// <summary>Returns the first element satisfying <paramref name="match"/>, or <c>default</c> if none.</summary>
		public static T Find<T>(this T[] array, Predicate<T> match) { for (int i = 0; i < array.Length; i++) if (match(array[i])) return array[i]; return default; }

		/// <summary>Returns the last element satisfying <paramref name="match"/>, or <c>default</c> if none.</summary>
		public static T FindLast<T>(this T[] array, Predicate<T> match) { for (int i = array.Length - 1; i >= 0; i--) if (match(array[i])) return array[i]; return default; }

		/// <summary>Returns a new array containing every element of <paramref name="array"/> that satisfies <paramref name="match"/>.</summary>
		public static T[] FindAll<T>(this T[] array, Predicate<T> match) { var list = new List<T>(); for (int i = 0; i < array.Length; i++) if (match(array[i])) list.Add(array[i]); return list.ToArray(); }

		/// <summary>Counts the elements of <paramref name="array"/> that satisfy <paramref name="match"/>.</summary>
		public static int Count<T>(this T[] array, Predicate<T> match) { int count = 0; for (int i = 0; i < array.Length; i++) if (match(array[i])) count++; return count; }

		/// <summary>Returns (via <paramref name="output"/>) a new array consisting of <paramref name="array"/> followed by <paramref name="obj"/>.</summary>
		public static void Add<T>(this T[] array, T obj, out T[] output) { output = new T[array.Length + 1]; Array.Copy(array, output, array.Length); output[array.Length] = obj; }

		/// <summary>Returns (via <paramref name="output"/>) a new array consisting of <paramref name="array1"/> followed by <paramref name="array2"/>.</summary>
		public static void AddRange<T>(this T[] array1, T[] array2, out T[] output) { output = new T[array1.Length + array2.Length]; Array.Copy(array1, 0, output, 0, array1.Length); Array.Copy(array2, 0, output, array1.Length, array2.Length); }

		/// <summary>Returns (via <paramref name="output"/>) a new array with every occurrence of <paramref name="obj"/> removed (using default equality).</summary>
		public static void Remove<T>(this T[] array, T obj, out T[] output) => output = array.Where(e => !EqualityComparer<T>.Default.Equals(e, obj)).ToArray();

		/// <summary>Returns (via <paramref name="output"/>) a new array with every element satisfying <paramref name="match"/> removed.</summary>
		public static void RemoveAll<T>(this T[] array, Predicate<T> match, out T[] output) => output = array.Where(e => !match(e)).ToArray();

		/// <summary>Returns (via <paramref name="output"/>) a new array with the element at <paramref name="index"/> removed. Out-of-range indices yield a clone of the input.</summary>
		public static void RemoveAt<T>(this T[] array, int index, out T[] output)
		{
			if (index < 0 || index >= array.Length) { output = (T[])array.Clone(); return; }
			output = new T[array.Length - 1];
			int j = 0;
			for (int i = 0; i < array.Length; i++) if (i != index) output[j++] = array[i];
		}

		/// <summary>Returns the index of the first occurrence of <paramref name="obj"/>, or -1 if not present.</summary>
		public static int IndexOf<T>(this T[] array, T obj) => Array.IndexOf(array, obj);

		/// <summary>Returns the index of the first element satisfying <paramref name="match"/>, or -1 if none.</summary>
		public static int FindIndex<T>(this T[] array, Predicate<T> match) => Array.FindIndex(array, match);

		/// <summary>Removes every <c>null</c> reference from <paramref name="list"/> in place and returns it for chaining.</summary>
		public static List<T> RemoveNull<T>(this List<T> list) where T : class { list.RemoveAll(item => item == null); return list; }

		/// <summary>Removes duplicate entries from <paramref name="list"/> in place, keeping the first occurrence of each. Order preserved.</summary>
		public static void RemoveDuplicated<T>(this List<T> list) { var seen = new HashSet<T>(); list.RemoveAll(item => !seen.Add(item)); }

		/// <summary>Swaps the elements at <paramref name="index1"/> and <paramref name="index2"/>. No-op if the list is null/empty, the indices are equal, or either is out of range.</summary>
		public static void Swap<T>(this List<T> list, int index1, int index2) { if (list == null || list.Count <= 1 || index1 == index2 || index1 < 0 || index1 >= list.Count || index2 < 0 || index2 >= list.Count) return; (list[index1], list[index2]) = (list[index2], list[index1]); }

		/// <summary>Returns <c>true</c> if any KVP in <paramref name="list"/> has the given <paramref name="key"/>.</summary>
		public static bool ContainsKey<T>(this List<KeyValuePair<int, T>> list, int key) => list.Exists(kvp => kvp.Key == key);

		/// <summary>Removes every KVP in <paramref name="list"/> whose key equals <paramref name="key"/>.</summary>
		public static void Remove<T>(this List<KeyValuePair<int, T>> list, int key) => list.RemoveAll(kvp => kvp.Key == key);

		/// <summary>For each key in <paramref name="dict"/>, adds the value to the matching entry in <paramref name="source"/>. Missing keys are inserted with the value.</summary>
		public static void Add(this Dictionary<int, int> source, Dictionary<int, int> dict) { foreach (var (key, value) in dict) { source.TryGetValue(key, out int current); source[key] = current + value; } }

		/// <summary>Subtracts each value in <paramref name="dict"/> from the matching entry in <paramref name="source"/>, then removes any entry whose result is <c>&lt;= 0</c>.</summary>
		public static void MinusAndRemove(this Dictionary<int, int> source, Dictionary<int, int> dict) { var removed = new List<int>(); foreach (var (key, value) in dict) if (source.TryGetValue(key, out int current)) { source[key] = current - value; if (source[key] <= 0) removed.Add(key); } foreach (var key in removed) source.Remove(key); }

		/// <summary>Sets <paramref name="key"/> to <paramref name="val"/>, adding or overwriting.</summary>
		public static void AddOrSet<K, V>(this Dictionary<K, V> source, K key, V val) => source[key] = val;

		/// <summary>Removes every key in <paramref name="keys"/> from <paramref name="source"/>. Missing keys are ignored.</summary>
		public static void Remove<K, V>(this Dictionary<K, V> source, List<K> keys) { foreach (var key in keys) source.Remove(key); }

		/// <summary>Returns a uniformly-random key from the dictionary. Allocates a temporary array. Behavior on empty dict is undefined.</summary>
		public static TKey RandomKey<TKey, TValue>(this Dictionary<TKey, TValue> source) { var keys = source.Keys.ToArray(); return keys[Random.Range(0, keys.Length)]; }

		/// <summary>Materializes the dictionary into a list of key-value pairs.</summary>
		public static List<KeyValuePair<TKey, TValue>> ToKeyValuePairs<TKey, TValue>(this Dictionary<TKey, TValue> source) => source.ToList();

		/// <summary>
		/// Returns a random key whose value equals the minimum value in the dictionary. Returns <c>default</c>
		/// when the dictionary is empty.
		/// </summary>
		public static TKey RandomKeyHasLowestValue<TKey, TValue>(this Dictionary<TKey, TValue> source) where TValue : IComparable<TValue> { if (source.Count == 0) return default; var min = source.Values.Min(); var keys = source.Where(kvp => kvp.Value.CompareTo(min) == 0).Select(kvp => kvp.Key).ToList(); return keys[Random.Range(0, keys.Count)]; }

		/// <summary>Returns a random key whose value is strictly less than <paramref name="minVal"/>, or <c>default</c> if none qualify.</summary>
		public static TKey RandomKey<TKey, TValue>(this Dictionary<TKey, TValue> source, TValue minVal) where TValue : IComparable<TValue> { var keys = source.Where(item => item.Value.CompareTo(minVal) < 0).Select(item => item.Key).ToList(); return keys.Count > 0 ? keys[Random.Range(0, keys.Count)] : default; }

		/// <summary>Fisher–Yates shuffle, in place. Uses <see cref="Random.Range(int, int)"/>.</summary>
		public static void Shuffle<T>(this T[] array) { for (int n = array.Length; n > 1;) { n--; int k = UnityEngine.Random.Range(0, n + 1); (array[k], array[n]) = (array[n], array[k]); } }

		/// <summary>Fisher–Yates shuffle, in place. Uses <see cref="Random.Range(int, int)"/>.</summary>
		public static void Shuffle<T>(this List<T> list) { for (int n = list.Count; n > 1;) { n--; int k = UnityEngine.Random.Range(0, n + 1); (list[k], list[n]) = (list[n], list[k]); } }

		/// <summary>Adds <paramref name="item"/> only if not already present. Returns <c>true</c> when the add happened.</summary>
		public static bool TryAdd<T>(this List<T> list, T item) { if (list.Contains(item)) return false; list.Add(item); return true; }

		/// <summary>Adds the key-value pair only if the key is not already present. Returns <c>true</c> when the add happened.</summary>
		public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value) { if (dict.ContainsKey(key)) return false; dict.Add(key, value); return true; }
	}
}
