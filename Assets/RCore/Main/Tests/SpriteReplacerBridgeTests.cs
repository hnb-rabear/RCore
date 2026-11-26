using NUnit.Framework;
using RCore.Editor.Tool;
using UnityEngine;

namespace RCore.Editor.Tests
{
	public class SpriteReplacerBridgeTests
	{
		[Test]
		public void bridge_finds_the_atlas_migration_window_entry()
		{
			// The RCore repo contains the package, so the window must find its entry point.
			var method = SpriteReplacer.FindAtlasMigration();

			Assert.IsNotNull(method, "RCore.AtlasMigration.Editor / RCore.Editor.Tool.AtlasMigration.WriteTpsForWindow not found");
			Assert.AreEqual(typeof(string[]), method.ReturnType);
			CollectionAssert.AreEqual(new[] { typeof(string), typeof(Sprite[]), typeof(string) },
				System.Array.ConvertAll(method.GetParameters(), p => p.ParameterType));
		}
	}
}
