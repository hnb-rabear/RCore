using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    [TestFixture]
    public class JObjectDBTests
    {
        private const string Prefix = "revcore_test_data_";

        [SetUp]
        public void SetUp()
        {
            // ClearInMemoryForTests is internal to RevCore.Data.Runtime; visible to this test
            // assembly via [InternalsVisibleTo("RevCore.Data.Tests")] on the runtime side.
            JObjectDB.ClearInMemoryForTests();
            PlayerPrefs.DeleteKey(Prefix + "score");
        }

        [TearDown]
        public void TearDown()
        {
            JObjectDB.Delete(Prefix + "score");
            PlayerPrefs.DeleteKey("JObjectDB");
        }

        [Test]
        public void CreateCollection_returns_new_instance()
        {
            var col = JObjectDB.CreateCollection<TestData>(Prefix + "score");
            Assert.IsNotNull(col);
            Assert.AreEqual(Prefix + "score", col.key);
        }

        [Test]
        public void CreateCollection_loads_existing_data()
        {
            PlayerPrefs.SetString(Prefix + "score", "{\"value\":42}");
            var col = JObjectDB.CreateCollection<TestData>(Prefix + "score");
            Assert.AreEqual(42, col.value);
        }

        [Test]
        public void Save_persists_to_PlayerPrefs()
        {
            var col = JObjectDB.CreateCollection<TestData>(Prefix + "score");
            col.value = 99;
            col.Save();
            string json = PlayerPrefs.GetString(Prefix + "score");
            Assert.IsTrue(json.Contains("99"));
        }

        [Test]
        public void Delete_removes_key_from_registry()
        {
            JObjectDB.CreateCollection<TestData>(Prefix + "score");
            JObjectDB.Delete(Prefix + "score");
            Assert.IsNull(JObjectDB.GetCollection(Prefix + "score"));
        }

        [Test]
        public void Reset_clears_loaded_data_to_defaults_but_keeps_key()
        {
            var col = JObjectDB.CreateCollection<TestData>(Prefix + "score");
            col.value = 55;
            col.Save();

            JObjectDB.Reset(Prefix + "score");

            // Key stays registered, live instance resets to the type default, data is emptied.
            Assert.IsTrue(JObjectDB.GetCollectionKeys().Contains(Prefix + "score"));
            Assert.AreEqual(0, col.value);
            Assert.AreEqual("{}", PlayerPrefs.GetString(Prefix + "score"));
        }

        [Test]
        public void Reset_empties_unloaded_collection_and_keeps_key()
        {
            // Register the key in the persisted index, then drop the in-memory instance.
            JObjectDB.CreateCollection<TestData>(Prefix + "score").Save();
            JObjectDB.ClearInMemoryForTests();

            JObjectDB.Reset(Prefix + "score");

            Assert.IsTrue(JObjectDB.GetCollectionKeys().Contains(Prefix + "score"));
            Assert.AreEqual("{}", PlayerPrefs.GetString(Prefix + "score"));
        }

        [Test]
        public void Reset_ignores_unknown_key()
        {
            // No throw, and nothing is persisted for a key that was never registered.
            Assert.DoesNotThrow(() => JObjectDB.Reset(Prefix + "missing"));
            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "missing"));
        }

        [Test]
        public void GetCollectionKeys_returns_registered_keys()
        {
            JObjectDB.CreateCollection<TestData>(Prefix + "score");
            var keys = JObjectDB.GetCollectionKeys();
            Assert.IsTrue(keys.Contains(Prefix + "score"));
        }

        [Test]
        public void Import_overwrites_data()
        {
            var col = JObjectDB.CreateCollection<TestData>(Prefix + "score");
            col.value = 1;
            string importJson = $"{{\"{Prefix}score\":\"{{\\\"value\\\":77}}\"}}";
            JObjectDB.Import(importJson);
            Assert.AreEqual(77, col.value);
        }

        [System.Serializable]
        private class TestData : JObjectData
        {
            public int value;
        }
    }

    [TestFixture]
    public class JObjectDataTests
    {
        private const string Key = "revcore_test_jdata_";

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(Key + "item");

        [Test]
        public void ToJson_default_uses_JsonUtility()
        {
            var data = new SimpleData { key = Key + "item", count = 5 };
            string json = data.ToJson();
            Assert.IsTrue(json.Contains("5"));
        }

        [Test]
        public void Load_overwrite_from_string()
        {
            var data = new SimpleData { key = Key + "item" };
            bool result = data.Load("{\"count\":123}");
            Assert.IsTrue(result);
            Assert.AreEqual(123, data.count);
        }

        [Test]
        public void Save_then_Load_roundtrip()
        {
            var data = new SimpleData { key = Key + "item", count = 77 };
            data.Save();
            var data2 = new SimpleData { key = Key + "item" };
            Assert.IsTrue(data2.Load());
            Assert.AreEqual(77, data2.count);
        }

        [System.Serializable]
        private class SimpleData : JObjectData
        {
            public int count;
        }
    }
}
