using NUnit.Framework;

namespace RevCore.Tests
{
	public class InspectorAttributeTests
	{
		[Test]
		public void ReadOnly_attribute_creates()
		{
			var attr = new ReadOnlyAttribute();
			Assert.IsNotNull(attr);
		}

		[Test]
		public void Separator_stores_title()
		{
			var attr = new SeparatorAttribute("Section");
			Assert.AreEqual("Section", attr.title);
		}

		[Test]
		public void Separator_default_empty_title()
		{
			var attr = new SeparatorAttribute();
			Assert.AreEqual("", attr.title);
		}

		[Test]
		public void Comment_stores_content()
		{
			var attr = new CommentAttribute("Help text");
			Assert.AreEqual("Help text", attr.content.text);
		}

		[Test]
		public void Comment_with_tooltip()
		{
			var attr = new CommentAttribute("Note", "Detail");
			Assert.IsTrue(attr.content.text.Contains("[?]"));
			Assert.AreEqual("Detail", attr.content.tooltip);
		}

		[Test]
		public void ShowIf_stores_condition()
		{
			var attr = new ShowIfAttribute("isEnabled");
			Assert.AreEqual("isEnabled", attr.ConditionMemberName);
		}

		[Test]
		public void AutoFill_default_empty_path()
		{
			var attr = new AutoFillAttribute();
			Assert.AreEqual("", attr.Path);
		}

		[Test]
		public void AutoFill_stores_path()
		{
			var attr = new AutoFillAttribute("Child/Renderer");
			Assert.AreEqual("Child/Renderer", attr.Path);
		}

		[Test]
		public void DisplayEnum_stores_type()
		{
			var attr = new DisplayEnumAttribute(typeof(System.DayOfWeek));
			Assert.AreEqual(typeof(System.DayOfWeek), attr.EnumType);
		}

		[Test]
		public void DisplayEnum_rejects_non_enum()
		{
			Assert.Throws<System.ArgumentException>(() => new DisplayEnumAttribute(typeof(int)));
		}

		[Test]
		public void DisplayEnum_stores_method_name()
		{
			var attr = new DisplayEnumAttribute("GetEnumType");
			Assert.AreEqual("GetEnumType", attr.MethodName);
		}

		[Test]
		public void TagSelector_default_use_default_drawer_false()
		{
			var attr = new TagSelectorAttribute();
			Assert.IsFalse(attr.UseDefaultTagFieldDrawer);
		}

		[Test]
		public void SpriteBox_default_size_36()
		{
			var attr = new SpriteBoxAttribute();
			Assert.AreEqual(36f, attr.width);
			Assert.AreEqual(36f, attr.height);
		}

		[Test]
		public void SpriteBox_custom_size()
		{
			var attr = new SpriteBoxAttribute(64f, 128f);
			Assert.AreEqual(64f, attr.width);
			Assert.AreEqual(128f, attr.height);
		}

		[Test]
		public void InspectorButton_stores_label()
		{
			var attr = new InspectorButtonAttribute("Do Thing");
			Assert.AreEqual("Do Thing", attr.Label);
		}

		[Test]
		public void InspectorButton_default_null_label()
		{
			var attr = new InspectorButtonAttribute();
			Assert.IsNull(attr.Label);
		}

		[Test]
		public void Highlight_creates()
		{
			var attr = new HighlightAttribute();
			Assert.IsNotNull(attr);
		}

		[Test]
		public void SingleLayer_creates()
		{
			var attr = new SingleLayerAttribute();
			Assert.IsNotNull(attr);
		}

		[Test]
		public void FolderPath_creates()
		{
			var attr = new FolderPathAttribute();
			Assert.IsNotNull(attr);
		}

		[Test]
		public void CreateScriptableObject_creates()
		{
			var attr = new CreateScriptableObjectAttribute();
			Assert.IsNotNull(attr);
		}

		[Test]
		public void ExposeScriptableObject_creates()
		{
			var attr = new ExposeScriptableObjectAttribute();
			Assert.IsNotNull(attr);
		}
	}
}
