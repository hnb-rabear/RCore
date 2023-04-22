/**
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
    [AddComponentMenu("Utitlies/UI/ImageWithBackground")]
    public class ImageWithBackground : ImageWithTextTMP
    {
        [SerializeField] private Image mImgBackground;

#if UNITY_EDITOR
        protected override void Validate()
        {
            if (mTxt == null)
                mTxt = GetComponentInChildren<TextMeshProUGUI>();
            if (mImg == null || mImgBackground == null)
            {
                var images = GetComponentsInChildren<Image>();
                mImgBackground = images.Length > 0 ? images[0] : null;
                mImg = images.Length > 1 ? images[1] : images[0];
            }
        }
#endif
    }
}
