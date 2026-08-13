using System.Text;

namespace RCore.Editor
{
	/// <summary>
	/// Pure, dependency-free search helper for the Asset Grid search box.
	///
	/// Expected behavior (kept here as living documentation, verify manually in Unity Editor):
	///   Normalize("IconStarChest")        == "icon star chest"
	///   Normalize("UIIconStar")            == "ui icon star"
	///   Normalize("icon_star-chest.png")   == "icon star chest png"
	///   Matches("icon star chest", "star chest") == true
	///   Matches("icon star chest", "icon chest")  == true
	///   Matches("icon star chest", "chest star")  == true
	///   Matches("icon star chest", "star sword")  == false
	///   Matches("icon star chest", "   ")          == true
	/// </summary>
	internal static class AssetSearchFilter
	{
		/// <summary>
		/// Normalizes source text (identifiers, file names, paths) into a lowercase,
		/// space-separated token string. Separator characters (_ - . / \) become spaces,
		/// and word boundaries are inferred from case transitions (camelCase / PascalCase /
		/// acronym runs), e.g. "IconStarChest" -> "icon star chest", "UIIconStar" -> "ui icon star".
		/// </summary>
		internal static string Normalize(string pText)
		{
			if (string.IsNullOrEmpty(pText))
				return string.Empty;

			var builder = new StringBuilder(pText.Length + 8);
			for (int i = 0; i < pText.Length; i++)
			{
				var current = pText[i];
				var previous = i > 0 ? pText[i - 1] : '\0';
				var next = i + 1 < pText.Length ? pText[i + 1] : '\0';

				if (IsSeparator(current))
				{
					AppendSpace(builder);
					continue;
				}

				if (NeedsWordBreak(previous, current, next))
					AppendSpace(builder);

				builder.Append(char.ToLowerInvariant(current));
			}
			return builder.ToString().Trim();
		}

		internal sealed class Query
		{
			internal readonly string[] terms;

			internal Query(string[] pTerms)
			{
				terms = pTerms ?? new string[0];
			}
		}

		internal static Query ParseQuery(string pQuery)
		{
			var normalizedQuery = Normalize(pQuery);
			if (string.IsNullOrEmpty(normalizedQuery))
				return new Query(new string[0]);

			return new Query(normalizedQuery.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries));
		}

		/// <summary>
		/// Returns true when every whitespace-separated term of the normalized query is found
		/// (in any order) inside the already-normalized source text. An empty/whitespace-only
		/// query always matches (no active filter).
		/// </summary>
		internal static bool Matches(string pNormalizedText, Query pQuery)
		{
			if (pQuery == null || pQuery.terms.Length == 0)
				return true;

			pNormalizedText = pNormalizedText ?? string.Empty;
			foreach (var term in pQuery.terms)
			{
				if (pNormalizedText.IndexOf(term, System.StringComparison.Ordinal) < 0)
					return false;
			}
			return true;
		}

		internal static bool Matches(string pNormalizedText, string pQuery)
		{
			return Matches(pNormalizedText, ParseQuery(pQuery));
		}

		private static bool IsSeparator(char pCharacter)
		{
			return pCharacter == '_' || pCharacter == '-' || pCharacter == '.' ||
				pCharacter == '/' || pCharacter == '\\';
		}

		private static void AppendSpace(StringBuilder pBuilder)
		{
			if (pBuilder.Length > 0 && pBuilder[pBuilder.Length - 1] != ' ')
				pBuilder.Append(' ');
		}

		private static bool NeedsWordBreak(char pPrevious, char pCurrent, char pNext)
		{
			if (pPrevious == '\0' || IsSeparator(pPrevious) || char.IsWhiteSpace(pPrevious))
				return false;

			return char.IsLower(pPrevious) && char.IsUpper(pCurrent) ||
				char.IsUpper(pPrevious) && char.IsUpper(pCurrent) && char.IsLower(pNext);
		}
	}
}
