using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI.Tests
{
    [TestFixture]
    public class HoledLayerMaskTests
    {
        private GameObject m_gameObject;
        private HoledLayerMask m_mask;

        [SetUp]
        public void SetUp()
        {
            m_gameObject = new GameObject("mask-test", typeof(RectTransform));
            m_mask = m_gameObject.AddComponent<HoledLayerMask>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_gameObject);
        }

        [Test]
        public void CreateComponents_creates_five_child_images()
        {
            m_mask.CreateComponents();

            Assert.IsNotNull(m_mask.imgHole);
            Assert.IsNotNull(m_mask.imgLeft);
            Assert.IsNotNull(m_mask.imgRight);
            Assert.IsNotNull(m_mask.imgTop);
            Assert.IsNotNull(m_mask.imgBot);
            Assert.AreEqual(5, m_gameObject.transform.childCount);
        }

        [Test]
        public void Active_false_disables_mask_components()
        {
            m_mask.CreateComponents();
            m_mask.Active(true);
            m_mask.Active(false);

            Assert.IsFalse(m_mask.imgHole.gameObject.activeSelf);
            Assert.IsFalse(m_mask.imgLeft.gameObject.activeSelf);
            Assert.IsFalse(m_mask.imgTop.gameObject.activeSelf);
            Assert.IsFalse(m_mask.imgRight.gameObject.activeSelf);
            Assert.IsFalse(m_mask.imgBot.gameObject.activeSelf);
            Assert.AreEqual(Vector2.zero, m_mask.imgHole.rectTransform.sizeDelta);
        }

        [Test]
        public void SetColor_applies_to_four_border_images()
        {
            m_mask.CreateComponents();
            var color = new Color(0.2f, 0.3f, 0.4f, 0.8f);

            m_mask.SetColor(color);

            Assert.AreEqual(color, m_mask.imgLeft.color);
            Assert.AreEqual(color, m_mask.imgTop.color);
            Assert.AreEqual(color, m_mask.imgRight.color);
            Assert.AreEqual(color, m_mask.imgBot.color);
        }

        [Test]
        public void Active_does_not_throw_when_images_null()
        {
            Assert.DoesNotThrow(() => m_mask.Active(false));
        }
    }
}
