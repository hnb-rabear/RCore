using System;
using System.Collections.Generic;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXRowTypeTests
	{
		[Serializable, SheetXBindable]
		private sealed class ValidRow
		{
			public int id;
		}

		[Serializable, SheetXBindable]
		private struct ValidStructRow
		{
			public int id;
		}

		[Serializable]
		private sealed class SerializableOnlyRow
		{
			public int id;
		}

		[SheetXBindable]
		private sealed class BindableOnlyRow
		{
			public int id;
		}

		[Serializable, SheetXBindable]
		private abstract class AbstractRow
		{
			public int id;
		}

		[Serializable, SheetXBindable]
		private sealed class GenericRow<T>
		{
			public T value;
		}

		// [SheetXBindable] cannot be applied here: AttributeUsage lists Class | Struct only,
		// and C# rejects an enum target (CS0592). A bare enum still exercises the rejection.
		private enum BindableEnum
		{
			None = 0,
		}

		[Test]
		public void valid_class_with_both_attributes_passes()
		{
			bool ok = SheetXRowType.Validate(typeof(ValidRow), out string error);

			Assert.That(ok, Is.True, error);
			Assert.That(error, Is.Null);
		}

		[Test]
		public void valid_struct_with_both_attributes_passes()
		{
			bool ok = SheetXRowType.Validate(typeof(ValidStructRow), out string error);

			Assert.That(ok, Is.True, error);
			Assert.That(error, Is.Null);
		}

		[Test]
		public void null_type_fails_without_throwing()
		{
			bool ok = SheetXRowType.Validate(null, out string error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("was not found"));
		}

		[Test]
		public void type_missing_bindable_attribute_fails_naming_the_attribute()
		{
			bool ok = SheetXRowType.Validate(typeof(SerializableOnlyRow), out string error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("SheetXBindable"));
			Assert.That(error, Does.Contain("SerializableOnlyRow"));
		}

		[Test]
		public void type_missing_serializable_attribute_fails_naming_serializable()
		{
			bool ok = SheetXRowType.Validate(typeof(BindableOnlyRow), out string error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("Serializable"));
			Assert.That(error, Does.Contain("BindableOnlyRow"));
		}

		[Test]
		public void abstract_class_fails()
		{
			bool ok = SheetXRowType.Validate(typeof(AbstractRow), out string error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("abstract"));
		}

		[Test]
		public void generic_type_definition_fails()
		{
			bool ok = SheetXRowType.Validate(typeof(GenericRow<>), out string error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("generic"));
		}

		[Test]
		public void closed_generic_type_fails()
		{
			bool ok = SheetXRowType.Validate(typeof(GenericRow<int>), out string error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("generic"));
		}

		[Test]
		public void enum_fails()
		{
			bool ok = SheetXRowType.Validate(typeof(BindableEnum), out string error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("BindableEnum"));
		}

		[Test]
		public void list_of_valid_types_filters_to_the_valid_ones()
		{
			var candidates = new List<Type>
			{
				typeof(ValidRow), typeof(ValidStructRow), typeof(SerializableOnlyRow),
				typeof(BindableOnlyRow), typeof(AbstractRow), typeof(BindableEnum),
			};

			var accepted = candidates.FindAll(type => SheetXRowType.Validate(type, out _));

			Assert.That(accepted, Is.EquivalentTo(new[] { typeof(ValidRow), typeof(ValidStructRow) }));
		}
	}
}
