using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI.Tests
{
    [TestFixture]
    public class JustButtonTests
    {
        private GameObject m_gameObject;
        private TestJustButton m_button;

        [SetUp]
        public void SetUp()
        {
            m_gameObject = new GameObject("button-test", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            m_button = m_gameObject.AddComponent<TestJustButton>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_gameObject);
        }

        [Test]
        public void GetGreyMat_returns_null_when_no_material_assigned()
        {
            Assert.IsNull(m_button.GetGreyMat());
        }

        [Test]
        public void SetEnable_with_no_material_does_not_throw()
        {
            m_button.greyscaleEffect = true;
            Assert.DoesNotThrow(() => m_button.SetEnable(false));
        }

        [Test]
        public void SetEnable_false_with_greyscale_applies_material()
        {
            var mat = new Material(Shader.Find("UI/Default"));
            m_button.SetGreyscaleMaterial(mat);

            m_button.greyscaleEffect = true;
            m_button.SetEnable(false);

            Assert.AreSame(mat, m_button.Img.material);
            Object.DestroyImmediate(mat);
        }

        private class TestJustButton : JustButton
        {
            public void SetGreyscaleMaterial(Material mat)
            {
                m_greyscaleMaterial = mat;
            }
        }
    }
}
