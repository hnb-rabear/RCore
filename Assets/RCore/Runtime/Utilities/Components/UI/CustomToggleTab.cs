/***
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
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if USE_DOTWEEN
using DG.Tweening;
#endif

namespace RCore.Components
{
    [AddComponentMenu("RCore/UI/CustomToggleTab")]
    public class CustomToggleTab : Toggle
    {
        public Image imgBackground;
        public TextMeshProUGUI txtLabel;
        [FormerlySerializedAs("contentsInActive")] public List<RectTransform> contentsActive;
        [FormerlySerializedAs("contentsInDeactive")] public List<RectTransform> contentsInactive;
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
        [FormerlySerializedAs("sizeDeactive")] public Vector2 sizeInactive;

        public bool enableFontSizeSwitch;
        public float fontSizeActive;
        public float fontSizeInactive;

        public float tweenTime = 0.5f;

        [ReadOnly] public bool isLocked;

        public Action onClickOnLock;

        private CustomToggleGroup m_CustomToggleGroup;
		private bool m_IsOn2;
		
		public bool IsOn
		{
			get => isOn;
			set
			{
				isOn = value;
				if (m_IsOn2 != value)
				{
					m_IsOn2 = value;
					onValueChanged?.Invoke(value);
				}
			}
		}

		protected override void Start()
        {
            base.Start();

            m_CustomToggleGroup = group as CustomToggleGroup;
			m_IsOn2 = isOn;
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
                if (imgBackground != null && sptActiveBackground == null && imgBackground.sprite != null)
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
                    sizeActive = ((RectTransform)transform).sizeDelta;
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
                onClickOnLock?.Invoke();
                return;
            }

            base.OnPointerClick(eventData);
        }

        private void Refresh()
        {
            if (contentsActive != null)
                foreach (var item in contentsActive)
                    item.SetActive(isOn);

            if (contentsInactive != null)
                foreach (var item in contentsInactive)
                    item.SetActive(!isOn);

            if (enableBgSpriteSwitch)
                imgBackground.sprite = isOn ? sptActiveBackground : sptInactiveBackground;

            if (enableSizeSwitch)
                ((RectTransform)transform).sizeDelta = isOn ? sizeActive : sizeInactive;

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

            if (m_CustomToggleGroup != null && isOn)
                m_CustomToggleGroup.SetTarget(transform as RectTransform, tweenTime);
        }

        private void RefreshByTween()
        {
#if USE_DOTWEEN
            if (m_IsOn2 == isOn)
                return;

            m_IsOn2 = isOn;

            if (contentsActive != null)
                foreach (var item in contentsActive)
                    item.SetActive(isOn);

            if (contentsInactive != null)
                foreach (var item in contentsInactive)
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

                    var txtFromColor = !isOn ? colorActiveText : colorInactiveText;
                    var txtToColor = isOn ? colorActiveText : colorInactiveText;
                    var bgFromColor = !isOn ? colorActiveBackground : colorInactiveBackground;
                    var bgToColor = isOn ? colorActiveBackground : colorInactiveBackground;
                    var sizeFrom = !isOn ? sizeActive : sizeInactive;
                    var sizeTo = isOn ? sizeActive : sizeInactive;
                    float fontSizeFrom = !isOn ? fontSizeActive : fontSizeInactive;
                    float fontSizeTo = isOn ? fontSizeActive : fontSizeInactive;
                    float val = 0;
                    var rectTransform = transform as RectTransform;
                    DOTween.Kill(GetInstanceID());
                    DOTween.To(() => val, x => val = x, 1, tweenTime)
                        .OnUpdate(() =>
                        {
                            if (enableSizeSwitch)
                            {
                                var size = Vector2.Lerp(sizeFrom, sizeTo, val);
                                rectTransform.sizeDelta = size;
                            }
                            if (enableTextColorSwitch)
                            {
                                var color = Color.Lerp(txtFromColor, txtToColor, val);
                                if (txtLabel != null)
                                    txtLabel.color = color;

                                if (additionalLabels != null)
                                    foreach (var label in additionalLabels)
                                        label.color = color;
                            }
                            if (enableBgColorSwitch)
                            {
                                var color = Color.Lerp(bgFromColor, bgToColor, val);
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
                                rectTransform.sizeDelta = isOn ? sizeActive : sizeInactive; 
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

                    if (m_CustomToggleGroup != null)
						m_CustomToggleGroup.SetTarget(rectTransform, tweenTime);
                }
            }
#endif
        }

        private void OnValueChanged(bool pIsOn)
        {
            if (pIsOn && AudioManager.Instance)
                AudioManager.Instance.PlaySFX(sfxClip, 0);
#if USE_DOTWEEN
			if (Application.isPlaying && tweenTime > 0 && transition != Transition.Animation)
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
                EditorHelper.SerializeField(serializedObject, "contentsActive");
                EditorHelper.SerializeField(serializedObject, "contentsInactive");
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
                    EditorHelper.SerializeField(serializedObject, "sizeInactive");
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