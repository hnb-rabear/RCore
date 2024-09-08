/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

#pragma warning disable 0649

using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
#endif

namespace RCore.Components
{
    [AddComponentMenu("RCore/UI/ImageWithBackground")]
    public class ImageWithBackground : ImageWithTextTMP
    {
        [SerializeField] private Image mImgBackground;

#if UNITY_EDITOR
        protected override void Validate()
        {
            if (m_Txt == null)
                m_Txt = GetComponentInChildren<TextMeshProUGUI>();
            if (m_Img == null || mImgBackground == null)
            {
                var images = GetComponentsInChildren<Image>();
                mImgBackground = images.Length > 0 ? images[0] : null;
                m_Img = images.Length > 1 ? images[1] : images[0];
            }
        }
#endif
    }
}
