/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class RowTypeMatchTests
	{
		public class SimpleRow
		{
			public int Id { get; set; }
		}

		public class TwoMemberRow
		{
			public int Id { get; set; }
			public string Description { get; set; }
		}

		public class CaseCollisionRow
		{
			public int id { get; set; }
			public int ID { get; set; }
		}

		public class CaseFallbackRow
		{
			public int TotalScore { get; set; }
		}

		public class RenamedRow
		{
			[JsonProperty("custom_id")]
			public int OriginalId { get; set; }
		}

		public class RejectedDestinationsRow
		{
			public int ValidProp { get; set; }
			[JsonIgnore]
			public int IgnoredProp { get; set; }
			public int ReadOnlyProp { get; }
			private int m_privateField;

			public int GetPrivateField() => m_privateField;
		}

		/// <summary>
		/// A get-only collection Newtonsoft populates in place under the default ObjectCreationHandling.
		/// The contract reports it unwritable, so a writability-only check would call it unmatched and
		/// warn about data being discarded that the deserializer actually keeps.
		/// </summary>
		public class PopulatableCollectionRow
		{
			public int Id { get; set; }
			public List<int> Tags { get; } = new List<int>();
		}

		/// <summary>
		/// Members named so a snake_case naming strategy maps the exported keys onto them. Under the
		/// default contract the serialized names are the PascalCase ones, so this only lines up when the
		/// consumer's own resolver is honoured.
		/// </summary>
		public class SnakeCaseRow
		{
			public int Id { get; set; }
			public List<int> BotIds { get; set; }
		}

		/// <summary>Read by a global converter in one of the tests, so the whole row type is opaque.</summary>
		public class ConverterTargetRow
		{
			public int Id { get; set; }
		}

		private sealed class ConverterTargetRowConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType) => objectType == typeof(ConverterTargetRow);

			public override object ReadJson(
				JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				JObject.Load(reader);
				return new ConverterTargetRow();
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
				=> writer.WriteNull();
		}

		public class BaseRow
		{
			public int BaseId { get; set; }
		}

		public class DerivedRow : BaseRow
		{
			public string Name { get; set; }
		}

		public class ExtensionDataRow
		{
			public int Id { get; set; }

			[JsonExtensionData]
			public IDictionary<string, JToken> Extra { get; set; }
		}

		public struct StructRow
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		public class JsonConstructorRow
		{
			public int Id { get; set; }

			[JsonConstructor]
			public JsonConstructorRow(int id, int legacyFlag)
			{
				Id = id;
			}
		}

		public class DummyConverter : JsonConverter
		{
			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();
			public override bool CanConvert(Type objectType) => true;
		}

		[JsonConverter(typeof(DummyConverter))]
		public class CustomConverterRow
		{
			public int Id { get; set; }
		}

		[Test]
		public void unmatched_key_is_detected()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Id", "ExtraKey" }, typeof(SimpleRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.HasUnmatched, Is.True);
			Assert.That(match.UnmatchedJsonMembers, Is.EqualTo(new[] { "ExtraKey" }));
			Assert.That(match.UnobservedClassMembers, Is.Empty);
		}

		[Test]
		public void absent_class_member_is_informational_only()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Id" }, typeof(TwoMemberRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.HasUnmatched, Is.False);
			Assert.That(match.UnmatchedJsonMembers, Is.Empty);
			Assert.That(match.UnobservedClassMembers, Is.EqualTo(new[] { "Description" }));
		}

		[Test]
		public void exact_then_case_insensitive_matching()
		{
			var matchExact = SheetXRowTypeMatch.Compare(new[] { "ID" }, typeof(CaseCollisionRow));
			Assert.That(matchExact.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(matchExact.HasUnmatched, Is.False);
			Assert.That(matchExact.UnobservedClassMembers, Is.EqualTo(new[] { "id" }));

			var matchFallback = SheetXRowTypeMatch.Compare(new[] { "totalscore" }, typeof(CaseFallbackRow));
			Assert.That(matchFallback.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(matchFallback.HasUnmatched, Is.False);
			Assert.That(matchFallback.UnobservedClassMembers, Is.Empty);
		}

		[Test]
		public void json_property_rename_honored()
		{
			var matchValid = SheetXRowTypeMatch.Compare(new[] { "custom_id" }, typeof(RenamedRow));
			Assert.That(matchValid.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(matchValid.HasUnmatched, Is.False);
			Assert.That(matchValid.UnobservedClassMembers, Is.Empty);

			var matchOld = SheetXRowTypeMatch.Compare(new[] { "OriginalId" }, typeof(RenamedRow));
			Assert.That(matchOld.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(matchOld.HasUnmatched, Is.True);
			Assert.That(matchOld.UnmatchedJsonMembers, Is.EqualTo(new[] { "OriginalId" }));
			Assert.That(matchOld.UnobservedClassMembers, Is.EqualTo(new[] { "custom_id" }));
		}

		[Test]
		public void json_ignore_read_only_and_private_field_rejected_as_destinations()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "ValidProp", "IgnoredProp", "ReadOnlyProp", "m_privateField" }, typeof(RejectedDestinationsRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.HasUnmatched, Is.True);
			Assert.That(match.UnmatchedJsonMembers, Is.EqualTo(new[] { "IgnoredProp", "ReadOnlyProp", "m_privateField" }));
			Assert.That(match.UnobservedClassMembers, Is.Empty);
		}

		#region Following the real deserializer

		[Test]
		public void a_get_only_collection_the_deserializer_populates_is_not_reported_as_unmatched()
		{
			// Prove what the deserializer actually does first, so the expectation is not an assumption.
			var loaded = JsonConvert.DeserializeObject<PopulatableCollectionRow>("{\"Id\":1,\"Tags\":[4,7]}");
			Assert.That(loaded.Tags, Is.EqualTo(new[] { 4, 7 }),
				"Fixture check: Newtonsoft populates a get-only collection in place.");

			var match = SheetXRowTypeMatch.Compare(new[] { "Id", "Tags" }, typeof(PopulatableCollectionRow));

			Assert.That(match.UnmatchedJsonMembers, Does.Not.Contain("Tags"),
				"The deserializer keeps this data, so warning that it is discarded would be a false alarm.");
		}

		[Test]
		public void a_get_only_scalar_that_cannot_receive_a_value_stays_unmatched()
		{
			// The other side of the same rule: this one provably cannot be populated, so it is still a
			// real destination failure and must not be softened.
			var match = SheetXRowTypeMatch.Compare(new[] { "ReadOnlyProp" }, typeof(RejectedDestinationsRow));

			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.UnmatchedJsonMembers, Is.EqualTo(new[] { "ReadOnlyProp" }));
		}

		[Test]
		public void a_consumer_snake_case_resolver_is_honoured_rather_than_reported_unmatched()
		{
			var original = JsonConvert.DefaultSettings;
			try
			{
				JsonConvert.DefaultSettings = () => new JsonSerializerSettings
				{
					ContractResolver = new DefaultContractResolver
					{
						NamingStrategy = new SnakeCaseNamingStrategy(),
					},
				};

				// Prove the deserializer accepts these keys under the consumer's global settings.
				var loaded = JsonConvert.DeserializeObject<SnakeCaseRow>("{\"id\":1,\"bot_ids\":[7,7]}");
				Assert.That(loaded.BotIds, Is.EqualTo(new[] { 7, 7 }), "Fixture check: snake_case keys bind.");

				var match = SheetXRowTypeMatch.Compare(new[] { "id", "bot_ids" }, typeof(SnakeCaseRow));

				Assert.That(match.HasUnmatched, Is.False,
					"A false unmatched here blocks an export whose data is not being dropped.");
			}
			finally
			{
				JsonConvert.DefaultSettings = original;
			}
		}

		[Test]
		public void default_settings_changed_after_an_earlier_compare_still_take_effect()
		{
			var original = JsonConvert.DefaultSettings;
			try
			{
				// A statically captured resolver would freeze whatever was in force at first use. The
				// settings are a mutable global, so they are read per comparison.
				JsonConvert.DefaultSettings = null;
				var before = SheetXRowTypeMatch.Compare(new[] { "bot_ids" }, typeof(SnakeCaseRow));
				Assert.That(before.UnmatchedJsonMembers, Is.EqualTo(new[] { "bot_ids" }),
					"Fixture check: under the default contract this key has no destination.");

				JsonConvert.DefaultSettings = () => new JsonSerializerSettings
				{
					ContractResolver = new DefaultContractResolver
					{
						NamingStrategy = new SnakeCaseNamingStrategy(),
					},
				};

				var after = SheetXRowTypeMatch.Compare(new[] { "bot_ids" }, typeof(SnakeCaseRow));

				Assert.That(after.HasUnmatched, Is.False,
					"The comparison read settings captured before they changed.");
			}
			finally
			{
				JsonConvert.DefaultSettings = original;
			}
		}

		[Test]
		public void a_global_reading_converter_makes_coverage_unverifiable()
		{
			var original = JsonConvert.DefaultSettings;
			try
			{
				JsonConvert.DefaultSettings = () => new JsonSerializerSettings
				{
					Converters = new List<JsonConverter> { new ConverterTargetRowConverter() },
				};

				// The converter reads the whole object, so which keys it accepts is its own business and
				// cannot be read off a contract. Saying nothing is honest; guessing is not.
				var match = SheetXRowTypeMatch.Compare(new[] { "Id", "anything" }, typeof(ConverterTargetRow));

				Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Unverifiable));
				Assert.That(match.HasUnmatched, Is.False, "An unverifiable contract must not accuse a member.");
			}
			finally
			{
				JsonConvert.DefaultSettings = original;
			}
		}

		#endregion

		[Test]
		public void inherited_member_is_accepted()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "BaseId", "Name" }, typeof(DerivedRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.HasUnmatched, Is.False);
			Assert.That(match.UnmatchedJsonMembers, Is.Empty);
			Assert.That(match.UnobservedClassMembers, Is.Empty);
		}

		[Test]
		public void json_extension_data_yields_unverifiable()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Id", "Extra" }, typeof(ExtensionDataRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Unverifiable));
			Assert.That(match.Note, Does.StartWith("Member coverage cannot be verified"));
			Assert.That(match.HasUnmatched, Is.False);
		}

		[Test]
		public void struct_type_works()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Id", "Name" }, typeof(StructRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.HasUnmatched, Is.False);
			Assert.That(match.UnmatchedJsonMembers, Is.Empty);
			Assert.That(match.UnobservedClassMembers, Is.Empty);
		}

		[Test]
		public void json_constructor_parameter_named_legacyFlag_is_accepted()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Id", "legacyFlag" }, typeof(JsonConstructorRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.HasUnmatched, Is.False);
			Assert.That(match.UnmatchedJsonMembers, Is.Empty);
			Assert.That(match.UnobservedClassMembers, Is.Empty);

			var matchOmitted = SheetXRowTypeMatch.Compare(new[] { "Id" }, typeof(JsonConstructorRow));
			Assert.That(matchOmitted.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(matchOmitted.UnobservedClassMembers, Is.EqualTo(new[] { "legacyFlag" }));
		}

		[Test]
		public void custom_json_converter_yields_unverifiable()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Id" }, typeof(CustomConverterRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Unverifiable));
			Assert.That(match.Note, Does.StartWith("Member coverage cannot be verified"));
		}

		[Test]
		public void compare_json_empty_array_is_empty_data()
		{
			var match = SheetXRowTypeMatch.CompareJson("[]", typeof(SimpleRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.EmptyData));
			Assert.That(match.HasUnmatched, Is.False);
			Assert.That(match.UnmatchedJsonMembers, Is.Empty);
			Assert.That(match.UnobservedClassMembers, Is.Empty);
		}

		[Test]
		public void malformed_compare_json_is_unverifiable()
		{
			var matchInvalid = SheetXRowTypeMatch.CompareJson("not json", typeof(SimpleRow));
			Assert.That(matchInvalid.State, Is.EqualTo(SheetXRowTypeMatchState.Unverifiable));
			Assert.That(matchInvalid.Note, Is.Not.Null.And.Not.Empty);

			var matchNonArray = SheetXRowTypeMatch.CompareJson("{\"id\":1}", typeof(SimpleRow));
			Assert.That(matchNonArray.State, Is.EqualTo(SheetXRowTypeMatchState.Unverifiable));
			Assert.That(matchNonArray.Note, Is.Not.Null.And.Not.Empty);
		}

		[Test]
		public void null_row_type_is_unverifiable()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Id" }, null);
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Unverifiable));
			Assert.That(match.Note, Does.StartWith("Member coverage cannot be verified"));
		}

		[Test]
		public void empty_member_list_is_empty_data_without_class_complaints()
		{
			var match = SheetXRowTypeMatch.Compare(Array.Empty<string>(), typeof(TwoMemberRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.EmptyData));
			Assert.That(match.HasUnmatched, Is.False);
			Assert.That(match.UnmatchedJsonMembers, Is.Empty);
			Assert.That(match.UnobservedClassMembers, Is.Empty);
		}

		[Test]
		public void compare_json_unions_keys_in_first_seen_order()
		{
			var match = SheetXRowTypeMatch.CompareJson("[{\"b\":2},{\"a\":1,\"c\":3}]", typeof(SimpleRow));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(match.UnmatchedJsonMembers, Is.EqualTo(new[] { "b", "a", "c" }));
		}

		[Test]
		public void non_object_contract_type_is_unverifiable()
		{
			var match = SheetXRowTypeMatch.Compare(new[] { "Length" }, typeof(int[]));
			Assert.That(match.State, Is.EqualTo(SheetXRowTypeMatchState.Unverifiable));
			Assert.That(match.Note, Does.StartWith("Member coverage cannot be verified"));
		}
	}
}
