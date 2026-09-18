/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// State of the row type member match against JSON members.
	/// </summary>
	internal enum SheetXRowTypeMatchState
	{
		/// <summary>Comparison completed against an object contract.</summary>
		Compared,
		/// <summary>JSON or member name input was empty.</summary>
		EmptyData,
		/// <summary>Contract cannot be verified (converter, non-object, extension data, or malformed JSON).</summary>
		Unverifiable,
	}

	/// <summary>
	/// Evaluates top-level JSON member coverage against a row type's effective Newtonsoft contract.
	/// </summary>
	internal sealed class SheetXRowTypeMatch
	{
		// No cached resolver. JsonConvert.DefaultSettings is a mutable global a consuming project may set,
		// or change, at any point after this class is first touched, and CreateDefault() is exactly what
		// JsonConvert.DeserializeObject does — so building a serializer per comparison is what keeps this
		// check agreeing with the deserializer it exists to predict.
		private static JsonSerializer CreateSerializer() => JsonSerializer.CreateDefault();

		private readonly SheetXRowTypeMatchState m_state;
		private readonly IReadOnlyList<string> m_unmatchedJsonMembers;
		private readonly IReadOnlyList<string> m_unobservedClassMembers;
		private readonly string m_note;

		private SheetXRowTypeMatch(
			SheetXRowTypeMatchState state,
			IReadOnlyList<string> unmatchedJsonMembers,
			IReadOnlyList<string> unobservedClassMembers,
			string note)
		{
			m_state = state;
			m_unmatchedJsonMembers = unmatchedJsonMembers ?? Array.Empty<string>();
			m_unobservedClassMembers = unobservedClassMembers ?? Array.Empty<string>();
			m_note = note;
		}

		/// <summary>Outcome state of the comparison.</summary>
		internal SheetXRowTypeMatchState State => m_state;

		/// <summary>Top-level JSON keys that have no accepted destination in the row type.</summary>
		internal IReadOnlyList<string> UnmatchedJsonMembers => m_unmatchedJsonMembers;

		/// <summary>Accepted class destinations that did not appear in the JSON.</summary>
		internal IReadOnlyList<string> UnobservedClassMembers => m_unobservedClassMembers;

		/// <summary>Diagnostic note explaining unverifiable or abnormal state.</summary>
		internal string Note => m_note;

		/// <summary>True when one or more JSON members have no destination in the row type.</summary>
		internal bool HasUnmatched => m_unmatchedJsonMembers.Count > 0;

		/// <summary>
		/// Compares a list of JSON member names against the effective contract of <paramref name="rowType"/>.
		/// </summary>
		internal static SheetXRowTypeMatch Compare(IReadOnlyList<string> jsonMemberNames, Type rowType)
		{
			if (rowType == null)
				return Unverifiable("Member coverage cannot be verified: row type is null.");

			JsonObjectContract objectContract;
			try
			{
				var serializer = CreateSerializer();
				var contract = serializer.ContractResolver.ResolveContract(rowType);
				if (contract == null)
					return Unverifiable("Member coverage cannot be verified: failed to resolve contract.");

				if (contract.Converter != null)
					return Unverifiable("Member coverage cannot be verified: contract has a custom converter.");

				// A converter supplied through the consumer's global settings reads the whole object, so
				// which keys it accepts is its own business and cannot be read off a contract. Same answer
				// as a contract-level converter: say nothing rather than guess.
				if (FindReadingConverter(serializer, rowType) != null)
				{
					return Unverifiable(
						"Member coverage cannot be verified: a JsonConverter in the serializer settings reads this type.");
				}

				if (!(contract is JsonObjectContract objContract))
					return Unverifiable("Member coverage cannot be verified: contract is not an object contract.");

				if (objContract.ExtensionDataSetter != null || objContract.ExtensionDataGetter != null)
					return Unverifiable("Member coverage cannot be verified: type exposes extension data.");

				objectContract = objContract;
			}
			catch (Exception ex)
			{
				return Unverifiable($"Member coverage cannot be verified: {ex.Message}");
			}

			if (jsonMemberNames == null || jsonMemberNames.Count == 0)
				return new SheetXRowTypeMatch(SheetXRowTypeMatchState.EmptyData, Array.Empty<string>(), Array.Empty<string>(), null);

			try
			{
				var destinations = CollectAcceptedDestinations(objectContract, out bool undecidable);

				// One or more get-only members might be populated in place by the deserializer, and a
				// contract cannot say whether they will be without constructing the instance. Reporting
				// them as unmatched would accuse the export of dropping data it keeps, so the honest answer
				// for the whole comparison is that coverage could not be established.
				if (undecidable)
				{
					return Unverifiable(
						"Member coverage cannot be verified: this type has a read-only member the deserializer "
						+ "may populate in place, which cannot be confirmed without loading the data.");
				}

				var unmatchedList = new List<string>();
				var unmatchedSet = new HashSet<string>(StringComparer.Ordinal);

				for (int i = 0; i < jsonMemberNames.Count; i++)
				{
					string key = jsonMemberNames[i];
					if (string.IsNullOrEmpty(key))
						continue;

					var match = FindDestination(destinations, key);
					if (match != null)
					{
						match.Observed = true;
					}
					else if (unmatchedSet.Add(key))
					{
						unmatchedList.Add(key);
					}
				}

				var unobservedList = new List<string>();
				for (int i = 0; i < destinations.Count; i++)
				{
					if (!destinations[i].Observed)
						unobservedList.Add(destinations[i].SerializedName);
				}

				return new SheetXRowTypeMatch(
					SheetXRowTypeMatchState.Compared,
					unmatchedList,
					unobservedList,
					null);
			}
			catch (Exception ex)
			{
				return Unverifiable($"Member coverage cannot be verified: {ex.Message}");
			}
		}

		/// <summary>
		/// Parses a JSON array of objects, extracts top-level keys in first-seen order, and compares against <paramref name="rowType"/>.
		/// </summary>
		internal static SheetXRowTypeMatch CompareJson(string json, Type rowType)
		{
			if (rowType == null)
				return Unverifiable("Member coverage cannot be verified: row type is null.");

			if (string.IsNullOrWhiteSpace(json))
				return Unverifiable("JSON data is empty or whitespace.");

			JArray rowArray;
			try
			{
				using var input = new StringReader(json);
				using var reader = new JsonTextReader(input)
				{
					DateParseHandling = DateParseHandling.None,
					MaxDepth = 64,
				};
				var root = JToken.ReadFrom(reader, new JsonLoadSettings
				{
					DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
				});

				if (!(root is JArray rows) || rows.Any(row => !(row is JObject)))
					return Unverifiable("JSON root must be an array of objects.");

				while (reader.Read())
				{
					if (reader.TokenType == JsonToken.Comment)
						continue;
					return Unverifiable("JSON contains more than one root value.");
				}

				rowArray = rows;
			}
			catch (Exception ex)
			{
				return Unverifiable(ex.Message);
			}

			var keys = new List<string>();
			var seenKeys = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < rowArray.Count; i++)
			{
				var row = (JObject)rowArray[i];
				foreach (var prop in row.Properties())
				{
					if (seenKeys.Add(prop.Name))
						keys.Add(prop.Name);
				}
			}

			return Compare(keys, rowType);
		}

		/// <summary>
		/// Builds an unverifiable result carrying <paramref name="note"/>. Internal so a caller that rejects
		/// a row type before comparing — an ineligible type, say — reports it through this same arm instead
		/// of inventing a second "cannot verify" shape the window would have to render differently.
		/// </summary>
		/// <param name="note">Why coverage could not be established.</param>
		internal static SheetXRowTypeMatch Unverifiable(string note)
		{
			return new SheetXRowTypeMatch(
				SheetXRowTypeMatchState.Unverifiable,
				Array.Empty<string>(),
				Array.Empty<string>(),
				note);
		}

		// undecidable reports that some read-only member might still be populated in place, which only the
		// real deserializer working on real data can settle.
		private static List<AcceptedDestination> CollectAcceptedDestinations(
			JsonObjectContract objectContract, out bool undecidable)
		{
			var destinations = new List<AcceptedDestination>();
			undecidable = false;

			if (objectContract.Properties != null)
			{
				for (int i = 0; i < objectContract.Properties.Count; i++)
				{
					var prop = objectContract.Properties[i];
					if (prop.Ignored || string.IsNullOrEmpty(prop.PropertyName))
						continue;

					if (prop.Writable)
					{
						destinations.Add(new AcceptedDestination(prop.PropertyName));
						continue;
					}

					// Not writable, so the question is whether the deserializer would populate whatever the
					// getter already returns. Under the default ObjectCreationHandling it does exactly that
					// for a collection or a nested object, but only if the instance is non-null — which is a
					// property of the data, not of the type. A scalar can never be populated this way, so a
					// read-only scalar stays a real destination failure.
					if (MayBePopulatedInPlace(prop.PropertyType))
						undecidable = true;
				}
			}

			if (objectContract.CreatorParameters != null && objectContract.CreatorParameters.Count > 0)
			{
				for (int i = 0; i < objectContract.CreatorParameters.Count; i++)
				{
					var param = objectContract.CreatorParameters[i];
					if (param.Ignored || string.IsNullOrEmpty(param.PropertyName))
						continue;

					bool alreadyCovered = false;
					for (int j = 0; j < destinations.Count; j++)
					{
						if (string.Equals(destinations[j].SerializedName, param.PropertyName, StringComparison.OrdinalIgnoreCase))
						{
							alreadyCovered = true;
							break;
						}
					}

					if (!alreadyCovered)
					{
						destinations.Add(new AcceptedDestination(param.PropertyName));
					}
				}
			}

			return destinations;
		}

		// Purely a type question — nothing is constructed and no getter is invoked. A value type, a string,
		// or an array cannot be filled in place: an array's length is fixed at creation, so Newtonsoft
		// builds a new one and needs a setter to store it. Anything else is a reference type Newtonsoft may
		// populate (a collection it can Add to, or a nested object whose members it can set).
		private static bool MayBePopulatedInPlace(Type propertyType)
		{
			if (propertyType == null)
				return false;
			if (propertyType.IsValueType || propertyType == typeof(string) || propertyType.IsArray)
				return false;
			return true;
		}

		// Mirrors how the serializer picks a converter for a type: the settings' own list, in order. The
		// contract-level converter is checked separately by the caller.
		private static JsonConverter FindReadingConverter(JsonSerializer serializer, Type rowType)
		{
			if (serializer?.Converters == null)
				return null;
			for (int i = 0; i < serializer.Converters.Count; i++)
			{
				var converter = serializer.Converters[i];
				if (converter != null && converter.CanRead && converter.CanConvert(rowType))
					return converter;
			}
			return null;
		}

		private static AcceptedDestination FindDestination(List<AcceptedDestination> destinations, string key)
		{
			AcceptedDestination fallback = null;

			for (int i = 0; i < destinations.Count; i++)
			{
				var d = destinations[i];
				if (string.Equals(d.SerializedName, key, StringComparison.Ordinal))
				{
					if (!d.Observed)
						return d;
					if (fallback == null)
						fallback = d;
				}
			}

			if (fallback != null)
				return fallback;

			for (int i = 0; i < destinations.Count; i++)
			{
				var d = destinations[i];
				if (string.Equals(d.SerializedName, key, StringComparison.OrdinalIgnoreCase))
				{
					if (!d.Observed)
						return d;
					if (fallback == null)
						fallback = d;
				}
			}

			return fallback;
		}

		private sealed class AcceptedDestination
		{
			public readonly string SerializedName;
			public bool Observed;

			public AcceptedDestination(string serializedName)
			{
				SerializedName = serializedName;
			}
		}
	}
}
