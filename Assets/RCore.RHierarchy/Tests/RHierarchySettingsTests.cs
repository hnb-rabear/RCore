using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RCore.RHierarchy.Editor;
using UnityEditor;

namespace RCore.RHierarchy.Tests
{
	public class RHierarchySettingsTests
	{
		private const string ORDER_KEY = "RHierarchy_Order";

		private bool m_hadOrderKey;
		private string m_originalOrder;

		[SetUp]
		public void SetUp()
		{
			m_hadOrderKey = EditorPrefs.HasKey(ORDER_KEY);
			m_originalOrder = m_hadOrderKey ? EditorPrefs.GetString(ORDER_KEY) : null;
		}

		[TearDown]
		public void TearDown()
		{
			if (m_hadOrderKey)
				EditorPrefs.SetString(ORDER_KEY, m_originalOrder);
			else
				EditorPrefs.DeleteKey(ORDER_KEY);
			ResetCache();
		}

		[Test]
		public void component_order_round_trips_through_editor_prefs()
		{
			var expected = new List<RHierarchySettings.RComponentType>
			{
				RHierarchySettings.RComponentType.Tag,
				RHierarchySettings.RComponentType.Layer,
				RHierarchySettings.RComponentType.Visibility,
				RHierarchySettings.RComponentType.Static,
				RHierarchySettings.RComponentType.Components,
				RHierarchySettings.RComponentType.ChildrenCount,
				RHierarchySettings.RComponentType.Vertices,
			};

			RHierarchySettings.ComponentOrder = expected;
			Assert.AreEqual("Tag,Layer,Visibility,Static,Components,ChildrenCount,Vertices", EditorPrefs.GetString(ORDER_KEY));

			ResetCache();
			Assert.AreEqual(expected, RHierarchySettings.ComponentOrder);
		}

		[Test]
		public void legacy_tag_layer_entry_expands_and_missing_types_are_appended()
		{
			EditorPrefs.SetString(ORDER_KEY, "TagLayer,Visibility");
			ResetCache();

			var order = RHierarchySettings.ComponentOrder;

			Assert.AreEqual(RHierarchySettings.RComponentType.Tag, order[0]);
			Assert.AreEqual(RHierarchySettings.RComponentType.Layer, order[1]);
			Assert.AreEqual(RHierarchySettings.RComponentType.Visibility, order[2]);
			CollectionAssert.AllItemsAreUnique(order);
			Assert.AreEqual(System.Enum.GetValues(typeof(RHierarchySettings.RComponentType)).Length, order.Count);
		}

		private static void ResetCache()
		{
			typeof(RHierarchySettings)
				.GetField("m_ComponentOrder", BindingFlags.NonPublic | BindingFlags.Static)
				.SetValue(null, null);
		}
	}
}
