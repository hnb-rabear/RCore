using System.IO;
using NUnit.Framework;
using RCore.Editor.Tool;
using UnityEngine;

namespace RCore.Editor.Tests
{
	public class AtlasMigrationTests
	{
		private static readonly Vector2 s_size = new Vector2(99, 59);
		private static readonly Vector2 s_center = new Vector2(0.5f, 0.5f);

		private static readonly string s_tmp = Path.Combine(Path.GetTempPath(), "rcore-resolve-key");

		[Test]
		public void resolve_key_window_path_falls_back_to_suffix_rule()
		{
			string key = AtlasMigration.ResolveKey(new[] { "cars/car.png", "trucks/car.png" }, "Assets/Old/cars/car.png", "car", Path.Combine(s_tmp, "Atlas"), true, out var error, out bool add);

			Assert.AreEqual("cars/car.png", key);
			Assert.IsFalse(add);
			Assert.IsNull(error);
		}

		[Test]
		public void resolve_key_window_path_falls_back_to_name()
		{
			string key = AtlasMigration.ResolveKey(new[] { "x/car.png" }, "Assets/Old/car.png", "car", Path.Combine(s_tmp, "Atlas"), true, out var error, out bool add);

			Assert.AreEqual("x/car.png", key);
			Assert.IsFalse(add);
			Assert.IsNull(error);
		}

		[Test]
		public void resolve_key_window_path_never_adds()
		{
			string key = AtlasMigration.ResolveKey(new string[0], Path.Combine(s_tmp, "Loose", "a.png"), "a", Path.Combine(s_tmp, "Atlas"), true, out var error, out bool add);

			Assert.IsNull(key);
			Assert.IsFalse(add);
			StringAssert.StartsWith("no key", error);
		}

		[Test]
		public void resolve_key_agent_path_adds_relative_key()
		{
			string key = AtlasMigration.ResolveKey(new[] { "ui/a.png" }, Path.Combine(s_tmp, "Loose", "ui", "a.png"), "a", Path.Combine(s_tmp, "Atlas"), false, out var error, out bool add);

			Assert.AreEqual("../Loose/ui/a.png", key);
			Assert.IsTrue(add);
			StringAssert.Contains("ignored by TexturePacker: ui/a.png", error);
		}

		[Test]
		public void resolve_key_agent_path_keeps_a_resolving_key()
		{
			string key = AtlasMigration.ResolveKey(new[] { "../Loose/ui/a.png" }, Path.Combine(s_tmp, "Loose", "ui", "a.png"), "a", Path.Combine(s_tmp, "Atlas"), false, out var error, out bool add);

			Assert.AreEqual("../Loose/ui/a.png", key);
			Assert.IsFalse(add);
			Assert.IsNull(error);
		}

		[Test]
		public void resolve_key_agent_path_reports_ambiguous()
		{
			string key = AtlasMigration.ResolveKey(new[] { "../Loose/a.png", "../loose/a.png" }, Path.Combine(s_tmp, "Loose", "a.png"), "a", Path.Combine(s_tmp, "Atlas"), false, out var error, out bool add);

			Assert.IsNull(key);
			Assert.IsFalse(add);
			StringAssert.StartsWith("ambiguous", error);
		}

		[Test]
		public void status_ok_when_all_match()
		{
			Assert.AreEqual("ok", AtlasMigration.Status(s_size, s_size, s_center, new Vector2(0.505f, 0.5f), Vector4.one, new Vector4(1, 1, 1, 1.5f), true));
		}

		[Test]
		public void status_pivot_mismatch()
		{
			Assert.AreEqual("pivot mismatch", AtlasMigration.Status(s_size, s_size, s_center, new Vector2(0.3f, 0.5f), Vector4.zero, Vector4.zero, true));
		}

		[Test]
		public void status_border_mismatch()
		{
			Assert.AreEqual("border mismatch", AtlasMigration.Status(s_size, s_size, s_center, s_center, new Vector4(10, 0, 0, 0), Vector4.zero, true));
		}

		[Test]
		public void status_size_differs_ignores_border()
		{
			Assert.AreEqual("size differs", AtlasMigration.Status(s_size, s_size * 0.5f, s_center, s_center, new Vector4(10, 0, 0, 0), Vector4.zero, true));
		}

		[Test]
		public void status_size_differs_still_reports_pivot()
		{
			Assert.AreEqual("size differs, pivot mismatch", AtlasMigration.Status(s_size, s_size * 0.5f, s_center, Vector2.zero, Vector4.zero, Vector4.zero, true));
		}

		[Test]
		public void status_missing_in_atlas()
		{
			Assert.AreEqual("missing in atlas", AtlasMigration.Status(s_size, Vector2.zero, s_center, s_center, Vector4.zero, Vector4.zero, false));
		}
	}
}
