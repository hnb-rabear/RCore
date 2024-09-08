/***
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using RCore.Common;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace RCore.Components
{
    [AddComponentMenu("RCore/UI/JustButton")]
    public class JustButton : Button
    {
        protected enum PivotForScale
        {
            Bot,
            Top,
            TopLeft,
            BotLeft,
            TopRight,
            BotRight,
            Center,
        }

        public enum PerfectRatio
        {
            None,
            Width,
            Height,
        }

        private static Material m_GreyMat;

        [SerializeField] protected PivotForScale mPivotForFX = PivotForScale.Center;
        [SerializeField] protected bool mEnabledFX = true;
        [SerializeField] protected Image mImg;
        [SerializeField] protected Vector2 mInitialScale = Vector2.one;
        [SerializeField] protected PerfectRatio m_PerfectRatio = PerfectRatio.Height;

        [SerializeField] protected bool mGreyMatEnabled;
        [SerializeField] protected bool mImgSwapEnabled;
        [SerializeField] public Sprite mImgActive;
        [SerializeField] public Sprite mImgInactive;
        [FormerlySerializedAs("m_SFXClip")]
        [SerializeField] protected string m_SfxClip = "sfx_button_click";

        public Image img
        {
            get
            {
                if (mImg == null)
                    mImg = targetGraphic as Image;
                return mImg;
            }
        }
        public Material imgMaterial
        {
            get => img.material;
            set => img.material = value;
        }
        public RectTransform rectTransform => image != null ? image.rectTransform : null;

        private PivotForScale mPrePivot;
        private Action mInactionStateAction;
        private bool mActive = true;
        private int m_PerfectSpriteId;

        public virtual void SetEnable(bool pValue)
        {
            mActive = pValue;
            enabled = pValue || mInactionStateAction != null;
            if (pValue)
            {
                if (mImgSwapEnabled)
                    mImg.sprite = mImgActive;
                else
                    imgMaterial = null;
            }
            else
            {
                if (mImgSwapEnabled)
                {
                    mImg.sprite = mImgInactive;
                }
                else
                {
                    transform.localScale = mInitialScale;

                    //Use grey material here
                    if (mGreyMatEnabled)
                        imgMaterial = GetGreyMat();
                }
            }
        }

        public virtual void SetInactiveStateAction(Action pAction)
        {
            mInactionStateAction = pAction;
            enabled = mActive || mInactionStateAction != null;
        }

        protected override void Start()
        {
            base.Start();

            mPrePivot = mPivotForFX;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (mEnabledFX)
                transform.localScale = mInitialScale;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (Application.isPlaying)
                return;

            base.OnValidate();

            if (targetGraphic == null)
            {
                var images = gameObject.GetComponentsInChildren<Image>();
                if (images.Length > 0)
                {
                    targetGraphic = images[0];
                    mImg = (Image)targetGraphic;
                }
            }
            if (targetGraphic != null && mImg == null)
                mImg = targetGraphic as Image;

            if (transition == Transition.Animation && GetComponent<Animator>() != null)
            {
                GetComponent<Animator>().updateMode = AnimatorUpdateMode.UnscaledTime;
                mEnabledFX = false;
            }

            RefreshPivot();

            CheckPerfectRatio();
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();

            if (mEnabledFX)
            {
                transform.localScale = mInitialScale;
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!mActive && mInactionStateAction != null)
                mInactionStateAction();

            if (mActive)
            {
                base.OnPointerDown(eventData);
                if (!string.IsNullOrEmpty(m_SfxClip) && AudioManager.Instance)
                    AudioManager.Instance.PlaySFX(m_SfxClip, 0);
            }

            if (mEnabledFX)
            {
                if (mPivotForFX != mPrePivot)
                {
                    mPrePivot = mPivotForFX;
                    RefreshPivot(rectTransform);
                }

                transform.localScale = mInitialScale * 0.95f;
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (mActive)
                base.OnPointerUp(eventData);

            if (mEnabledFX)
            {
                transform.localScale = mInitialScale;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (mActive)
                base.OnPointerClick(eventData);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            if (mActive)
                base.OnSelect(eventData);
        }

        public void RefreshPivot()
        {
            RefreshPivot(rectTransform);
        }

        private void RefreshPivot(RectTransform pRect)
        {
            switch (mPivotForFX)
            {
                case PivotForScale.Bot:
                    SetPivot(pRect, new Vector2(0.5f, 0));
                    break;
                case PivotForScale.BotLeft:
                    SetPivot(pRect, new Vector2(0, 0));
                    break;
                case PivotForScale.BotRight:
                    SetPivot(pRect, new Vector2(1, 0));
                    break;
                case PivotForScale.Top:
                    SetPivot(pRect, new Vector2(0.5f, 1));
                    break;
                case PivotForScale.TopLeft:
                    SetPivot(pRect, new Vector2(0, 1f));
                    break;
                case PivotForScale.TopRight:
                    SetPivot(pRect, new Vector2(1, 1f));
                    break;
                case PivotForScale.Center:
                    SetPivot(pRect, new Vector2(0.5f, 0.5f));
                    break;
            }
        }

        public void SetPivot(RectTransform pRectTransform, Vector2 pivot)
        {
            if (pRectTransform == null) return;

            var size = pRectTransform.rect.size;
            var deltaPivot = pRectTransform.pivot - pivot;
            var deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
            pRectTransform.pivot = pivot;
            pRectTransform.localPosition -= deltaPosition;
        }

        public Material GetGreyMat()
        {
            if (m_GreyMat == null)
                //mGreyMat = new Material(Shader.Find("NBCustom/Sprites/Greyscale"));
                m_GreyMat = Resources.Load<Material>("Greyscale");
            return m_GreyMat;
        }

        public void EnableGrey(bool pValue)
        {
            mGreyMatEnabled = pValue;
        }

        public bool Enabled() { return enabled && mActive; }

        protected void CheckPerfectRatio()
        {
            if (m_PerfectRatio == PerfectRatio.Width)
            {
                var image1 = mImg;
                if (image1 != null && image1.sprite != null && image1.type == Image.Type.Sliced && m_PerfectSpriteId != image1.sprite.GetInstanceID())
                {
                    var nativeSize = image1.sprite.NativeSize();
                    var rectSize = rectTransform.sizeDelta;
                    if (rectSize.x > 0 && rectSize.x < nativeSize.x)
                    {
                        var ratio = nativeSize.x * 1f / rectSize.x;
                        image1.pixelsPerUnitMultiplier = ratio;
                    }
                    else
                        image1.pixelsPerUnitMultiplier = 1;
                    m_PerfectSpriteId = image1.sprite.GetInstanceID();
                }
            }
            else if (m_PerfectRatio == PerfectRatio.Height)
            {
                var image1 = mImg;
                if (image1 != null && image1.sprite != null && image1.type == Image.Type.Sliced && m_PerfectSpriteId != image1.sprite.GetInstanceID())
                {
                    var nativeSize = image1.sprite.NativeSize();
                    var rectSize = rectTransform.sizeDelta;
                    if (rectSize.y > 0 && rectSize.y < nativeSize.y)
                    {
                        var ratio = nativeSize.y * 1f / rectSize.y;
                        image1.pixelsPerUnitMultiplier = ratio;
                    }
                    else
                        image1.pixelsPerUnitMultiplier = 1;
                    m_PerfectSpriteId = image1.sprite.GetInstanceID();
                }
            }
        }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JustButton), true)]
    internal class JustButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical("box");
            {
                EditorHelper.SerializeField(serializedObject, "mImg");
                EditorHelper.SerializeField(serializedObject, "mPivotForFX");
                EditorHelper.SerializeField(serializedObject, "mEnabledFX");
                EditorHelper.SerializeField(serializedObject, "mGreyMatEnabled");
                EditorHelper.SerializeField(serializedObject, "m_SfxClip");
                EditorHelper.SerializeField(serializedObject, "m_PerfectRatio");
                var imgSwapEnabled = EditorHelper.SerializeField(serializedObject, "mImgSwapEnabled");
                if (imgSwapEnabled.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "mImgActive");
                    EditorHelper.SerializeField(serializedObject, "mImgInactive");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("RCore/UI/Replace Button By JustButton")]
        private static void ReplaceButton()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var buttons = gameObjects[i].FindComponentsInChildren<Button>();
                for (int j = 0; j < buttons.Count; j++)
                {
                    var btn = buttons[j];
                    if (btn is not JustButton)
                    {
                        var obj = btn.gameObject;
                        DestroyImmediate(btn);
                        obj.AddComponent<JustButton>();
                    }
                }
            }
        }
    }
#endif
}