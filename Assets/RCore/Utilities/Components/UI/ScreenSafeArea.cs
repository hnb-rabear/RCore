using System.Collections;
using UnityEngine;

namespace RCore.Components
{
    public class ScreenSafeArea : MonoBehaviour
    {
        public static float topOffset;
        public Canvas canvas;
        public RectTransform[] safeRects;
        public bool fixedTop;
        public bool fixedBottom;
        private ScreenOrientation m_CurrentOrientation;
        private Rect m_CurrentSafeArea;

        private void Start()
        {
            m_CurrentOrientation = Screen.orientation;
            m_CurrentSafeArea = Screen.safeArea;
            CheckSafeArea2();
        }

        /// <summary>
        /// This method work well in simulator or device but in editor it is little buggy if Simulator is not currently active
        /// So this method for only infomation purpose. The method 2 is much better
        /// </summary>
        [InspectorButton]
        public void CheckSafeArea()
        {
            var safeArea = Screen.safeArea;
            var sWidth = Screen.currentResolution.width;
            var sHeight = Screen.currentResolution.height;
            var oWidthTop = (Screen.currentResolution.width - safeArea.width - safeArea.x) / 2f;
            var oHeightTop = (Screen.currentResolution.height - safeArea.height - safeArea.y) / 2f;
            var oWidthBot = -safeArea.x / 2f;
            var oHeightBot = -safeArea.y / 2f;
            Debug.Log($"Screen size: (width:{sWidth}, height:{sHeight})" +
                $"\nSafe area: {safeArea}" +
                $"\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})" +
                $"\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");

            var offsetTop = new Vector2(oWidthTop, oHeightTop);
            var offsetBottom = new Vector2(oWidthBot, oHeightBot);

            foreach (var rect in safeRects)
            {
                if (!fixedTop)
                    rect.offsetMax = new Vector2(0, -offsetTop.y);
                else
                    rect.offsetMax = Vector2.zero;
                if (!fixedBottom)
                    rect.offsetMin = new Vector2(0, -offsetBottom.y);
                else
                    rect.offsetMin = Vector2.zero;
            }
        }

        [InspectorButton]
        public void CheckSafeArea2()
        {
            var safeArea = Screen.safeArea;
            safeArea.height -= topOffset;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= canvas.pixelRect.width;
            anchorMin.y /= canvas.pixelRect.height;
            anchorMax.x /= canvas.pixelRect.width;
            anchorMax.y /= canvas.pixelRect.height;

            foreach (var rect in safeRects)
            {
                if (!fixedBottom)
                    rect.anchorMin = anchorMin;
                else
                    rect.anchorMin = Vector2.zero;
                if (!fixedTop)
                    rect.anchorMax = anchorMax;
                else
                    rect.anchorMax = Vector2.one;
            }

            m_CurrentOrientation = Screen.orientation;
            m_CurrentSafeArea = Screen.safeArea;
        }

        public static void SetTopOffsetForBannerAd(float pBannerHeight, bool pPlaceInSafeArea = true) //150
        {
            float offset = 0;
            var safeAreaHeightOffer = Screen.height - Screen.safeArea.height; //80
            if (!pPlaceInSafeArea)
            {
                if (pBannerHeight <= safeAreaHeightOffer)
                    offset = 0;
                else
                    offset = pBannerHeight - safeAreaHeightOffer;
            }
            else
            {
                offset = pBannerHeight;
            }

            if (topOffset == offset)
                return;

            topOffset = offset;

            var ScreenSafeAreas = FindObjectsOfType<ScreenSafeArea>();
            foreach (var component in ScreenSafeAreas)
                component.StartCoroutine(component.IECheckSafeArea2());
        }

        private IEnumerator IECheckSafeArea2()
        {
            float time = 0.5f;
            while (time > 0)
            {
                CheckSafeArea2();
                time -= Time.deltaTime;
                yield return null;
            }
        }

        [ContextMenu("Test")]
        private void Test()
        {
            SetTopOffsetForBannerAd(143);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (m_CurrentOrientation != Screen.orientation || m_CurrentSafeArea != Screen.safeArea)
                CheckSafeArea2();
        }
#endif
    }
}