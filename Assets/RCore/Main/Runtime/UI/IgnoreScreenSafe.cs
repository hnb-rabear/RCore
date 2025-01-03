using System;
using UnityEngine;

namespace RCore.UI
{
    public class IgnoreScreenSafe : MonoBehaviour
    {
        private void Start()
        {
            var offsetHeight = Screen.currentResolution.height - Screen.safeArea.height;
            if (offsetHeight > 0)
            {
                var rectTransform = (transform as RectTransform);
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + offsetHeight / 2f);
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y + offsetHeight);
            }
        }
    }
}