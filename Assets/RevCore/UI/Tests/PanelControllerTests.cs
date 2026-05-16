using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RevCore.UI.Tests
{
    [TestFixture]
    public class PanelControllerTests
    {
        private static readonly FieldInfo s_sessionShowCountsField = typeof(PanelController)
            .GetField("s_sessionShowCounts", BindingFlags.NonPublic | BindingFlags.Static);

        private GameObject m_rootObject;
        private TestPanelRoot m_root;
        private GameObject m_panelObject;
        private TestPanelController m_panel;

        [SetUp]
        public void SetUp()
        {
            // PanelController.s_sessionShowCounts is a process-lifetime static dictionary.
            // Earlier tests in this fixture push m_panel through Show(), which increments
            // the counter; without resetting, SessionShowCount_starts_at_zero observes the
            // leak. Reset via reflection — no test-only public API needed.
            ((IDictionary)s_sessionShowCountsField.GetValue(null)).Clear();

            m_rootObject = new GameObject("panel-root-test");
            m_root = m_rootObject.AddComponent<TestPanelRoot>();

            m_panelObject = new GameObject("panel-test");
            m_panelObject.transform.SetParent(m_rootObject.transform);
            m_panel = m_panelObject.AddComponent<TestPanelController>();
            m_panelObject.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_panelObject);
            UnityEngine.Object.DestroyImmediate(m_rootObject);
        }

        [Test]
        public void PushPanel_sets_parent_and_adds_to_stack()
        {
            m_root.PushPanelToTop(ref m_panel);

            Assert.AreSame(m_panel, m_root.TopPanel);
            Assert.AreSame(m_root, m_panel.ParentPanel);
            Assert.AreEqual(1, m_root.StackCount);
        }

        [Test]
        public void Lock_prevents_pop()
        {
            m_root.PushPanelToTop(ref m_panel);
            m_panel.Lock(true);

            bool canPop = m_panel.CanPop(out var blockingPanel);

            Assert.IsFalse(canPop);
            Assert.AreSame(m_panel, blockingPanel);
        }

        [Test]
        public void SessionShowCount_starts_at_zero()
        {
            Assert.AreEqual(0, m_panel.SessionShowCount);
        }

        private class TestPanelRoot : PanelRoot
        {
            protected override PanelController OnReceivedPanelRequest(Type panelType, object value)
            {
                return null;
            }
        }

        private class TestPanelController : PanelController { }
    }
}
