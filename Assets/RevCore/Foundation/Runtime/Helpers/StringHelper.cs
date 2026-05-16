using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RevCore
{
	/// <summary>Static string utility methods.</summary>
	public static class StringHelper
	{
		private static readonly Regex s_numRegex = new(@"[-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?", RegexOptions.IgnorePatternWhitespace);

		/// <summary>
		/// Splits a string into the first numeric substring and the remaining non-numeric text.
		/// Useful for "Level 12" → ("12", "Level ") style parses.
		/// </summary>
		/// <param name="str">Input string. May contain at most one numeric run (only the first match is extracted).</param>
		/// <param name="numberPart">The first numeric substring found, or empty string if none.</param>
		/// <param name="stringPart">The input with the matched number removed.</param>
		public static void SeparateStringAndNum(string str, out string numberPart, out string stringPart)
		{
			numberPart = s_numRegex.Match(str).ToString();
			stringPart = str.Replace(numberPart, "");
		}

		/// <summary>
		/// Converts <c>"PlayerName"</c>, <c>"player-name"</c>, or <c>"player name"</c> to <c>"player_name"</c>.
		/// Uppercase boundaries, dashes, and spaces all become underscore separators.
		/// </summary>
		/// <param name="input">Input string. Null or empty returns <see cref="string.Empty"/>.</param>
		/// <returns>The lowercase underscore form.</returns>
		public static string ToLowerUnderscore(string input)
		{
			if (string.IsNullOrEmpty(input)) return string.Empty;
			var result = new StringBuilder(input.Length * 2);
			char prevChar = input[0];
			result.Append(char.ToLower(prevChar));
			for (int i = 1; i < input.Length; i++)
			{
				char currentChar = input[i];
				if ((char.IsUpper(currentChar) || currentChar == ' ' || currentChar == '-') && prevChar != ' ' && prevChar != '-') result.Append('_');
				result.Append(char.ToLower(currentChar));
				prevChar = currentChar;
			}
			return result.ToString().Replace("__", "_");
		}

		/// <summary>Joins the non-empty entries of <paramref name="strs"/> with <paramref name="separator"/>. Empty/null entries are skipped.</summary>
		public static string JoinString(string separator, params string[] strs) => string.Join(separator, Array.FindAll(strs, s => !string.IsNullOrEmpty(s)));

		/// <summary>Reverses the contents of the given <see cref="StringBuilder"/> in place.</summary>
		public static void Reverse(StringBuilder sb) { int end = sb.Length - 1; int start = 0; while (end - start > 0) { (sb[end], sb[start]) = (sb[start], sb[end]); start++; end--; } }

		/// <summary>Returns the first <paramref name="length"/> whitespace-separated words from <paramref name="text"/>, or the full string if it has fewer words.</summary>
		public static string GetFirstWords(string text, int length) { string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); int count = Math.Min(words.Length, length); return string.Join(" ", words, 0, count); }

		/// <summary>Returns the substring up to (but not including) the first newline character. Returns the full string if no newline is found.</summary>
		public static string GetFirstLine(string text) { int index = text.IndexOfAny(new[] { '\n', '\r' }); return index >= 0 ? text.Substring(0, index) : text; }

		/// <summary>Returns the substring up to and including the first sentence-terminating character (<c>.</c>, <c>?</c>, <c>!</c>, or newline).</summary>
		public static string GetFirstSentence(string text) { int index = text.IndexOfAny(new[] { '.', '?', '!', '\n', '\r' }); return index >= 0 ? text.Substring(0, index + 1) : text; }

		/// <summary>
		/// Computes a process-stable hash code for <paramref name="str"/>. Unlike <see cref="string.GetHashCode"/>,
		/// this value is consistent across .NET runtimes and process restarts — suitable for persisted IDs.
		/// </summary>
		public static int GetStableHashCode(string str) { unchecked { int hash1 = 5381; int hash2 = hash1; for (int i = 0; i < str.Length; i += 2) { hash1 = ((hash1 << 5) + hash1) ^ str[i]; if (i == str.Length - 1) break; hash2 = ((hash2 << 5) + hash2) ^ str[i + 1]; } return hash1 + hash2 * 1566083941; } }
	}

	/// <summary>Extension methods on <see cref="string"/>.</summary>
	public static class StringExtension
	{
		private static readonly Regex s_sentenceCaseRegex = new(@"(^|\.\s+)([a-z])");
		private static readonly Regex s_specialCharRegex = new("[^a-zA-Z0-9_.]+");

		/// <summary>Lower-cases the input, then capitalizes the first letter of each sentence (after <c>.&#32;</c>).</summary>
		public static string ToSentenceCase(this string str) => string.IsNullOrEmpty(str) ? str : s_sentenceCaseRegex.Replace(str.ToLower(), m => m.Value.ToUpper());

		/// <summary>Lower-cases the input, then capitalizes the first letter of each word using the current culture.</summary>
		public static string ToCapitalizeEachWord(this string str) => string.IsNullOrEmpty(str) ? str : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());

		/// <summary>Returns a copy of the input with the first character lower-cased; other characters unchanged.</summary>
		public static string ToLowerCaseFirstChar(this string str) => string.IsNullOrEmpty(str) ? str : char.ToLowerInvariant(str[0]) + str.Substring(1);

		/// <summary>
		/// Removes any character that is not alphanumeric, underscore, or period. Optionally replaces removed
		/// characters with <paramref name="replace"/>. Useful for sanitizing keys/filenames.
		/// </summary>
		public static string RemoveSpecialCharacters(this string str, string replace = "") => s_specialCharRegex.Replace(str, replace);
	}
}
