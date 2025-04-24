/***
 * Author HNB-RaBear - 2019
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RCore.Inspector;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
	[AddComponentMenu("RCore/UI/JustToggle")]
	public class JustToggle : Toggle
	{
		[Serializable]
		public class SizeTransition
		{
			public RectTransform target;
			public LayoutElement layoutElement;
			public Vector3 on;
			public Vector3 off;
		}

		[Serializable]
		public class PositionTransition
		{
			public RectTransform target;
			public Vector3 on;
			public Vector3 off;
		}

		[Serializable]
		public class ColorTransition
		{
			public MaskableGraphic target;
			public Color on;
			public Color off;
		}

		[Serializable]
		public class SpriteTransition
		{
			public Image target;
			public Sprite on;
			public Sprite off;
		}

		public Image imgBackground;
		public TextMeshProUGUI txtLabel;
		public List<RectTransform> contentsActive;
		public List<RectTransform> contentsInactive;
		public string sfxClip = "button";
		public string sfxClipOff = "button";
		public bool scaleBounceEffect = true;

		public bool enableSizeSwitch;
		public Vector2 sizeActive;
		public Vector2 sizeInactive;

		public float tweenTime = 0.3f;
		public SizeTransition[] sizeTransitions;
		public PositionTransition[] positionTransitions;
		public ColorTransition[] colorTransitions;
		public SpriteTransition[] spriteTransitions;

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

			for (var i = contentsInactive.Count - 1; i >= 0; i--)
				if (contentsInactive[i] == null)
					contentsInactive.RemoveAt(i);

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

			foreach (var transition1 in sizeTransitions)
			{
				if (transition1.target == null)
					continue;
				if (transition1.layoutElement == null)
					transition1.target.TryGetComponent(out transition1.layoutElement);
				if (transition1.on == default)
					transition1.on = transition1.target.rect.size;
				if (transition1.off == default)
					transition1.off = transition1.target.rect.size;
			}
			foreach (var transition1 in positionTransitions)
			{
				if (transition1.target == null)
					continue;
				if (transition1.on == default)
					transition1.on = transition1.target.anchoredPosition;
				if (transition1.off == default)
					transition1.off = transition1.target.anchoredPosition;
			}
		}
#endif

		protected override void OnEnable()
		{
			base.OnEnable();
			
			if (m_initialScale == Vector2.zero)
				m_initialScale = Vector2.one;

			if (scaleBounceEffect)
				transform.localScale = m_initialScale;

			onValueChanged.AddListener(OnValueChanged);

			Refresh();
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (m_initialScale == Vector2.zero)
				m_initialScale = Vector2.one;
			
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

			foreach (var transition1 in sizeTransitions)
			{
				var size = isOn ? transition1.on : transition1.off;
				if (transition1.layoutElement != null)
				{
					transition1.layoutElement.minWidth = size.x;
					transition1.layoutElement.minHeight = size.y;
				}
				transition1.target.sizeDelta = size;
			}

			foreach (var transition1 in positionTransitions)
				transition1.target.anchoredPosition =
					isOn ? transition1.on : transition1.off;

			foreach (var transition1 in colorTransitions)
				transition1.target.color = isOn ? transition1.on : transition1.off;

			foreach (var transition1 in spriteTransitions)
				transition1.target.sprite = isOn ? transition1.on : transition1.off;

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
				if (enableSizeSwitch || sizeTransitions.Length > 0 || positionTransitions.Length > 0 || colorTransitions.Length > 0 || spriteTransitions.Length > 0)
				{
					if (m_customToggleGroup != null)
						m_customToggleGroup.SetToggleInteractable(false);
					var layoutElement = gameObject.GetComponent<LayoutElement>();
					var sizeFrom = !isOn ? sizeActive : sizeInactive;
					var sizeTo = isOn ? sizeActive : sizeInactive;
					float lerp = 0;
					var rectTransform = transform as RectTransform;
					DOTween.Kill(GetInstanceID() + 1);
					DOTween.To(() => lerp, p_x => lerp = p_x, 1, tweenTime)
						.OnUpdate(() =>
						{
							if (enableSizeSwitch)
							{
								var size = Vector2.Lerp(sizeFrom, sizeTo, lerp);
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

							foreach (var transition1 in sizeTransitions)
							{
								var size = isOn ? Vector2.Lerp(transition1.off, transition1.on, lerp) : Vector2.Lerp(transition1.on, transition1.off, lerp);
								if (transition1.layoutElement != null)
								{
									transition1.layoutElement.minWidth = size.x;
									transition1.layoutElement.minHeight = size.y;
								}
								transition1.target.sizeDelta = size;
							}

							foreach (var transition1 in positionTransitions)
								transition1.target.anchoredPosition =
									isOn ? Vector2.Lerp(transition1.off, transition1.on, lerp) : Vector2.Lerp(transition1.on, transition1.off, lerp);

							foreach (var transition1 in colorTransitions)
								transition1.target.color = isOn ? Color.Lerp(transition1.off, transition1.on, lerp) : Color.Lerp(transition1.on, transition1.off, lerp);

							foreach (var transition1 in spriteTransitions)
							{
								if (lerp < 0.5f)
								{
									// Lerp alpha of transition1.target to zero
									var tempColor = transition1.target.color;
									tempColor.a = Mathf.Lerp(1, 0.3f, lerp * 2); // Multiply by 2 because lerp is [0, 0.5]
									transition1.target.color = tempColor;
									transition1.target.sprite = isOn ? transition1.off : transition1.on;
								}
								else
								{
									// Lerp alpha of transition1.target to 1
									var tempColor = transition1.target.color;
									tempColor.a = Mathf.Lerp(0.3f, 1, (lerp - 0.5f) * 2);
									transition1.target.color = tempColor;
									transition1.target.sprite = isOn ? transition1.on : transition1.off;
								}
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

							foreach (var transition1 in sizeTransitions)
							{
								var size = isOn ? transition1.on : transition1.off;
								if (transition1.layoutElement != null)
								{
									transition1.layoutElement.minWidth = size.x;
									transition1.layoutElement.minHeight = size.y;
								}
								transition1.target.sizeDelta = size;
							}

							foreach (var transition1 in positionTransitions)
								transition1.target.anchoredPosition =
									isOn ? transition1.on : transition1.off;

							foreach (var transition1 in colorTransitions)
								transition1.target.color = isOn ? transition1.on : transition1.off;

							foreach (var transition1 in spriteTransitions)
								transition1.target.sprite = isOn ? transition1.on : transition1.off;
						})
						.SetId(GetInstanceID() + 1)
						.SetEase(Ease.OutCubic)
						.SetUpdate(true);
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
				if (IsOn && !string.IsNullOrEmpty(sfxClip))
					EventDispatcher.Raise(new Audio.UISfxTriggeredEvent(sfxClip));
				else if (!isOn && !string.IsNullOrEmpty(sfxClipOff))
					EventDispatcher.Raise(new Audio.UISfxTriggeredEvent(sfxClipOff));
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

		IEnumerator SpriteLerp(SpriteRenderer spriteRenderer, Sprite fromSprite, Sprite toSprite, float duration)
		{
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				spriteRenderer.sprite = t < 0.5f ? fromSprite : toSprite; // Simplest form of "lerping"
				yield return null;
			}
			spriteRenderer.sprite = toSprite;
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(JustToggle), true)]
		public class CustomToggleTabEditor : UnityEditor.UI.ToggleEditor
		{
			private JustToggle m_mToggle;

			protected override void OnEnable()
			{
				base.OnEnable();

				m_mToggle = (JustToggle)target;
			}

			public override void OnInspectorGUI()
			{
				serializedObject.SerializeField(nameof(imgBackground));
				serializedObject.SerializeField(nameof(txtLabel));
				serializedObject.SerializeField(nameof(contentsActive));
				serializedObject.SerializeField(nameof(contentsInactive));
				serializedObject.SerializeField(nameof(sfxClip));
				serializedObject.SerializeField(nameof(sfxClipOff));
				serializedObject.SerializeField(nameof(scaleBounceEffect));

				var enableSizeSwitch1 = serializedObject.SerializeField(nameof(enableSizeSwitch));
				if (enableSizeSwitch1.boolValue)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.BeginVertical("box");
					serializedObject.SerializeField(nameof(sizeActive));
					serializedObject.SerializeField(nameof(sizeInactive));
					EditorGUILayout.EndVertical();
					EditorGUI.indentLevel--;
				}

				serializedObject.SerializeField(nameof(sizeTransitions));
				serializedObject.SerializeField(nameof(positionTransitions));
				serializedObject.SerializeField(nameof(colorTransitions));
				serializedObject.SerializeField(nameof(spriteTransitions));
				serializedObject.SerializeField(nameof(tweenTime));

				if (m_mToggle.txtLabel != null)
					m_mToggle.txtLabel.text = EditorGUILayout.TextField("Label", m_mToggle.txtLabel.text);

				serializedObject.ApplyModifiedProperties();

				EditorHelper.Separator();

				base.OnInspectorGUI();
			}
		}
#endif
	}
}