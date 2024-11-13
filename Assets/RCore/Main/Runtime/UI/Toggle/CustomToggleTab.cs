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
		[FormerlySerializedAs("m_imgBackground")]
		public Image imgBackground;
		[FormerlySerializedAs("m_txtLabel")]
		public TextMeshProUGUI txtLabel;
		[FormerlySerializedAs("m_contentsActive")]
		public List<RectTransform> contentsActive;
		[FormerlySerializedAs("m_contentsInactive")]
		public List<RectTransform> contentsInactive;
		[FormerlySerializedAs("m_additionalLabels")]
		public List<TextMeshProUGUI> additionalLabels;
		public TapFeedback tapFeedback = TapFeedback.Haptic;
		[FormerlySerializedAs("m_sfxClip")]
		public string sfxClip = "button";
		[FormerlySerializedAs("m_sfxClipOff")]
		public string sfxClipOff = "button";
		[FormerlySerializedAs("m_scaleBounceEffect")]
		public bool scaleBounceEffect = true;

		[FormerlySerializedAs("m_enableBgSpriteSwitch")]
		public bool enableBgSpriteSwitch;
		[FormerlySerializedAs("m_sptActiveBackground")]
		public Sprite sptActiveBackground;
		[FormerlySerializedAs("m_sptInactiveBackground")]
		public Sprite sptInactiveBackground;

		[FormerlySerializedAs("m_enableBgColorSwitch")]
		public bool enableBgColorSwitch;
		[FormerlySerializedAs("m_colorActiveBackground")]
		public Color colorActiveBackground;
		[FormerlySerializedAs("m_colorInactiveBackground")]
		public Color colorInactiveBackground;

		[FormerlySerializedAs("m_enableTextColorSwitch")]
		public bool enableTextColorSwitch;
		[FormerlySerializedAs("m_colorActiveText")]
		public Color colorActiveText;
		[FormerlySerializedAs("m_colorInactiveText")]
		public Color colorInactiveText;

		[FormerlySerializedAs("m_enableSizeSwitch")]
		public bool enableSizeSwitch;
		[FormerlySerializedAs("m_sizeActive")]
		public Vector2 sizeActive;
		[FormerlySerializedAs("m_sizeInactive")]
		public Vector2 sizeInactive;

		[FormerlySerializedAs("m_enableFontSizeSwitch")]
		public bool enableFontSizeSwitch;
		[FormerlySerializedAs("m_fontSizeActive")]
		public float fontSizeActive;
		[FormerlySerializedAs("m_fontSizeInactive")]
		public float fontSizeInactive;

		[FormerlySerializedAs("m_tweenTime")]
		public float tweenTime = 0.3f;

		[ReadOnly] public bool isLocked;

		public Action onClickOnLock;

		private CustomToggleGroup m_customToggleGroup;
		private bool m_isOn2;
		private Vector2 m_initialScale;
		private bool m_clicked;

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

			if (txtLabel == null)
				txtLabel = gameObject.FindComponentInChildren<TextMeshProUGUI>();

			if (imgBackground == null)
			{
				var images = gameObject.GetComponentsInChildren<Image>();
				imgBackground = images[0];
			}

			if (imgBackground != null)
			{
				if (enableBgColorSwitch)
				{
					if (colorActiveBackground == default)
						colorActiveBackground = imgBackground.color;
				}
				else if (imgBackground.color == default && colorActiveBackground != default)
				{
					imgBackground.color = colorActiveBackground;
				}
				if (enableBgSpriteSwitch)
				{
					if (sptActiveBackground == null && imgBackground.sprite != null)
						sptActiveBackground = imgBackground.sprite;
				}
				else if (imgBackground.sprite == null && sptActiveBackground != null)
				{
					imgBackground.sprite = sptActiveBackground;
				}
			}
			if (txtLabel != null)
			{
				if (enableFontSizeSwitch && fontSizeActive == 0)
					fontSizeActive = txtLabel.fontSize;
				if (enableTextColorSwitch)
				{
					if (colorActiveText == default)
						colorActiveText = txtLabel.color;
				}
				else if (txtLabel.color == default && colorActiveText != default)
					txtLabel.color = colorActiveText;
			}
			if (enableSizeSwitch)
			{
				if (sizeActive == Vector2.zero)
					sizeActive = ((RectTransform)transform).sizeDelta;
			}

			if (group == null)
				group = gameObject.GetComponentInParent<ToggleGroup>();

			if (scaleBounceEffect)
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

			if (scaleBounceEffect)
				transform.localScale = m_initialScale;

			onValueChanged.AddListener(OnValueChanged);

			Refresh();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			
			if (scaleBounceEffect)
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
			m_clicked = true;
			base.OnPointerClick(p_eventData);
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			
			if (!isLocked && scaleBounceEffect)
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
			
			if (!isLocked && scaleBounceEffect)
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
			if (contentsActive != null)
				foreach (var item in contentsActive)
					item.gameObject.SetActive(isOn);

			if (contentsInactive != null)
				foreach (var item in contentsInactive)
					item.gameObject.SetActive(!isOn);

			if (enableBgSpriteSwitch)
				imgBackground.sprite = isOn ? sptActiveBackground : sptInactiveBackground;

			if (enableSizeSwitch)
			{
				var size = isOn ? sizeActive : sizeInactive;
				if (gameObject.TryGetComponent(out LayoutElement layoutElement))
				{
					layoutElement.minWidth = size.x;
					layoutElement.minHeight = size.y;
				}
				else
					((RectTransform)transform).sizeDelta = size;
			}

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

			if (m_customToggleGroup != null && isOn)
				m_customToggleGroup.SetTarget(transform as RectTransform, tweenTime);
		}

		private void RefreshByTween()
		{
#if DOTWEEN
			if (m_isOn2 == isOn)
				return;

			m_isOn2 = isOn;

			if (contentsActive != null)
				foreach (var item in contentsActive)
					item.gameObject.SetActive(isOn);

			if (contentsInactive != null)
				foreach (var item in contentsInactive)
					item.gameObject.SetActive(!isOn);

			if (Application.isPlaying)
			{
				if (enableBgSpriteSwitch)
				{
					imgBackground.DOKill();
					imgBackground.sprite = isOn ? sptActiveBackground : sptInactiveBackground;
				}
				if (enableSizeSwitch || enableTextColorSwitch || enableBgColorSwitch || enableFontSizeSwitch)
				{
					if (m_customToggleGroup != null)
						m_customToggleGroup.SetToggleInteractable(false);
					var layoutElement = gameObject.GetComponent<LayoutElement>();
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
					DOTween.Kill(GetInstanceID() + 1);
					DOTween.To(() => val, p_x => val = p_x, 1, tweenTime)
						.OnUpdate(() =>
						{
							if (enableSizeSwitch)
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
							m_customToggleGroup.SetToggleInteractable(true);
							if (enableSizeSwitch)
							{
								var size = isOn ? sizeActive : sizeInactive;
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
						.SetId(GetInstanceID() + 1)
						.SetEase(Ease.OutCubic);
				}
				if (m_customToggleGroup != null)
					m_customToggleGroup.SetTarget(transform as RectTransform, tweenTime);
			}
#endif
		}

		private void OnValueChanged(bool p_pIsOn)
		{
			if (m_clicked)
			{
				if (tapFeedback == TapFeedback.Haptic || tapFeedback == TapFeedback.SoundAndHaptic)
					Vibration.VibratePop();
				if (tapFeedback == TapFeedback.Sound || tapFeedback == TapFeedback.SoundAndHaptic)
				{
					if (IsOn && !string.IsNullOrEmpty(sfxClip))
						EventDispatcher.Raise(new Audio.SFXTriggeredEvent(sfxClip));
					else if (!isOn && !string.IsNullOrEmpty(sfxClipOff))
						EventDispatcher.Raise(new Audio.SFXTriggeredEvent(sfxClipOff));
				}
				m_clicked = false;
			}
#if DOTWEEN
			if (Application.isPlaying && tweenTime > 0 && transition != Transition.Animation)
			{
				RefreshByTween();
				return;
			}
#endif
			Refresh();
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(CustomToggleTab), true)]
		public class CustomToggleTabEditor : UnityEditor.UI.ToggleEditor
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
					serializedObject.SerializeField("imgBackground");
					serializedObject.SerializeField("txtLabel");
					serializedObject.SerializeField("contentsActive");
					serializedObject.SerializeField("contentsInactive");
					serializedObject.SerializeField("additionalLabels");
					serializedObject.SerializeField("sfxClip");
					serializedObject.SerializeField("sfxClipOff");
					serializedObject.SerializeField("scaleBounceEffect");
					serializedObject.SerializeField("tapFeedback");

					var enableBgSpriteSwitch = serializedObject.SerializeField("enableBgSpriteSwitch");
					if (enableBgSpriteSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField("sptActiveBackground");
						serializedObject.SerializeField("sptInactiveBackground");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableBgColorSwitch = serializedObject.SerializeField("enableBgColorSwitch");
					if (enableBgColorSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField("colorActiveBackground");
						serializedObject.SerializeField("colorInactiveBackground");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableTextColorSwitch = serializedObject.SerializeField("enableTextColorSwitch");
					if (enableTextColorSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField("colorActiveText");
						serializedObject.SerializeField("colorInactiveText");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableSizeSwitch = serializedObject.SerializeField("enableSizeSwitch");
					if (enableSizeSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField("sizeActive");
						serializedObject.SerializeField("sizeInactive");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var enableFontSizeSwitch = serializedObject.SerializeField("enableFontSizeSwitch");
					if (enableFontSizeSwitch.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField("fontSizeActive");
						serializedObject.SerializeField("fontSizeInactive");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					serializedObject.SerializeField("tweenTime");

					if (m_mToggle.txtLabel != null)
						m_mToggle.txtLabel.text = EditorGUILayout.TextField("Label", m_mToggle.txtLabel.text);

					serializedObject.ApplyModifiedProperties();
				}
				EditorGUILayout.EndVertical();

				base.OnInspectorGUI();
			}
		}
#endif
	}
}