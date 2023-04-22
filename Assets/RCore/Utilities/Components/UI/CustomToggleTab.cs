/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RCore.Inspector;
using RCore.Common;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if USE_DOTWEEN
using DG.Tweening;
#endif

namespace RCore.Components
{
    [AddComponentMenu("Utitlies/UI/CustomToggleTab")]
    public class CustomToggleTab : Toggle
    {
        public Image imgBackground;
        public TextMeshProUGUI txtLabel;
        public List<RectTransform> contentsInActive;
        public List<RectTransform> contentsInDeactive;
        public List<TextMeshProUGUI> additionalLabels;
        public string sfxClip = "button";

        public bool enableBgSpriteSwitch;
        public Sprite sptActiveBackground;
        public Sprite sptInactiveBackground;

        public bool enableBgColorSwitch;
        public Color colorActiveBackground;
        public Color colorInactiveBackground;

        public bool enableTextColorSwitch;
        public Color colorActiveText;
        public Color colorInactiveText;

        public bool enableSizeSwitch;
        public Vector2 sizeActive;
        public Vector2 sizeDeactive;

        public bool enableFontSizeSwitch;
        public float fontSizeActive;
        public float fontSizeInactive;

        public float tweenTime = 0.5f;

        [ReadOnly] public bool isLocked;

        public Action OnClickOnLock;

        private CustomToggleGroup mCustomToggleGroup;
        private bool mIsOn;

        protected override void Start()
        {
            base.Start();

            mCustomToggleGroup = group as CustomToggleGroup;
            mIsOn = isOn;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (Application.isPlaying)
                return;

            base.OnValidate();

            if (txtLabel == null)
                txtLabel = gameObject.FindComponentInChildren<TextMeshProUGUI>();

            if (imgBackground == null)
            {
                var images = gameObject.GetComponentsInChildren<Image>();
                imgBackground = images[0];
            }

            if (enableBgColorSwitch)
            {
                if (colorActiveBackground.a == 0 && imgBackground != null)
                    colorActiveBackground = imgBackground.color;
            }
            if (enableBgSpriteSwitch)
            {
                if (sptActiveBackground == null && imgBackground.sprite != null)
                    sptActiveBackground = imgBackground.sprite;
            }
            if (enableFontSizeSwitch)
            {
                if (fontSizeActive == 0 && txtLabel != null)
                    fontSizeActive = txtLabel.fontSize;
            }
            if (enableTextColorSwitch)
            {
                if (colorActiveText.a == 0 && txtLabel != null)
                    colorActiveText = txtLabel.color;
            }
            if (enableSizeSwitch)
            {
                if (sizeActive == Vector2.zero)
                    sizeActive = (transform as RectTransform).sizeDelta;
            }

            if (group == null)
                group = gameObject.GetComponentInParent<ToggleGroup>();
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();

            onValueChanged.AddListener(OnValueChanged);

            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            onValueChanged.RemoveListener(OnValueChanged);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (isLocked)
            {
                OnClickOnLock?.Invoke();
                return;
            }

            base.OnPointerClick(eventData);
        }

        private void Refresh()
        {
            if (contentsInActive != null)
                foreach (var item in contentsInActive)
                    item.SetActive(isOn);

            if (contentsInDeactive != null)
                foreach (var item in contentsInDeactive)
                    item.SetActive(!isOn);

            if (enableBgSpriteSwitch)
                imgBackground.sprite = isOn ? sptActiveBackground : sptInactiveBackground;

            if (enableSizeSwitch)
                (transform as RectTransform).sizeDelta = isOn ? sizeActive : sizeDeactive;

            if (enableBgColorSwitch)
                imgBackground.color = isOn ? colorActiveBackground : colorInactiveBackground;

            if (enableTextColorSwitch)
            {
                if (txtLabel != null)
                    txtLabel.color = isOn ? colorActiveText : colorInactiveText;

                if (additionalLabels != null)
                    foreach (var label in additionalLabels)
                        label.color = isOn ? colorActiveText : colorInactiveText;
            }

            if (enableFontSizeSwitch)
            {
                if (txtLabel != null)
                    txtLabel.fontSize = isOn ? fontSizeActive : fontSizeInactive;

                if (additionalLabels != null)
                    foreach (var label in additionalLabels)
                        label.fontSize = isOn ? fontSizeActive : fontSizeInactive;
            }

            if (mCustomToggleGroup != null && isOn)
                mCustomToggleGroup.SetTarget(transform as RectTransform, tweenTime);
        }

        private void RefreshByTween()
        {
#if USE_DOTWEEN
            if (mIsOn == isOn)
                return;

            mIsOn = isOn;

            if (contentsInActive != null)
                foreach (var item in contentsInActive)
                    item.SetActive(isOn);

            if (contentsInDeactive != null)
                foreach (var item in contentsInDeactive)
                    item.SetActive(!isOn);

            if (Application.isPlaying)
            {
                if (enableBgSpriteSwitch)
                {
                    imgBackground.DOKill();
                    imgBackground.sprite = isOn ? sptActiveBackground : sptInactiveBackground;
                }
                if (enableSizeSwitch || enableTextColorSwitch || enableBgColorSwitch || enableFontSizeSwitch)
                {

                    Color txtFromColor = !isOn ? colorActiveText : colorInactiveText;
                    Color txtToColor = isOn ? colorActiveText : colorInactiveText;
                    Color bgFromColor = !isOn ? colorActiveBackground : colorInactiveBackground;
                    Color bgToColor = isOn ? colorActiveBackground : colorInactiveBackground;
                    Vector2 sizeFrom = !isOn ? sizeActive : sizeDeactive;
                    Vector2 sizeTo = isOn ? sizeActive : sizeDeactive;
                    float fontSizeFrom = !isOn ? fontSizeActive : fontSizeInactive;
                    float fontSizeTo = isOn ? fontSizeActive : fontSizeInactive;
                    float val = 0;
                    var rectTransform = (transform as RectTransform);
                    DOTween.Kill(GetInstanceID());
                    DOTween.To(() => val, x => val = x, 1, tweenTime)
                        .OnUpdate(() =>
                        {
                            if (enableSizeSwitch)
                            {
                                Vector2 size = Vector2.Lerp(sizeFrom, sizeTo, val);
                                rectTransform.sizeDelta = size;
                            }
                            if (enableTextColorSwitch)
                            {
                                Color color = Color.Lerp(txtFromColor, txtToColor, val);
                                if (txtLabel != null)
                                    txtLabel.color = color;

                                if (additionalLabels != null)
                                    foreach (var label in additionalLabels)
                                        label.color = color;
                            }
                            if (enableBgColorSwitch)
                            {
                                Color color = Color.Lerp(bgFromColor, bgToColor, val);
                                imgBackground.color = color;
                            }
                            if (enableFontSizeSwitch)
                            {
                                if (txtLabel != null)
                                    txtLabel.fontSize = Mathf.Lerp(fontSizeFrom, fontSizeTo, val);

                                if (additionalLabels != null)
                                    foreach (var label in additionalLabels)
                                        label.fontSize = Mathf.Lerp(fontSizeFrom, fontSizeTo, val);
                            }
                        })
                        .OnComplete(() =>
                        {
                            if (enableSizeSwitch)
                                rectTransform.sizeDelta = isOn ? sizeActive : sizeDeactive;
                            if (enableTextColorSwitch)
                            {
                                if (txtLabel != null)
                                    txtLabel.color = txtToColor;

                                if (additionalLabels != null)
                                    foreach (var label in additionalLabels)
                                        label.color = isOn ? colorActiveText : colorInactiveText;
                            }
                            if (enableBgColorSwitch)
                                imgBackground.color = isOn ? colorActiveBackground : colorInactiveBackground;
                            if (enableFontSizeSwitch)
                            {
                                if (txtLabel != null)
                                    txtLabel.fontSize = isOn ? fontSizeActive : fontSizeInactive;

                                if (additionalLabels != null)
                                    foreach (var label in additionalLabels)
                                        label.fontSize = isOn ? fontSizeActive : fontSizeInactive;
                            }
                        })
                        .SetId(GetInstanceID())
                        .SetEase(Ease.OutCubic);

                    if (mCustomToggleGroup != null)
                        mCustomToggleGroup.SetTarget(rectTransform, tweenTime);
                }
            }
#endif
        }

        private void OnValueChanged(bool pIson)
        {
            if (pIson)
                AudioManager.Instance?.PlaySFX(sfxClip, 0);
#if USE_DOTWEEN
            if (Application.isPlaying && tweenTime > 0)
            {
                RefreshByTween();
                return;
            }
#endif
            Refresh();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CustomToggleTab), true)]
    class CustomToggleTabEditor : UnityEditor.UI.ToggleEditor
    {
        private CustomToggleTab mToggle;

        protected override void OnEnable()
        {
            base.OnEnable();

            mToggle = (CustomToggleTab)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorHelper.SerializeField(serializedObject, "imgBackground");
                EditorHelper.SerializeField(serializedObject, "txtLabel");
                EditorHelper.SerializeField(serializedObject, "contentsInActive");
                EditorHelper.SerializeField(serializedObject, "contentsInDeactive");
                EditorHelper.SerializeField(serializedObject, "additionalLabels");
                EditorHelper.SerializeField(serializedObject, "sfxClip");

                var enableBgSpriteSwitch = EditorHelper.SerializeField(serializedObject, "enableBgSpriteSwitch");
                if (enableBgSpriteSwitch.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "sptActiveBackground");
                    EditorHelper.SerializeField(serializedObject, "sptInactiveBackground");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                var enableBgColorSwitch = EditorHelper.SerializeField(serializedObject, "enableBgColorSwitch");
                if (enableBgColorSwitch.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "colorActiveBackground");
                    EditorHelper.SerializeField(serializedObject, "colorInactiveBackground");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                var enableTextColorSwitch = EditorHelper.SerializeField(serializedObject, "enableTextColorSwitch");
                if (enableTextColorSwitch.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "colorActiveText");
                    EditorHelper.SerializeField(serializedObject, "colorInactiveText");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                var enableSizeSwitch = EditorHelper.SerializeField(serializedObject, "enableSizeSwitch");
                if (enableSizeSwitch.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "sizeActive");
                    EditorHelper.SerializeField(serializedObject, "sizeDeactive");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                var enableFontSizeSwitch = EditorHelper.SerializeField(serializedObject, "enableFontSizeSwitch");
                if (enableFontSizeSwitch.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "fontSizeActive");
                    EditorHelper.SerializeField(serializedObject, "fontSizeInactive");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                EditorHelper.SerializeField(serializedObject, "tweenTime");

                if (mToggle.txtLabel != null)
                    mToggle.txtLabel.text = EditorGUILayout.TextField("Label", mToggle.txtLabel.text);

                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndVertical();

            base.OnInspectorGUI();
        }
    }
#endif
}