using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RCore.Editor.Tool
{
	/// <summary>Edits a TexturePacker .tps file as XML text. Keeps every byte outside the edited values.</summary>
	internal sealed class TpsFile
	{
		private readonly XDocument m_doc;
		private readonly string m_declaration;
		private readonly string m_newLine;
		private readonly string m_tail;

		/// <summary>True after an edit changed a value.</summary>
		public bool Changed { get; private set; }

		private TpsFile(XDocument pDoc, string pDeclaration, string pNewLine, string pTail)
		{
			m_doc = pDoc;
			m_declaration = pDeclaration;
			m_newLine = pNewLine;
			m_tail = pTail;
		}

		/// <summary>Throws <see cref="XmlException"/> when the text is not XML.</summary>
		public static TpsFile Parse(string pText)
		{
			string newLine = pText.Contains("\r\n") ? "\r\n" : "\n";
			var doc = XDocument.Parse(pText, LoadOptions.PreserveWhitespace);
			int rootStart = pText.IndexOf("<data", StringComparison.Ordinal);
			string declaration = rootStart > 0 ? pText.Substring(0, rootStart) : "";
			string tail = pText.Substring(pText.LastIndexOf('>') + 1);
			return new TpsFile(doc, declaration, newLine, tail);
		}

		public string ToText()
		{
			var settings = new XmlWriterSettings
			{
				OmitXmlDeclaration = true,
				Encoding = new UTF8Encoding(false),
				NewLineHandling = NewLineHandling.Replace, // the parser turned CRLF into LF; write the file's own newline back
				NewLineChars = m_newLine,
			};
			var sb = new StringBuilder();
			using (var writer = XmlWriter.Create(new StringWriter(sb), settings))
				m_doc.Root.WriteTo(writer);
			return m_declaration + sb.ToString().Replace(" />", "/>") + m_tail;
		}

		private XElement SpriteMap
		{
			get
			{
				var label = m_doc.Root?.Element("struct")?.Elements("key").FirstOrDefault(k => k.Value == "individualSpriteSettings");
				return label?.ElementsAfterSelf().FirstOrDefault() is XElement map && map.Name == "map" ? map : null;
			}
		}

		public IReadOnlyList<string> Keys
			=> SpriteMap?.Elements("key").Select(k => k.Value).ToList() ?? new List<string>();

		/// <summary>
		/// The key for a source file. Without <paramref name="pTpsDir"/>, the one key the asset path ends with.
		/// With it, only a key that resolves against the .tps folder to the same file counts (TexturePacker writes keys relative to the .tps folder,
		/// e.g. "../Loose/ui/a.png", and ignores any other). No such key: the error starts with "no key" and names the keys that only match by suffix.
		/// </summary>
		public static string FindKey(IReadOnlyList<string> pKeys, string pAssetPath, out string pError, string pTpsDir = null)
		{
			string path = "/" + pAssetPath.Replace('\\', '/');
			var suffixHits = pKeys.Where(k => path.EndsWith("/" + k, StringComparison.Ordinal)).ToList();
			if (pTpsDir == null)
			{
				pError = suffixHits.Count == 0 ? $"no key in .tps ends {pAssetPath}"
					: suffixHits.Count > 1 ? $"ambiguous: {string.Join(", ", suffixHits)}"
					: null;
				return suffixHits.Count == 1 ? suffixHits[0] : null;
			}
			string full = Path.GetFullPath(pAssetPath);
			var hits = pKeys.Where(k => string.Equals(Path.GetFullPath(Path.Combine(pTpsDir, k)), full, StringComparison.OrdinalIgnoreCase)).ToList();
			pError = hits.Count > 1 ? $"ambiguous: {string.Join(", ", hits)}"
				: hits.Count == 1 ? null
				: suffixHits.Count == 0 ? $"no key in .tps points to {pAssetPath}"
				: $"no key in .tps points to {pAssetPath} (ignored by TexturePacker: {string.Join(", ", suffixHits)})";
			return hits.Count == 1 ? hits[0] : null;
		}

		/// <summary>True when a fileList entry (a folder packed recursively, or a single file; relative to the .tps folder) covers the file. True when the .tps has no fileList.</summary>
		public bool Packs(string pTpsDir, string pAssetPath)
		{
			var label = m_doc.Root?.Element("struct")?.Elements("key").FirstOrDefault(k => k.Value == "fileList");
			if (!(label?.ElementsAfterSelf().FirstOrDefault() is XElement list) || list.Name != "array")
				return true;
			string full = Path.GetFullPath(pAssetPath);
			foreach (var entry in list.Elements("filename"))
			{
				if (string.IsNullOrWhiteSpace(entry.Value))
					continue;
				string folder = Path.GetFullPath(Path.Combine(pTpsDir, entry.Value.Trim())).TrimEnd('\\', '/');
				if (string.Equals(full, folder, StringComparison.OrdinalIgnoreCase)
					|| full.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		/// <summary>The key TexturePacker writes for a source file: its path relative to the .tps folder, with '/'.</summary>
		public static string RelativeKey(string pTpsDir, string pAssetPath)
		{
			// ponytail: Uri-based; a path with '#' or on another drive gives a wrong key. Path.GetRelativePath once every Unity profile has it.
			var from = new Uri(Path.GetFullPath(pTpsDir).TrimEnd('\\', '/') + "/");
			var to = new Uri(Path.GetFullPath(pAssetPath));
			return Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString()).Replace('\\', '/');
		}

		/// <summary>Adds a default settings entry for a source file the .tps has no entry for. False when the key exists or there is no sprite map.</summary>
		public bool AddKey(string pKey, int pWidth, int pHeight)
		{
			var map = SpriteMap;
			if (map == null || KeyElement(pKey) != null)
				return false;
			string mapIndent = (map.PreviousNode as XText)?.Value ?? "\n        ";
			string indent = mapIndent + "    ";
			string inner = indent + "    ";
			string rect = DefaultScale9Rect(pWidth, pHeight);
			var settings = new XElement("struct", new XAttribute("type", "IndividualSpriteSettings"));
			foreach (var (name, value) in new[]
			{
				("pivotPoint", new XElement("point_f", "0.5,0.5")),
				("spriteScale", new XElement("double", "1")),
				("scale9Enabled", new XElement("false")),
				("scale9Borders", new XElement("rect", rect)),
				("scale9Paddings", new XElement("rect", rect)),
				("scale9FromFile", new XElement("false")),
			})
				settings.Add(new XText(inner), new XElement("key", name), new XText(inner), value);
			settings.Add(new XText(indent));
			var key = new XElement("key", new XAttribute("type", "filename"), pKey);
			var last = map.Elements().LastOrDefault();
			if (last != null)
				last.AddAfterSelf(new XText(indent), key, new XText(indent), settings);
			else
			{
				map.RemoveNodes(); // a map with only whitespace would leave a blank line
				map.Add(new XText(indent), key, new XText(indent), settings, new XText(mapIndent));
			}
			Changed = true;
			return true;
		}

		/// <summary>TexturePacker's default 9-slice rect for a new entry: offset (n+2)/4 per axis, size n - 2*offset (n/2 when n is even).</summary>
		internal static string DefaultScale9Rect(int pWidth, int pHeight)
		{
			int x = (pWidth + 2) / 4, y = (pHeight + 2) / 4;
			int w = pWidth % 2 == 0 ? pWidth / 2 : pWidth - 2 * x;
			int h = pHeight % 2 == 0 ? pHeight / 2 : pHeight - 2 * y;
			return FormattableString.Invariant($"{x},{y},{w},{h}");
		}

		/// <summary>Fallback for the window, where Old sprites may not be the .tps source files: the one key whose file name is the sprite name.</summary>
		public static string FindKeyByName(IReadOnlyList<string> pKeys, string pSpriteName, out string pError)
		{
			var hits = pKeys.Where(k => Path.GetFileNameWithoutExtension(k) == pSpriteName).ToList();
			pError = hits.Count == 0 ? $"no key in .tps named {pSpriteName}"
				: hits.Count > 1 ? $"ambiguous: {string.Join(", ", hits)}"
				: null;
			return hits.Count == 1 ? hits[0] : null;
		}

		public static string ToScale9Rect(int pLeft, int pBottom, int pRight, int pTop, int pWidth, int pHeight)
			=> FormattableString.Invariant($"{pLeft},{pTop},{pWidth - pLeft - pRight},{pHeight - pTop - pBottom}");

		public bool SetPivot(string pKey, float pX, float pY)
		{
			string value = FormattableString.Invariant($"{Round(pX)},{Round(1 - pY)}"); // TexturePacker y goes down
			return Set(pKey, ("pivotPoint", "point_f", value));
		}

		public bool SetBorder(string pKey, int pLeft, int pBottom, int pRight, int pTop, int pWidth, int pHeight)
		{
			string rect = ToScale9Rect(pLeft, pBottom, pRight, pTop, pWidth, pHeight);
			return Set(pKey, ("scale9Enabled", "true", null), ("scale9Borders", "rect", rect), ("scale9Paddings", "rect", rect));
		}

		/// <summary>Sets values in the key's own struct. Splits a shared struct only when a value really changes.</summary>
		private bool Set(string pKey, params (string key, string element, string value)[] pValues)
		{
			var current = SettingsOf(pKey);
			if (current == null)
				return false;
			if (pValues.Any(v => !current.Elements("key").Any(k => k.Value == v.key)))
				return false; // a setting the struct lacks cannot be written; report it, do not pretend
			if (pValues.All(v => IsSame(current, v.key, v.element, v.value)))
				return true;
			var own = OwnSettings(pKey);
			foreach (var v in pValues)
				SetValue(own, v.key, v.element, v.value);
			return true;
		}

		private static bool IsSame(XElement pSettings, string pKey, string pElement, string pValue)
		{
			var value = pSettings.Elements("key").FirstOrDefault(k => k.Value == pKey)?.ElementsAfterSelf().FirstOrDefault();
			return value == null || (value.Name == pElement && (pValue == null || value.Value == pValue));
		}

		public bool EnsureWritePivotPoints()
		{
			var global = m_doc.Root?.Element("struct")?.Elements("key").FirstOrDefault(k => k.Value == "globalSpriteSettings")
				?.ElementsAfterSelf().FirstOrDefault();
			var value = global?.Elements("key").FirstOrDefault(k => k.Value == "writePivotPoints")?.ElementsAfterSelf().FirstOrDefault();
			if (value == null || value.Name == "true")
				return false;
			value.ReplaceWith(new XElement("true"));
			Changed = true;
			return true;
		}

		public string ValueOf(string pKey, string pSetting)
		{
			var settings = SettingsOf(pKey);
			return settings?.Elements("key").FirstOrDefault(k => k.Value == pSetting)?.ElementsAfterSelf().FirstOrDefault()?.Value;
		}

		private static float Round(float pValue) => (float)Math.Round(pValue, 4, MidpointRounding.AwayFromZero);

		private XElement KeyElement(string pKey) => SpriteMap?.Elements("key").FirstOrDefault(k => k.Value == pKey);

		/// <summary>The struct a key uses: the first struct after it.</summary>
		private XElement SettingsOf(string pKey) => KeyElement(pKey)?.ElementsAfterSelf("struct").FirstOrDefault();

		/// <summary>The settings struct only this key uses. Splits a shared struct first. Null when the key or its struct is missing.</summary>
		private XElement OwnSettings(string pKey)
		{
			var key = KeyElement(pKey);
			var shared = key?.ElementsAfterSelf("struct").FirstOrDefault();
			if (shared == null)
				return null;
			var run = shared.ElementsBeforeSelf().Reverse().TakeWhile(e => e.Name == "key").ToList();
			if (run.Count == 1)
				return shared;

			// Move the key behind the shared struct and give it its own copy. The rest of the run keeps the original.
			var leading = key.PreviousNode as XText;
			string indent = leading?.Value ?? "";
			key.Remove();
			leading?.Remove();
			var copy = new XElement(shared);
			shared.AddAfterSelf(new XText(indent), key, new XText(indent), copy);
			Changed = true;
			return copy;
		}

		private void SetValue(XElement pSettings, string pKey, string pElement, string pValue)
		{
			var value = pSettings.Elements("key").FirstOrDefault(k => k.Value == pKey)?.ElementsAfterSelf().FirstOrDefault();
			if (value == null)
				return;
			if (value.Name == pElement && (pValue == null || value.Value == pValue))
				return;
			value.ReplaceWith(pValue == null ? new XElement(pElement) : new XElement(pElement, pValue));
			Changed = true;
		}
	}
}
