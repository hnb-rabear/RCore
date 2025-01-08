/***
 * Author HNB-RaBear - 2019
 **/
#pragma warning disable 0649

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.UI
{
    /// <summary>
    /// Create a hole at a rect-transform target
    /// And everything's outside that hole will not be interactable
    /// </summary>
    public class HoledLayerMask : MonoBehaviour
    {
        public RectTransform rectContainer;
        public Image imgHole;
        public Image imgLeft;
        public Image imgRight;
        public Image imgTop;
        public Image imgBot;
        public RectTransform testTarget;

        private RectTransform m_currentTarget;
        private bool m_tweening;
        private Bounds m_bounds;

        private void OnEnable()
        {
            m_bounds = rectContainer.Bounds();
            imgTop.rectTransform.pivot = new Vector2(0.5f, 1f);
            imgBot.rectTransform.pivot = new Vector2(0.5f, 0f);
            imgLeft.rectTransform.pivot = new Vector2(0, 0.5f);
            imgRight.rectTransform.pivot = new Vector2(1f, 0.5f);
        }

        private void Awake()
        {
            enabled = false;
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
            //Change size of 4-border following the size and position of hole
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
            var holeAnchoredPosition = imgHole.rectTransform.anchoredPosition;
            leftLayerPos.y = holeAnchoredPosition.y;
            imgLeft.rectTransform.anchoredPosition = leftLayerPos;
            var rightLayerPos = imgRight.rectTransform.anchoredPosition;
            rightLayerPos.y = holeAnchoredPosition.y;
            imgRight.rectTransform.anchoredPosition = rightLayerPos;

            if (m_currentTarget != null && !m_tweening)
            {
	            var rect = m_currentTarget.rect;
	            var localScale = m_currentTarget.localScale;
	            imgHole.rectTransform.sizeDelta = new Vector2(rect.width * localScale.x, rect.height * localScale.y);
            }
        }

        public void Active(bool pValue)
        {
            enabled = pValue;
            if (!pValue)
                imgHole.rectTransform.sizeDelta = Vector2.zero;
            imgHole.gameObject.SetActive(pValue);
            imgLeft.gameObject.SetActive(pValue);
            imgTop.gameObject.SetActive(pValue);
            imgRight.gameObject.SetActive(pValue);
            imgBot.gameObject.SetActive(pValue);
        }

        public void SetColor(Color pColor)
        {
            imgLeft.color = pColor;
            imgTop.color = pColor;
            imgRight.color = pColor;
            imgBot.color = pColor;
        }

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
                float val = 0;
                DOTween.To(() => val, xx => val = xx, 1f, pTime)
                    .OnUpdate(() =>
                    {
                        imgHole.rectTransform.sizeDelta = Vector2.Lerp(fromSize, toSize, val);
                    })
                    .OnComplete(() =>
                    {
                        imgHole.raycastTarget = false;
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

        public RectTransform GetHoleTransform()
        {
            return imgHole.rectTransform;
        }
        /// <summary>
        /// NOTE: need more test
        /// Focus to a object in 2D world 
        /// </summary>
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

        /// <summary>
        /// Incase target is in a scrollview or some sort of UI element which take one or few frames to refresh
        /// We need to observer target longer
        /// </summary>
        private IEnumerator IEPostValidating(RectTransform pTarget)
        {
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                FocusToTargetImmediately(pTarget, false);
            }
        }

        /// <summary>
        /// Make a clone of sprite and use it as a mask to cover around target
        /// NOTE: condition is source sprite texture must be TRUE COLOR, and it's Read/Write enabled
        /// </summary>
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
                if (sprite.border != Vector4.zero)
                    imgHole.type = Image.Type.Sliced;
                else
                    imgHole.type = Image.Type.Simple;
            }
            catch (Exception ex)
            {
                imgHole.sprite = null;
                imgHole.color = Color.clear;
                Debug.LogError(ex.ToString());
            }
        }

        public void ClearSpriteMask()
        {
            imgHole.sprite = null;
            imgHole.color = Color.clear;
        }

        private void CreateComponents()
        {
            if (imgHole == null)
            {
                imgHole = new GameObject().AddComponent<Image>();
                imgHole.transform.SetParent(transform);
                imgHole.transform.localScale = Vector3.one;
                imgHole.rectTransform.anchoredPosition = Vector2.zero;
                imgHole.color = Color.black.SetAlpha(0.5f);
                imgHole.name = "Hole";
            }
            if (imgLeft == null)
            {
                imgLeft = new GameObject().AddComponent<Image>();
                imgLeft.transform.SetParent(transform);
                imgLeft.transform.localScale = Vector3.one;
                imgLeft.rectTransform.anchorMin = new Vector2(0, 0.5f);
                imgLeft.rectTransform.anchorMax = new Vector2(0, 0.5f);
                imgLeft.rectTransform.pivot = new Vector2(0, 0.5f);
                imgLeft.rectTransform.anchoredPosition = Vector2.zero;
                imgLeft.color = Color.black.SetAlpha(0.1f);
                imgLeft.name = "Left";
            }
            if (imgRight == null)
            {
                imgRight = new GameObject().AddComponent<Image>();
                imgRight.transform.SetParent(transform);
                imgRight.transform.localScale = Vector3.one;
                imgRight.rectTransform.anchorMin = new Vector2(1, 0.5f);
                imgRight.rectTransform.anchorMax = new Vector2(1, 0.5f);
                imgRight.rectTransform.pivot = new Vector2(1, 0.5f);
                imgRight.rectTransform.anchoredPosition = Vector2.zero;
                imgRight.color = Color.black.SetAlpha(0.5f);
                imgRight.name = "Right";
            }
            if (imgTop == null)
            {
                imgTop = new GameObject().AddComponent<Image>();
                imgTop.transform.SetParent(transform);
                imgTop.transform.localScale = Vector3.one;
                imgTop.rectTransform.anchorMin = new Vector2(0.5f, 1);
                imgTop.rectTransform.anchorMax = new Vector2(0.5f, 1);
                imgTop.rectTransform.pivot = new Vector2(0.5f, 1);
                imgTop.rectTransform.anchoredPosition = Vector2.zero;
                imgTop.color = Color.black.SetAlpha(0.5f);
                imgTop.name = "Top";
            }
            if (imgBot == null)
            {
                imgBot = new GameObject().AddComponent<Image>();
                imgBot.transform.SetParent(transform);
                imgBot.transform.localScale = Vector3.one;
                imgBot.rectTransform.anchorMin = new Vector2(0.5f, 0);
                imgBot.rectTransform.anchorMax = new Vector2(0.5f, 0);
                imgBot.rectTransform.pivot = new Vector2(0.5f, 0);
                imgBot.rectTransform.anchoredPosition = Vector2.zero;
                imgBot.color = Color.black.SetAlpha(0.5f);
                imgBot.name = "Bot";
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HoledLayerMask))]
        public class HoledLayerMaskEditor : UnityEditor.Editor
        {
            private HoledLayerMask mScript;
            private Sprite mSprite;

            private void OnEnable()
            {
                mScript = (HoledLayerMask)target;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                mSprite = (Sprite)EditorGUILayout.ObjectField(mSprite, typeof(Sprite), true);

                if (GUILayout.Button("Clone Sprite"))
                    mScript.CreateHoleFromSprite(mSprite);
                if (GUILayout.Button("Focus To Test Target"))
                    mScript.FocusToTargetImmediately(mScript.testTarget);
                if (GUILayout.Button("Create Components"))
                    mScript.CreateComponents();
            }
        }
#endif
    }
}