using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RCore.Editor
{
	public static class AssetCatalogUsageScanner
	{
		public struct LinkerInfo
		{
			public readonly string assetType;
			public readonly string componentType;

			public LinkerInfo(string pAssetType, string pComponentType)
			{
				assetType = pAssetType;
				componentType = pComponentType;
			}
		}

		public struct Usage
		{
			public readonly string prefabPath;
			public readonly string assetType;
			public readonly string key;
			public readonly string componentType;

			public Usage(string pPrefabPath, string pAssetType, string pKey, string pComponentType)
			{
				prefabPath = pPrefabPath;
				assetType = pAssetType;
				key = pKey;
				componentType = pComponentType;
			}
		}

		public struct ScanResult
		{
			public readonly List<Usage> usages;
			public readonly bool requiresPrefabContentsScan;

			public ScanResult(List<Usage> pUsages, bool pRequiresPrefabContentsScan)
			{
				usages = pUsages;
				requiresPrefabContentsScan = pRequiresPrefabContentsScan;
			}
		}

		private static readonly Regex MonoBehaviourDocumentRegex = new Regex(
			@"^--- !u!114\b.*?(?=^--- !u!|\z)",
			RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);
		private static readonly Regex ScriptGuidRegex = new Regex(
			@"^\s*m_Script:\s*\{[^}]*\bguid:\s*([^,\s}]+)",
			RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		private static readonly Regex KeyRegex = new Regex(
			@"^\s*m_Key:\s*(.*?)\s*$",
			RegexOptions.Multiline | RegexOptions.CultureInvariant);
		private static readonly Regex NestedPrefabRegex = new Regex(
			@"^\s*m_SourcePrefab:\s*\{",
			RegexOptions.Multiline | RegexOptions.CultureInvariant);

		public static ScanResult ScanPrefabFile(
			string pPrefabPath,
			IReadOnlyDictionary<string, LinkerInfo> pLinkersByScriptGuid,
			IReadOnlyDictionary<string, HashSet<string>> pCatalogKeysByAssetType,
			out string pError)
		{
			var usages = new List<Usage>();
			pError = null;
			string content;
			try
			{
				content = File.ReadAllText(pPrefabPath);
			}
			catch (Exception ex)
			{
				pError = ex.Message;
				return new ScanResult(usages, false);
			}

			if (NestedPrefabRegex.IsMatch(content))
				return new ScanResult(usages, true);

			foreach (Match documentMatch in MonoBehaviourDocumentRegex.Matches(content))
			{
				var document = documentMatch.Value;
				var scriptGuidMatch = ScriptGuidRegex.Match(document);
				if (!scriptGuidMatch.Success || !pLinkersByScriptGuid.TryGetValue(scriptGuidMatch.Groups[1].Value, out var linker))
					continue;

				var keyMatch = KeyRegex.Match(document);
				if (!keyMatch.Success)
					continue;

				var key = ParseYamlScalar(keyMatch.Groups[1].Value);
				if (string.IsNullOrEmpty(key) ||
					!pCatalogKeysByAssetType.TryGetValue(linker.assetType, out var catalogKeys) ||
					!catalogKeys.Contains(key))
					continue;

				usages.Add(new Usage(pPrefabPath, linker.assetType, key, linker.componentType));
			}

			return new ScanResult(usages, false);
		}

		private static string ParseYamlScalar(string pValue)
		{
			var value = pValue.Trim();
			if (value.Length < 2)
				return value;

			var quote = value[0];
			if ((quote != '\'' && quote != '\"') || value[value.Length - 1] != quote)
				return value;

			var unquoted = value.Substring(1, value.Length - 2);
			if (quote == '\'')
				return unquoted.Replace("''", "'");

			return Regex.Unescape(unquoted);
		}
	}
}
