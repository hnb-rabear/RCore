using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RevCore
{
	public static class StringHelper
	{
		private static readonly Regex s_numRegex = new(@"[-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?", RegexOptions.IgnorePatternWhitespace);

		public static void SeparateStringAndNum(string str, out string numberPart, out string stringPart)
		{
			numberPart = s_numRegex.Match(str).ToString();
			stringPart = str.Replace(numberPart, "");
		}

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

		public static string JoinString(string separator, params string[] strs) => string.Join(separator, Array.FindAll(strs, s => !string.IsNullOrEmpty(s)));
		public static void Reverse(StringBuilder sb) { int end = sb.Length - 1; int start = 0; while (end - start > 0) { (sb[end], sb[start]) = (sb[start], sb[end]); start++; end--; } }
		public static string GetFirstWords(string text, int length) { string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); int count = Math.Min(words.Length, length); return string.Join(" ", words, 0, count); }
		public static string GetFirstLine(string text) { int index = text.IndexOfAny(new[] { '\n', '\r' }); return index >= 0 ? text.Substring(0, index) : text; }
		public static string GetFirstSentence(string text) { int index = text.IndexOfAny(new[] { '.', '?', '!', '\n', '\r' }); return index >= 0 ? text.Substring(0, index + 1) : text; }
		public static int GetStableHashCode(string str) { unchecked { int hash1 = 5381; int hash2 = hash1; for (int i = 0; i < str.Length; i += 2) { hash1 = ((hash1 << 5) + hash1) ^ str[i]; if (i == str.Length - 1) break; hash2 = ((hash2 << 5) + hash2) ^ str[i + 1]; } return hash1 + hash2 * 1566083941; } }
	}

	public static class StringExtension
	{
		private static readonly Regex s_sentenceCaseRegex = new(@"(^|\.\s+)([a-z])");
		private static readonly Regex s_specialCharRegex = new("[^a-zA-Z0-9_.]+");

		public static string ToSentenceCase(this string str) => string.IsNullOrEmpty(str) ? str : s_sentenceCaseRegex.Replace(str.ToLower(), m => m.Value.ToUpper());
		public static string ToCapitalizeEachWord(this string str) => string.IsNullOrEmpty(str) ? str : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
		public static string ToLowerCaseFirstChar(this string str) => string.IsNullOrEmpty(str) ? str : char.ToLowerInvariant(str[0]) + str.Substring(1);
		public static string RemoveSpecialCharacters(this string str, string replace = "") => s_specialCharRegex.Replace(str, replace);
	}
}
