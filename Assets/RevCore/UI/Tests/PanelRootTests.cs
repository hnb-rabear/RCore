using System;
using NUnit.Framework;
using UnityEngine;

namespace RevCore.UI.Tests
{
    [TestFixture]
    public class PanelRootTests
    {
        private GameObject m_rootObject;
        private TestPanelRoot m_root;

        [SetUp]
        public void SetUp()
        {
            Events.Clear();
            m_rootObject = new GameObject("panel-root-test");
            m_root = m_rootObject.AddComponent<TestPanelRoot>();
        }

        [TearDown]
        public void TearDown()
        {
            Events.Clear();
            UnityEngine.Object.DestroyImmediate(m_rootObject);
        }

        [Test]
        public void PushOuterPanel_root_mismatch_ignored()
        {
            var panelObj = new GameObject("panel");
            panelObj.transform.SetParent(m_rootObject.transform);
            var panel = panelObj.AddComponent<TestPanel>();
            panelObj.SetActive(false);

            PanelRoot.PushOuterPanel(typeof(OtherPanelRoot), panel);

            Assert.AreEqual(0, m_root.StackCount);
            UnityEngine.Object.DestroyImmediate(panelObj);
        }

        [Test]
        public void PushOuterPanel_matching_root_pushes_panel()
        {
            var panelObj = new GameObject("panel");
            panelObj.transform.SetParent(m_rootObject.transform);
            var panel = panelObj.AddComponent<TestPanel>();
            panelObj.SetActive(false);

            PanelRoot.PushOuterPanel(typeof(TestPanelRoot), panel);

            Assert.AreEqual(1, m_root.StackCount);
            Assert.AreSame(panel, m_root.TopPanel);
            UnityEngine.Object.DestroyImmediate(panelObj);
        }

        [Test]
        public void RequestPanel_passes_Type_to_handler()
        {
            m_root.RequestedType = null;

            PanelRoot.RequestPanel<PanelController>(typeof(TestPanelRoot), typeof(TestPanel), "test-value");

            Assert.AreEqual(typeof(TestPanel), m_root.RequestedType);
            Assert.AreEqual("test-value", m_root.RequestedValue);
        }

        [Test]
        public void OnResolvePanelByType_returns_null_by_default()
        {
            PanelRoot.PushInternalPanel<PanelController>(typeof(TestPanelRoot), typeof(TestPanel));

            Assert.AreEqual(0, m_root.StackCount);
        }

        private class TestPanelRoot : PanelRoot
        {
            public Type RequestedType;
            public object RequestedValue;

            protected override PanelController OnReceivedPanelRequest(Type panelType, object value)
            {
                RequestedType = panelType;
                RequestedValue = value;
                return null;
            }
        }

        private class OtherPanelRoot : PanelRoot
        {
            protected override PanelController OnReceivedPanelRequest(Type panelType, object value)
            {
                return null;
            }
        }

        private class TestPanel : PanelController { }
    }
}
