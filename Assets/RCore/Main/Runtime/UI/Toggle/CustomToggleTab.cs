/***
 * Author RaBear - HNB - 2019
 **/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RCore.Inspector;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
	[AddComponentMenu("RCore/UI/CustomToggleTab")]
	public class CustomToggleTab : Toggle
	{
		[SerializeField] private Image m_imgBackground;
		[FormerlySerializedAs("txtLabel")]
		[SerializeField] private TextMeshProUGUI m_txtLabel;
		[FormerlySerializedAs("contentsActive")]
		[SerializeField] private List<RectTransform> m_contentsActive;
		[FormerlySerializedAs("contentsInactive")]
		[SerializeField] private List<RectTransform> m_contentsInactive;
		[FormerlySerializedAs("additionalLabels")]
		[SerializeField] private List<TextMeshProUGUI> m_additionalLabels;
		[FormerlySerializedAs("sfxClip")]
		[SerializeField] private string m_sfxClip = "button";
		[FormerlySerializedAs("sfxClipOff")]
		[SerializeField] private string m_sfxClipOff = "button";
		[SerializeField] protected bool m_scaleBounceEffect = true;

		[FormerlySerializedAs("enableBgSpriteSwitch")]
		[SerializeField] private bool m_enableBgSpriteSwitch;
		[FormerlySerializedAs("sptActiveBackground")]
		[SerializeField] private Sprite m_sptActiveBackground;
		[FormerlySerializedAs("sptInactiveBackground")]
		[SerializeField] private Sprite m_sptInactiveBackground;

		[FormerlySerializedAs("enableBgColorSwitch")]
		[SerializeField] private bool m_enableBgColorSwitch;
		[FormerlySerializedAs("colorActiveBackground")]
		[SerializeField] private Color m_colorActiveBackground;
		[FormerlySerializedAs("colorInactiveBackground")]
		[SerializeField] private Color m_colorInactiveBackground;

		[FormerlySerializedAs("enableTextColorSwitch")]
		[SerializeField] private bool m_enableTextColorSwitch;
		[FormerlySerializedAs("colorActiveText")]
		[SerializeField] private Color m_colorActiveText;
		[FormerlySerializedAs("colorInactiveText")]
		[SerializeField] private Color m_colorInactiveText;

		[FormerlySerializedAs("enableSizeSwitch")]
		[SerializeField] private bool m_enableSizeSwitch;
		[FormerlySerializedAs("sizeActive")]
		[SerializeField] private Vector2 m_sizeActive;
		[FormerlySerializedAs("sizeInactive")]
		[SerializeField] private Vector2 m_sizeInactive;

		[FormerlySerializedAs("enableFontSizeSwitch")]
		[SerializeField] private bool m_enableFontSizeSwitch;
		[FormerlySerializedAs("fontSizeActive")]
		[SerializeField] private float m_fontSizeActive;
		[FormerlySerializedAs("fontSizeInactive")]
		[SerializeField] private float m_fontSizeInactive;

		[FormerlySerializedAs("tweenTime")]
		[SerializeField] private float m_tweenTime = 0.5f;

		[ReadOnly] public bool isLocked;

		public Action onClickOnLock;

		private CustomToggleGroup m_customToggleGroup;
		private bool m_isOn2;
		private Vector2 m_initialScale;

		public bool IsOn
		{
			get => isOn;
			set
			{
				isOn = value;
				if (m_isOn2 != value)
				{
					m_isOn2 = value;
					onValueChanged?.Invoke(value);
				}
			}
		}

		protected override void Awake()
		{
			base.Awake();

			m_initialScale = transform.localScale;
		}

		protected override void Start()
		{
			base.Start();

			m_customToggleGroup = group as CustomToggleGroup;
			m_isOn2 = isOn;

			if (isOn && m_customToggleGroup != null)
				m_customToggleGroup.SetTarget(transform as RectTransform);
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			if (Application.isPlaying)
				return;

			base.OnValidate();

			if (m_txtLabel == null)
				m_txtLabel = gameObject.FindComponentInChildren<TextMeshProUGUI>();

			if (m_imgBackground == null)
			{
				var images = gameObject.GetComponentsInChildren<Image>();
				m_imgBackground = images[0];
			}

			if (m_imgBackground != null)
			{
				if (m_enableBgColorSwitch)
				{
					if (m_colorActiveBackground == default)
						m_colorActiveBackground = m_imgBackground.color;
				}
				else if (m_imgBackground.color == default && m_colorActiveBackground != default)
				{
					m_imgBackground.color = m_colorActiveBackground;
				}
				if (m_enableBgSpriteSwitch)
				{
					if (m_sptActiveBackground == null && m_imgBackground.sprite != null)
						m_sptActiveBackground = m_imgBackground.sprite;
				}
				else if (m_imgBackground.sprite == null && m_sptActiveBackground != null)
				{
					m_imgBackground.sprite = m_sptActiveBackground;
				}
			}
			if (m_txtLabel != null)
			{
				if (m_enableFontSizeSwitch && m_fontSizeActive == 0)
					m_fontSizeActive = m_txtLabel.fontSize;
				if (m_enableTextColorSwitch)
				{
					if (m_colorActiveText == default)
						m_colorActiveText = m_txtLabel.color;
				}
				else if (m_txtLabel.color == default && m_colorActiveText != default)
					m_txtLabel.color = m_colorActiveText;
			}
			if (m_enableSizeSwitch)
			{
				if (m_sizeActive == Vector2.zero)
					m_sizeActive = ((RectTransform)transform).sizeDelta;
			}

			if (group == null)
				group = gameObject.GetComponentInParent<ToggleGroup>();

			if (m_scaleBounceEffect)
			{
				if (transition == Transition.Animation)
					transition = Transition.None;

				if (gameObject.TryGetComponent(out Animator a))
					a.enabled = false;
			}
			else if (transition == Transition.Animation)
			{
				var _animator = gameObject.GetOrAddComponent<Animator>();
				_animator.updateMode = AnimatorUpdateMode.UnscaledTime;
				if (_animator.runtimeAnimatorController == null)
				{
					string animatorCtrlPath = AssetDatabase.GUIDToAssetPath("a32018778a1faa24fbd0f51f8de100a6");
					if (!string.IsNullOrEmpty(animatorCtrlPath))
					{
						var animatorCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorCtrlPath);
						if (animatorCtrl != null)
							_animator.runtimeAnimatorController = animatorCtrl;
					}
				}
				_animator.enabled = true;
			}
		}
#endif

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_scaleBounceEffect)
				transform.localScale = m_initialScale;
			
			onValueChanged.AddListener(OnValueChanged);

			Refresh();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			
			if (m_scaleBounceEffect)
				transform.localScale = m_initialScale;

			onValueChanged.RemoveListener(OnValueChanged);
		}

		public override void OnPointerClick(PointerEventData p_eventData)
		{
			if (isLocked)
			{
				onClickOnLock?.Invoke();
				return;
			}

			base.OnPointerClick(p_eventData);
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			
			if (m_scaleBounceEffect)
			{
#if DOTWEEN
				DOTween.Kill(GetInstanceID());
				transform
					.DOScale(m_initialScale * 0.9f, 0.125f)
					.SetUpdate(true)
					.SetEase(Ease.InOutBack)
					.SetId(GetInstanceID());
#else
				transform.localScale = m_initialScale * 0.95f;
#endif
			}
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			base.OnPointerUp(eventData);
			
			if (m_scaleBounceEffect)
			{
#if DOTWEEN
				DOTween.Kill(GetInstanceID());
				transform
					.DOScale(m_initialScale, 0.1f)
					.SetUpdate(true)
					.SetId(GetInstanceID());
#else
				transform.localScale = m_initialScale;
#endif
			}
		}

		private void Refresh()
		{
			if (m_contentsActive != null)
				foreach (var item in m_contentsActive)
					item.SetActive(isOn);

			if (m_contentsInactive != null)
				foreach (var item in m_contentsInactive)
					item.SetActive(!isOn);

			if (m_enableBgSpriteSwitch)
				m_imgBackground.sprite = isOn ? m_sptActiveBackground : m_sptInactiveBackground;

			if (m_enableSizeSwitch)
			{
				var size = isOn ? m_sizeActive : m_sizeInactive;
				if (gameObject.TryGetComponent(out LayoutElement layoutElement))
				{
					layoutElement.minWidth = size.x;
					layoutElement.minHeight = size.y;
				}
				else
					((RectTransform)transform).sizeDelta = size;
			}

			if (m_enableBgColorSwitch)
				m_imgBackground.color = isOn ? m_colorActiveBackground : m_colorInactiveBackground;

			if (m_enableTextColorSwitch)
			{
				if (m_txtLabel != null)
					m_txtLabel.color = isOn ? m_colorActiveText : m_colorInactiveText;

				if (m_additionalLabels != null)
					foreach (var label in m_additionalLabels)
						label.color = isOn ? m_colorActiveText : m_colorInactiveText;
			}

			if (m_enableFontSizeSwitch)
			{
				if (m_txtLabel != null)
					m_txtLabel.fontSize = isOn ? m_fontSizeActive : m_fontSizeInactive;

				if (m_additionalLabels != null)
					foreach (var label in m_additionalLabels)
						label.fontSize = isOn ? m_fontSizeActive : m_fontSizeInactive;
			}

			if (m_customToggleGroup != null && isOn)
				m_customToggleGroup.SetTarget(transform as RectTransform, m_tweenTime);
		}

		private void RefreshByTween()
		{
#if DOTWEEN
			if (m_isOn2 == isOn)
				return;

			m_isOn2 = isOn;

			if (m_contentsActive != null)
				foreach (var item in m_contentsActive)
					item.SetActive(isOn);

			if (m_contentsInactive != null)
				foreach (var item in m_contentsInactive)
					item.SetActive(!isOn);

			if (Application.isPlaying)
			{
				if (m_enableBgSpriteSwitch)
				{
					m_imgBackground.DOKill();
					m_imgBackground.sprite = isOn ? m_sptActiveBackground : m_sptInactiveBackground;
				}
				if (m_enableSizeSwitch || m_enableTextColorSwitch || m_enableBgColorSwitch || m_enableFontSizeSwitch)
				{
					m_customToggleGroup.SetToggleInteractable(false);
					var layoutElement = gameObject.GetComponent<LayoutElement>();
					var txtFromColor = !isOn ? m_colorActiveText : m_colorInactiveText;
					var txtToColor = isOn ? m_colorActiveText : m_colorInactiveText;
					var bgFromColor = !isOn ? m_colorActiveBackground : m_colorInactiveBackground;
					var bgToColor = isOn ? m_colorActiveBackground : m_colorInactiveBackground;
					var sizeFrom = !isOn ? m_sizeActive : m_sizeInactive;
					var sizeTo = isOn ? m_sizeActive : m_sizeInactive;
					float fontSizeFrom = !isOn ? m_fontSizeActive : m_fontSizeInactive;
					float fontSizeTo = isOn ? m_fontSizeActive : m_fontSizeInactive;
					float val = 0;
					var rectTransform = transform as RectTransform;
					DOTween.Kill(GetInstanceID());
					DOTween.To(() => val, p_x => val = p_x, 1, m_tweenTime)
						.OnUpdate(() =>
						{
							if (m_enableSizeSwitch)
							{
								var size = Vector2.Lerp(sizeFrom, sizeTo, val);
								if (layoutElement == null)
								{
									rectTransform.sizeDelta = size;
								}
								else
								{
									layoutElement.minWidth = size.x;
									layoutElement.minHeight = size.y;
								}
							}
							if (m_enableTextColorSwitch)
							{
								var color = Color.Lerp(txtFromColor, txtToColor, val);
								if (m_txtLabel != null)
									m_txtLabel.color = color;

								if (m_additionalLabels != null)
									foreach (var label in m_additionalLabels)
										label.color = color;
							}
							if (m_enableBgColorSwitch)
							{
								var color = Color.Lerp(bgFromColor, bgToColor, val);
								m_imgBackground.color = color;
							}
							if (m_enableFontSizeSwitch)
							{
								if (m_txtLabel != null)
									m_txtLabel.fontSize = Mathf.Lerp(fontSizeFrom, fontSizeTo, val);

								if (m_additionalLabels != null)
									foreach (var label in m_additionalLabels)
										label.fontSize = Mathf.Lerp(fontSizeFrom, fontSizeTo, val);
							}
						})
						.OnComplete(() =>
						{
							m_customToggleGroup.SetToggleInteractable(true);
							if (m_enableSizeSwitch)
							{
								var size = isOn ? m_sizeActive : m_sizeInactive;
								if (layoutElement == null)
								{
									rectTransform.sizeDelta = size;
								}
								else
								{
									layoutElement.minWidth = size.x;
									layoutElement.minHeight = size.y;
								}
							}
							if (m_enableTextColorSwitch)
							{
								if (m_txtLabel != null)
									m_txtLabel.color = txtToColor;

								if (m_additionalLabels != null)
									foreach (var label in m_additionalLabels)
										label.color = isOn ? m_colorActiveText : m_colorInactiveText;
							}
							if (m_enableBgColorSwitch)
								m_imgBackground.color = isOn ? m_colorActiveBackground : m_colorInactiveBackground;
							if (m_enableFontSizeSwitch)
							{
								if (m_txtLabel != null)
									m_txtLabel.fontSize = isOn ? m_fontSizeActive : m_fontSizeInactive;

								if (m_additionalLabels != null)
									foreach (var label in m_additionalLabels)
										label.fontSize = isOn ? m_fontSizeActive : m_fontSizeInactive;
							}
						})
						.SetId(GetInstanceID())
						.SetEase(Ease.OutCubic);
				}
				if (m_customToggleGroup != null)
					m_customToggleGroup.SetTarget(transform as RectTransform, m_tweenTime);
			}
#endif
		}

		private void OnValueChanged(bool p_pIsOn)
		{
			if (p_pIsOn && !string.IsNullOrEmpty(m_sfxClip))
				EventDispatcher.Raise(new Audio.SFXTriggeredEvent(m_sfxClip));
			else if (!p_pIsOn && !string.IsNullOrEmpty(m_sfxClipOff))
				EventDispatcher.Raise(new Audio.SFXTriggeredEvent(m_sfxClipOff));

#if DOTWEEN
			if (Application.isPlaying && m_tweenTime > 0 && transition != Transition.Animation)
			{
				RefreshByTween();
				return;
			}
#endif
			Refresh();
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(CustomToggleTab), true)]
		class CustomToggleTabEditor : UnityEditor.UI.ToggleEditor
		{
			private CustomToggleTab m_mToggle;

			protected override void OnEnable()
			{
				base.OnEnable();

				m_mToggle = (CustomToggleTab)target;
			}

			public override void OnInspectorGUI()
			{
				EditorGUILayout.BeginVertical("box");
				{
					EditorHelper.SerializeField(serializedObject, "m_imgBackground");
					EditorHelper.SerializeField(serializedObject, "m_txtLabel");
					EditorHelper.SerializeField(serializedObject, "m_contentsActive");
					EditorHelper.SerializeField(serializedObject, "m_contentsInactive");
					EditorHelper.SerializeField(serializedObject, "m_additionalLabels");
					EditorHelper.SerializeField(serializedObject, "m_sfxClip");
					EditorHelper.SerializeField(serializedObject, "m_scaleBounceEffect");

					var enableBgSpriteSwitch = EditorHelper.SerializeField(serializedObject, "m_enableBgSpriteSwitch");
					if (enableBgSpriteSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						EditorHelper.SerializeField(serializedObject, "m_sptActiveBackground");
						EditorHelper.SerializeField(serializedObject, "m_sptInactiveBackground");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableBgColorSwitch = EditorHelper.SerializeField(serializedObject, "m_enableBgColorSwitch");
					if (enableBgColorSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						EditorHelper.SerializeField(serializedObject, "m_colorActiveBackground");
						EditorHelper.SerializeField(serializedObject, "m_colorInactiveBackground");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableTextColorSwitch = EditorHelper.SerializeField(serializedObject, "m_enableTextColorSwitch");
					if (enableTextColorSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						EditorHelper.SerializeField(serializedObject, "m_colorActiveText");
						EditorHelper.SerializeField(serializedObject, "m_colorInactiveText");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableSizeSwitch = EditorHelper.SerializeField(serializedObject, "m_enableSizeSwitch");
					if (enableSizeSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						EditorHelper.SerializeField(serializedObject, "m_sizeActive");
						EditorHelper.SerializeField(serializedObject, "m_sizeInactive");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableFontSizeSwitch = EditorHelper.SerializeField(serializedObject, "m_enableFontSizeSwitch");
					if (enableFontSizeSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						EditorHelper.SerializeField(serializedObject, "m_fontSizeActive");
						EditorHelper.SerializeField(serializedObject, "m_fontSizeInactive");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					EditorHelper.SerializeField(serializedObject, "m_tweenTime");

					if (m_mToggle.m_txtLabel != null)
						m_mToggle.m_txtLabel.text = EditorGUILayout.TextField("Label", m_mToggle.m_txtLabel.text);

					serializedObject.ApplyModifiedProperties();
				}
				EditorGUILayout.EndVertical();

				base.OnInspectorGUI();
			}
		}
	}
#endif
}