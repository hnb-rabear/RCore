using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    public class PlayerPrefTests
    {
        private const string Prefix = "revcore_prefs_tests_";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(Prefix + "bool");
            PlayerPrefs.DeleteKey(Prefix + "int");
            PlayerPrefs.DeleteKey(Prefix + "float");
            PlayerPrefs.DeleteKey(Prefix + "string");
        }

        [TearDown]
        public void TearDown()
        {
            SetUp();
            PlayerPrefContainer.ClearRegistered();
        }

        [Test]
        public void Bool_pref_saves_changed_value()
        {
            var pref = new PlayerPrefBool(Prefix + "bool");
            pref.Value = true;

            PlayerPrefContainer.SaveChanges();

            Assert.AreEqual(1, PlayerPrefs.GetInt(Prefix + "bool"));
        }

        [Test]
        public void Int_pref_loads_default_and_saves_changed_value()
        {
            var pref = new PlayerPrefInt(Prefix + "int", 3);
            Assert.AreEqual(3, pref.Value);

            pref.Value = 7;
            PlayerPrefContainer.SaveChanges();

            Assert.AreEqual(7, PlayerPrefs.GetInt(Prefix + "int"));
        }

        [Test]
        public void Float_pref_saves_changed_value()
        {
            var pref = new PlayerPrefFloat(Prefix + "float");
            pref.Value = 1.5f;

            PlayerPrefContainer.SaveChanges();

            Assert.AreEqual(1.5f, PlayerPrefs.GetFloat(Prefix + "float"));
        }

        [Test]
        public void String_pref_delete_removes_key()
        {
            var pref = new PlayerPrefString(Prefix + "string");
            pref.Value = "saved";
            pref.SaveChange();

            pref.Delete();

            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "string"));
        }
    }
}
