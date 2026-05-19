using NUnit.Framework;
using RevCore;
using UnityEngine;

namespace RevCore.Tests
{
	public class ComponentRefTests
	{
		[Test]
		public void HasType_returns_true_for_assignable_subclass()
		{
			var componentRef = new ComponentRef<Renderer>("00000000000000000000000000000000");

			Assert.IsTrue(componentRef.HasType(typeof(MeshRenderer)));
			Assert.IsFalse(componentRef.HasType(typeof(Collider)));
		}

		[Test]
		public void ValidateAsset_returns_false_for_GameObject_missing_component()
		{
			var componentRef = new ComponentRef<Rigidbody>("00000000000000000000000000000000");
			var go = new GameObject();

			try
			{
				Assert.IsFalse(componentRef.ValidateAsset(go));
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}
	}
}