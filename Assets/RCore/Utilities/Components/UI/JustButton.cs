/**
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using RCore.Common;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace RCore.Components
{
    [AddComponentMenu("Utitlies/UI/JustButton")]
    public class JustButton : Button
    {
        public enum PivotForScale
        {
            Bot,
            Top,
            TopLeft,
            BotLeft,
            TopRight,
            BotRight,
            Center,
        }

        private static Material mGreyMat;

        [SerializeField] protected PivotForScale mPivotForFX;
        [SerializeField] protected bool mEnabledFX = true;
        [SerializeField] protected Image mImg;
        [SerializeField] protected Vector2 mInitialScale = Vector2.one;

        [SerializeField] protected bool mGreyMatEnabled;
        [SerializeField] protected bool mImgSwapEnabled;
        [SerializeField] protected Sprite mImgActive;
        [SerializeField] protected Sprite mImgInactive;
        [SerializeField] protected string m_SfxClip = "sfx_button_click";

        [SerializeField] protected bool mContentSwapEnabled;
        [SerializeField] protected GameObject[] mContentActive;
        [SerializeField] protected GameObject[] mContentInactive;

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
            get { return img.material; }
            set { img.material = value; }
        }
        public RectTransform rectTransform
        {
            get { return image != null ? image.rectTransform : null; }
        }

        private PivotForScale mPrePivot;
        private Action mInactionStateAction;
        private bool mActive = true;

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

                if (mContentSwapEnabled)
                {
                    foreach (var obj in mContentActive)
                        obj.SetActive(true);
                    foreach (var obj in mContentInactive)
                        obj.SetActive(false);
                }
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

                    if (mContentSwapEnabled)
                    {
                        foreach (var obj in mContentActive)
                            obj.SetActive(false);
                        foreach (var obj in mContentInactive)
                            obj.SetActive(true);
                    }
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
                    mImg = targetGraphic as Image;
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
                if (!string.IsNullOrEmpty(m_SfxClip))
                    AudioManager.Instance?.PlaySFX(m_SfxClip, 0);
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

        public void SetPivot(RectTransform rectTransform, Vector2 pivot)
        {
            if (rectTransform == null) return;

            Vector2 size = rectTransform.rect.size;
            Vector2 deltaPivot = rectTransform.pivot - pivot;
            Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
            rectTransform.pivot = pivot;
            rectTransform.localPosition -= deltaPosition;
        }

        public Material GetGreyMat()
        {
            if (mGreyMat == null)
                //mGreyMat = new Material(Shader.Find("NBCustom/Sprites/Greyscale"));
                mGreyMat = Resources.Load<Material>("Greyscale");
            return mGreyMat;
        }

        public void EnableGrey(bool pValue)
        {
            mGreyMatEnabled = pValue;
        }

        public bool Enabled() { return enabled && mActive; }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JustButton), true)]
    class JustButtonEditor : ButtonEditor
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
                var contentSwapEnabled = EditorHelper.SerializeField(serializedObject, "mContentSwapEnabled");
                if (contentSwapEnabled.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    SerializedProperty mContentActive = serializedObject.FindProperty("mContentActive");
                    if (mContentActive.isExpanded)
                        EditorGUILayout.PropertyField(mContentActive, true);
                    else
                        EditorGUILayout.PropertyField(mContentActive, new GUIContent(mContentActive.displayName));
                    SerializedProperty mContentInactive = serializedObject.FindProperty("mContentInactive");
                    if (mContentInactive.isExpanded)
                        EditorGUILayout.PropertyField(mContentInactive, true);
                    else
                        EditorGUILayout.PropertyField(mContentInactive, new GUIContent(mContentInactive.displayName));
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("RUtilities/UI/Replace Button By JustButton")]
        private static void ReplaceButton()
        {
            var gameobjects = Selection.gameObjects;
            for (int i = 0; i < gameobjects.Length; i++)
            {
                var btns = gameobjects[i].FindComponentsInChildren<Button>();
                for (int j = 0; j < btns.Count; j++)
                {
                    var btn = btns[j];
                    if (!(btn is JustButton))
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
