#pragma warning disable 0649

using System;
using System.Collections;
#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>
    /// Full-screen dim overlay with a rectangular "hole" cut out around a target — used to highlight
    /// a single UI element during tutorials and onboarding. The hole is built from a four-quad
    /// approach (top/bottom/left/right images), so any UI render mode works.
    /// </summary>
    [AddComponentMenu("RevCore/UI/Holed Layer Mask")]
    public class HoledLayerMask : MonoBehaviour
    {
        /// <summary>Root rect containing the hole + four side dimmer images.</summary>
        public RectTransform rectContainer;
        /// <summary>Image rendered inside the hole (typically transparent — the "look through" area).</summary>
        public Image imgHole;
        /// <summary>Dim image covering the area left of the hole.</summary>
        public Image imgLeft;
        /// <summary>Dim image covering the area right of the hole.</summary>
        public Image imgRight;
        /// <summary>Dim image covering the area above the hole.</summary>
        public Image imgTop;
        /// <summary>Dim image covering the area below the hole.</summary>
        public Image imgBot;
        /// <summary>Inspector helper — focus the hole on this rect in edit mode.</summary>
        public RectTransform testTarget;

        private RectTransform m_currentTarget;
        private bool m_tweening;
        private Bounds m_bounds;

        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (rectContainer == null)
                rectContainer = transform as RectTransform;
            if (rectContainer == null || imgHole == null || imgLeft == null || imgRight == null || imgTop == null || imgBot == null)
                return;

            m_bounds = rectContainer.Bounds();
            imgTop.rectTransform.pivot = new Vector2(0.5f, 1f);
            imgBot.rectTransform.pivot = new Vector2(0.5f, 0f);
            imgLeft.rectTransform.pivot = new Vector2(0, 0.5f);
            imgRight.rectTransform.pivot = new Vector2(1f, 0.5f);
        }

        private void Update()
        {
            DrawHole();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;
            DrawHole();
        }

        private void DrawHole()
        {
            if (imgHole == null || imgLeft == null || imgRight == null || imgTop == null || imgBot == null)
                return;

            var holePosition = imgHole.rectTransform.anchoredPosition;
            var holeSizeDelta = imgHole.rectTransform.sizeDelta;
            var holeHalfSize = holeSizeDelta / 2f;
            float borderLeft = m_bounds.min.x;
            float borderRight = m_bounds.max.x;
            float borderTop = m_bounds.max.y;
            float borderBot = m_bounds.min.y;
            float layerLeftW = holePosition.x - holeHalfSize.x - borderLeft;
            float layerRightW = borderRight - (holePosition.x + holeHalfSize.x);
            float layerLeftH = holeSizeDelta.y;
            float layerRightH = holeSizeDelta.y;
            float layerTopW = m_bounds.size.x;
            float layerBotW = m_bounds.size.x;
            float layerTopH = borderTop - (holePosition.y + holeHalfSize.y);
            float layerBotH = holePosition.y - holeHalfSize.y - borderBot;

            imgLeft.rectTransform.sizeDelta = new Vector2(layerLeftW, layerLeftH);
            imgRight.rectTransform.sizeDelta = new Vector2(layerRightW, layerRightH);
            imgTop.rectTransform.sizeDelta = new Vector2(layerTopW, layerTopH);
            imgBot.rectTransform.sizeDelta = new Vector2(layerBotW, layerBotH);

            var leftLayerPos = imgLeft.rectTransform.anchoredPosition;
            leftLayerPos.y = holePosition.y;
            imgLeft.rectTransform.anchoredPosition = leftLayerPos;
            var rightLayerPos = imgRight.rectTransform.anchoredPosition;
            rightLayerPos.y = holePosition.y;
            imgRight.rectTransform.anchoredPosition = rightLayerPos;

            if (m_currentTarget != null && !m_tweening)
            {
                var rect = m_currentTarget.rect;
                var localScale = m_currentTarget.localScale;
                imgHole.rectTransform.sizeDelta = new Vector2(rect.width * localScale.x, rect.height * localScale.y);
            }
        }

        /// <summary>Shows or hides the overlay. Hiding disables the component to stop the per-frame tween work.</summary>
        public void Active(bool pValue)
        {
            enabled = pValue;
            if (!pValue && imgHole != null)
                imgHole.rectTransform.sizeDelta = Vector2.zero;
            if (imgHole != null)
                imgHole.gameObject.SetActive(pValue);
            if (imgLeft != null)
                imgLeft.gameObject.SetActive(pValue);
            if (imgTop != null)
                imgTop.gameObject.SetActive(pValue);
            if (imgRight != null)
                imgRight.gameObject.SetActive(pValue);
            if (imgBot != null)
                imgBot.gameObject.SetActive(pValue);
        }

        /// <summary>Sets the color of all four dim images uniformly.</summary>
        public void SetColor(Color pColor)
        {
            if (imgLeft != null)
                imgLeft.color = pColor;
            if (imgTop != null)
                imgTop.color = pColor;
            if (imgRight != null)
                imgRight.color = pColor;
            if (imgBot != null)
                imgBot.color = pColor;
        }

        /// <summary>Tweens the hole over <paramref name="pTime"/> seconds to <paramref name="pTarget"/>'s world rect (requires DOTWEEN).</summary>
        public void FocusToTarget(RectTransform pTarget, float pTime)
        {
            Active(true);

            m_bounds = rectContainer.Bounds();
            m_currentTarget = pTarget;

            var rect = pTarget.rect;
            var fromSize = new Vector2(rect.width, rect.height) * 10;
            var toSize = new Vector2(rect.width, rect.height);

            imgHole.rectTransform.position = pTarget.position;
            imgHole.rectTransform.sizeDelta = fromSize;
            var targetPivot = pTarget.pivot;
            var anchoredPosition = imgHole.rectTransform.anchoredPosition;
            var x = anchoredPosition.x - rect.width * targetPivot.x + rect.width / 2f;
            var y = anchoredPosition.y - rect.height * targetPivot.y + rect.height / 2f;
            anchoredPosition = new Vector2(x, y);
            imgHole.rectTransform.anchoredPosition = anchoredPosition;
#if DOTWEEN
            DOTween.Kill(imgHole.GetInstanceID());
            if (pTime > 0)
            {
                imgHole.raycastTarget = true;
                m_tweening = true;
                float val = 0;
                DOTween.To(() => val, xx => val = xx, 1f, pTime)
                    .OnUpdate(() => imgHole.rectTransform.sizeDelta = Vector2.Lerp(fromSize, toSize, val))
                    .OnComplete(() =>
                    {
                        imgHole.raycastTarget = false;
                        m_tweening = false;
                    })
                    .SetUpdate(true)
                    .SetId(imgHole.GetInstanceID());
            }
            else
            {
                imgHole.raycastTarget = false;
                imgHole.rectTransform.sizeDelta = toSize;
            }
#else
            imgHole.rectTransform.sizeDelta = toSize;
#endif
        }

        /// <summary>Snaps the hole to <paramref name="pTarget"/>'s world rect without animating. <paramref name="pPostValidatingRect"/> triggers a follow-up validate next frame.</summary>
        public void FocusToTargetImmediately(RectTransform pTarget, bool pPostValidatingRect = true)
        {
            Active(true);

            m_bounds = rectContainer.Bounds();
            m_currentTarget = pTarget;

            var targetPivot = pTarget.pivot;
            imgHole.rectTransform.position = pTarget.position;
            var rect = pTarget.rect;
            imgHole.rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
            var anchoredPosition = imgHole.rectTransform.anchoredPosition;
            var x = anchoredPosition.x - rect.width * targetPivot.x + rect.width / 2f;
            var y = anchoredPosition.y - rect.height * targetPivot.y + rect.height / 2f;
            anchoredPosition = new Vector2(x, y);
            imgHole.rectTransform.anchoredPosition = anchoredPosition;

            imgHole.raycastTarget = false;

            if (pPostValidatingRect)
                StartCoroutine(IEPostValidating(pTarget));
        }

        /// <summary>Returns the hole image's rect transform — useful when you need to place additional decorations around it.</summary>
        public RectTransform GetHoleTransform()
        {
            return imgHole.rectTransform;
        }

        /// <summary>Variant that focuses on a world-space <see cref="SpriteRenderer"/> instead of a UI rect. Projects through <paramref name="pWorldCamera"/> into <paramref name="pMainCanvas"/>'s space.</summary>
        public void FocusToTargetImmediately(SpriteRenderer pTarget, RectTransform pMainCanvas, Camera pWorldCamera)
        {
            Active(true);

            m_currentTarget = null;
            m_bounds = rectContainer.Bounds();

            var sprite = pTarget.sprite;
            var pivot = sprite.pivot;
            var lossyScale = pTarget.transform.lossyScale;
            float x = Mathf.Abs(lossyScale.x) * pTarget.size.x * sprite.pixelsPerUnit * 3.6f / pWorldCamera.orthographicSize;
            float y = Mathf.Abs(lossyScale.y) * pTarget.size.y * sprite.pixelsPerUnit * 3.6f / pWorldCamera.orthographicSize;
            var wRecSprite = new Vector2(x, y);
            Vector2 viewportPoint = pWorldCamera.WorldPointToCanvasPoint(pTarget.transform.position, pMainCanvas);

            var normalizedPivot = new Vector2(pivot.x / sprite.rect.width, pivot.y / sprite.rect.height);
            float offsetX = sprite.rect.width * (0.5f - normalizedPivot.x) * lossyScale.x;
            float offsetY = sprite.rect.height * (0.5f - normalizedPivot.y) * lossyScale.y;

            imgHole.rectTransform.anchoredPosition = new Vector3(viewportPoint.x + offsetX, viewportPoint.y + offsetY);
            imgHole.rectTransform.sizeDelta = new Vector2(wRecSprite.x, wRecSprite.y);
            imgHole.raycastTarget = false;
        }

        private IEnumerator IEPostValidating(RectTransform pTarget)
        {
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                FocusToTargetImmediately(pTarget, false);
            }
        }

        /// <summary>Replaces the hole shape with a custom sprite mask cut from <paramref name="spriteToClone"/>. Editor utility for non-rectangular holes.</summary>
        public void CreateHoleFromSprite(Sprite spriteToClone)
        {
            try
            {
                int posX = (int)spriteToClone.rect.x;
                int posY = (int)spriteToClone.rect.y;
                int sizeX = (int)(spriteToClone.bounds.size.x * spriteToClone.pixelsPerUnit);
                int sizeY = (int)(spriteToClone.bounds.size.y * spriteToClone.pixelsPerUnit);

                var newTex = new Texture2D(sizeX, sizeY, spriteToClone.texture.format, false);
                var colors = spriteToClone.texture.GetPixels(posX, posY, sizeX, sizeY);

                for (int i = 0; i < colors.Length; i++)
                {
                    if (colors[i].a == 0)
                        colors[i] = Color.white;
                    else
                        colors[i] = Color.clear;
                }
                newTex.SetPixels(colors);
                newTex.Apply();

                var sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), spriteToClone.pivot, spriteToClone.pixelsPerUnit, 0, SpriteMeshType.Tight, spriteToClone.border);
                imgHole.sprite = sprite;
                imgHole.color = imgLeft.color;
                imgHole.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
            catch (Exception ex)
            {
                imgHole.sprite = null;
                imgHole.color = Color.clear;
                Debug.LogError(ex.ToString());
            }
        }

        /// <summary>Reverts a sprite-mask hole back to the default rectangular four-quad layout.</summary>
        public void ClearSpriteMask()
        {
            imgHole.sprite = null;
            imgHole.color = Color.clear;
        }

        /// <summary>Editor utility that auto-creates the four side dimmer images + hole image as children. Idempotent.</summary>
        public void CreateComponents()
        {
            if (rectContainer == null)
                rectContainer = transform as RectTransform;
            if (imgHole == null)
            {
                imgHole = new GameObject("Hole").AddComponent<Image>();
                imgHole.transform.SetParent(transform);
                imgHole.transform.localScale = Vector3.one;
                imgHole.rectTransform.anchoredPosition = Vector2.zero;
                imgHole.color = Color.black.SetAlpha(0.5f);
            }
            if (imgLeft == null)
            {
                imgLeft = new GameObject("Left").AddComponent<Image>();
                imgLeft.transform.SetParent(transform);
                imgLeft.transform.localScale = Vector3.one;
                imgLeft.rectTransform.anchorMin = new Vector2(0, 0.5f);
                imgLeft.rectTransform.anchorMax = new Vector2(0, 0.5f);
                imgLeft.rectTransform.pivot = new Vector2(0, 0.5f);
                imgLeft.rectTransform.anchoredPosition = Vector2.zero;
                imgLeft.color = Color.black.SetAlpha(0.5f);
            }
            if (imgRight == null)
            {
                imgRight = new GameObject("Right").AddComponent<Image>();
                imgRight.transform.SetParent(transform);
                imgRight.transform.localScale = Vector3.one;
                imgRight.rectTransform.anchorMin = new Vector2(1, 0.5f);
                imgRight.rectTransform.anchorMax = new Vector2(1, 0.5f);
                imgRight.rectTransform.pivot = new Vector2(1, 0.5f);
                imgRight.rectTransform.anchoredPosition = Vector2.zero;
                imgRight.color = Color.black.SetAlpha(0.5f);
            }
            if (imgTop == null)
            {
                imgTop = new GameObject("Top").AddComponent<Image>();
                imgTop.transform.SetParent(transform);
                imgTop.transform.localScale = Vector3.one;
                imgTop.rectTransform.anchorMin = new Vector2(0.5f, 1);
                imgTop.rectTransform.anchorMax = new Vector2(0.5f, 1);
                imgTop.rectTransform.pivot = new Vector2(0.5f, 1);
                imgTop.rectTransform.anchoredPosition = Vector2.zero;
                imgTop.color = Color.black.SetAlpha(0.5f);
            }
            if (imgBot == null)
            {
                imgBot = new GameObject("Bot").AddComponent<Image>();
                imgBot.transform.SetParent(transform);
                imgBot.transform.localScale = Vector3.one;
                imgBot.rectTransform.anchorMin = new Vector2(0.5f, 0);
                imgBot.rectTransform.anchorMax = new Vector2(0.5f, 0);
                imgBot.rectTransform.pivot = new Vector2(0.5f, 0);
                imgBot.rectTransform.anchoredPosition = Vector2.zero;
                imgBot.color = Color.black.SetAlpha(0.5f);
            }
        }
    }
}
