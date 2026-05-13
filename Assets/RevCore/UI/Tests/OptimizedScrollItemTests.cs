using NUnit.Framework;
using UnityEngine;

namespace RevCore.UI.Tests
{
    [TestFixture]
    public class OptimizedScrollItemTests
    {
        private GameObject m_gameObject;
        private TestScrollItem m_item;

        [SetUp]
        public void SetUp()
        {
            m_gameObject = new GameObject("scroll-item-test");
            m_item = m_gameObject.AddComponent<TestScrollItem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_gameObject);
        }

        [Test]
        public void UpdateContent_changes_index_and_defers_refresh()
        {
            bool changed = m_item.UpdateContent(3);

            Assert.IsTrue(changed);
            Assert.AreEqual(3, m_item.Index);
            Assert.AreEqual(0, m_item.UpdateCount);

            m_item.ManualUpdate();

            Assert.AreEqual(1, m_item.UpdateCount);
        }

        [Test]
        public void UpdateContent_returns_false_when_same_index_without_force()
        {
            m_item.UpdateContent(2);
            m_item.ManualUpdate();

            bool changed = m_item.UpdateContent(2);
            m_item.ManualUpdate();

            Assert.IsFalse(changed);
            Assert.AreEqual(1, m_item.UpdateCount);
        }

        [Test]
        public void Refresh_defers_next_manual_update()
        {
            m_item.UpdateContent(1);
            m_item.ManualUpdate();

            m_item.Refresh();
            m_item.ManualUpdate();

            Assert.AreEqual(2, m_item.UpdateCount);
        }

        private class TestScrollItem : OptimizedScrollItem
        {
            public int UpdateCount { get; private set; }

            protected override void OnUpdateContent()
            {
                UpdateCount++;
            }
        }
    }
}
