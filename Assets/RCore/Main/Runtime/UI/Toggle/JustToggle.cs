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
	/// <summary>
	/// An advanced UI Toggle component that extends the standard Unity Toggle with additional features
	/// such as complex transitions (size, position, color, sprite), sound effects, and animations.
	/// It provides enhanced visual feedback and interactivity.
	/// </summary>
	[AddComponentMenu("RCore/UI/JustToggle")]
	public class JustToggle : Toggle
	{
#region Nested Classes

		/// <summary>
		/// Defines a size change transition for a target RectTransform.
		/// </summary>
		[Serializable]
		public class SizeTransition
		{
			/// <summary> The RectTransform to resize. </summary>
			public RectTransform target;
			/// <summary> Optional LayoutElement on the target to control its size. </summary>
			public LayoutElement layoutElement;
			/// <summary> The size of the target when the toggle is ON. </summary>
			public Vector3 on;
			/// <summary> The size of the target when the toggle is OFF. </summary>
			public Vector3 off;
		}

		/// <summary>
		/// Defines a position change transition for a target RectTransform.
		/// </summary>
		[Serializable]
		public class PositionTransition
		{
			/// <summary> The RectTransform to move. </summary>
			public RectTransform target;
			/// <summary> The anchored position of the target when the toggle is ON. </summary>
			public Vector3 on;
			/// <summary> The anchored position of the target when the toggle is OFF. </summary>
			public Vector3 off;
		}

		/// <summary>
		/// Defines a color change transition for a target MaskableGraphic (e.g., Image, Text).
		/// </summary>
		[Serializable]
		public class ColorTransition
		{
			/// <summary> The graphic component to change the color of. </summary>
			public MaskableGraphic target;
			/// <summary> The color of the target when the toggle is ON. </summary>
			public Color on;
			/// <summary> The color of the target when the toggle is OFF. </summary>
			public Color off;
		}

		/// <summary>
		/// Defines a sprite change transition for a target Image.
		/// </summary>
		[Serializable]
		public class SpriteTransition
		{
			/// <summary> The Image component to change the sprite of. </summary>
			public Image target;
			/// <summary> The sprite to display when the toggle is ON. </summary>
			public Sprite on;
			/// <summary> The sprite to display when the toggle is OFF. </summary>
			public Sprite off;
		}

#endregion

#region Public Fields

		/// <summary> Optional background image of the toggle. </summary>
		public Image imgBackground;
		/// <summary> Optional TextMeshPro label for the toggle. </summary>
		public TextMeshProUGUI txtLabel;
		/// <summary> A list of GameObjects to activate when the toggle is ON. </summary>
		public List<RectTransform> contentsActive;
		/// <summary> A list of GameObjects to activate when the toggle is OFF. </summary>
		public List<RectTransform> contentsInactive;
		/// <summary> The name of the sound effect clip to play when the toggle is turned ON. </summary>
		public string sfxClip = "button";
		/// <summary> The name of the sound effect clip to play when the toggle is turned OFF. </summary>
		public string sfxClipOff = "button";
		/// <summary> If true, the toggle will perform a scaling animation when clicked. </summary>
		public bool scaleBounceEffect = true;

		/// <summary> If true, enables resizing of this toggle's RectTransform between its ON and OFF states. </summary>
		public bool enableSizeSwitch;
		/// <summary> The size of the toggle's RectTransform when it is ON. </summary>
		[Tooltip("The size of the toggle's RectTransform when it is ON.")]
		public Vector2 sizeActive;
		/// <summary> The size of the toggle's RectTransform when it is OFF. </summary>
		[Tooltip("The size of the toggle's RectTransform when it is OFF.")]
		public Vector2 sizeInactive;

		/// <summary> The duration of the tween animations for all transitions. </summary>
		public float tweenTime = 0.3f;
		/// <summary> An array of size transitions to perform when the toggle state changes. </summary>
		public SizeTransition[] sizeTransitions;
		/// <summary> An array of position transitions to perform when the toggle state changes. </summary>
		public PositionTransition[] positionTransitions;
		/// <summary> An array of color transitions to perform when the toggle state changes. </summary>
		public ColorTransition[] colorTransitions;
		/// <summary> An array of sprite transitions to perform when the toggle state changes. </summary>
		public SpriteTransition[] spriteTransitions;

		/// <summary> If true, the toggle cannot be interacted with. </summary>
		[ReadOnly] public bool isLocked;

		/// <summary> An action that is invoked when the user clicks on the toggle while it is locked. </summary>
		public Action onClickOnLock;

#endregion

#region Private Fields

		private CustomToggleGroup m_customToggleGroup;
		private bool m_isOn2;
		private Vector2 m_initialScale;
		private bool m_clicked;

#endregion

		/// <summary>
		/// Gets or sets the ON/OFF state of the toggle.
		/// This property ensures the onValueChanged event is properly invoked when the state changes programmatically.
		/// </summary>
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
		/// <summary>
		/// (Editor-only) Automatically called when the script is loaded or a value is changed in the Inspector.
		/// </summary>
		protected override void OnValidate()
		{
			if (Application.isPlaying)
				return;

			base.OnValidate();

			// Auto-assign components if they are not set
			if (txtLabel == null)
				txtLabel = gameObject.FindComponentInChildren<TextMeshProUGUI>();

			if (imgBackground == null)
			{
				var images = gameObject.GetComponentsInChildren<Image>();
				if (images.Length > 0)
					imgBackground = images[0];
			}

			// Clean up null entries in lists
			if (contentsInactive != null)
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

			// Configure animator based on settings
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
					// Attempt to find and assign a default animator controller
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

			// Initialize transition default values
			if (sizeTransitions != null)
				foreach (var transition1 in sizeTransitions)
				{
					if (transition1.target == null) continue;
					if (transition1.layoutElement == null) transition1.target.TryGetComponent(out transition1.layoutElement);
					if (transition1.on == default) transition1.on = transition1.target.rect.size;
					if (transition1.off == default) transition1.off = transition1.target.rect.size;
				}
			if (positionTransitions != null)
				foreach (var transition1 in positionTransitions)
				{
					if (transition1.target == null) continue;
					if (transition1.on == default) transition1.on = transition1.target.anchoredPosition;
					if (transition1.off == default) transition1.off = transition1.target.anchoredPosition;
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

			// Apply initial visual state
			Refresh();
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (m_initialScale == Vector2.zero)
				m_initialScale = Vector2.one;

			// Reset scale on disable
			if (scaleBounceEffect)
				transform.localScale = m_initialScale;

			onValueChanged.RemoveListener(OnValueChanged);
		}

		/// <summary>
		/// Handles pointer click events.
		/// Prevents interaction if locked and plays sounds.
		/// </summary>
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

		/// <summary>
		/// Handles pointer down events.
		/// Triggers the scale-down part of the bounce effect.
		/// </summary>
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

		/// <summary>
		/// Handles pointer up events.
		/// Triggers the scale-up part of the bounce effect.
		/// </summary>
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

		/// <summary>
		/// Instantly updates the visual state of the toggle and all its transitions
		/// to match the current 'isOn' state.
		/// </summary>
		private void Refresh()
		{
			// Active/Inactive content
			if (contentsActive != null)
				foreach (var item in contentsActive)
					item.gameObject.SetActive(isOn);

			if (contentsInactive != null)
				foreach (var item in contentsInactive)
					item.gameObject.SetActive(!isOn);

			// Main toggle size
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

			// Apply all transitions
			if (sizeTransitions != null)
				foreach (var transition1 in sizeTransitions)
				{
					var size = isOn ? transition1.on : transition1.off;
					if (transition1.layoutElement != null)
					{
						transition1.layoutElement.minWidth = size.x;
						transition1.layoutElement.minHeight = size.y;
					}
					if (transition1.target != null)
						transition1.target.sizeDelta = size;
				}

			if (positionTransitions != null)
				foreach (var transition1 in positionTransitions)
					if (transition1.target != null)
						transition1.target.anchoredPosition = isOn ? transition1.on : transition1.off;

			if (colorTransitions != null)
				foreach (var transition1 in colorTransitions)
					if (transition1.target != null)
						transition1.target.color = isOn ? transition1.on : transition1.off;

			if (spriteTransitions != null)
				foreach (var transition1 in spriteTransitions)
					if (transition1.target != null)
						transition1.target.sprite = isOn ? transition1.on : transition1.off;

			if (m_customToggleGroup != null && isOn)
				m_customToggleGroup.SetTarget(transform as RectTransform, tweenTime);
		}

		/// <summary>
		/// Smoothly animates the visual state of the toggle and its transitions
		/// over 'tweenTime' seconds. Requires DOTween.
		/// </summary>
		private void RefreshByTween()
		{
#if DOTWEEN
			if (m_isOn2 == isOn)
				return;

			m_isOn2 = isOn;

			// Activate/deactivate content immediately
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
					// Disable interaction during tween to prevent issues
					if (m_customToggleGroup != null)
						m_customToggleGroup.SetToggleInteractable(false);

					var layoutElement = gameObject.GetComponent<LayoutElement>();
					var sizeFrom = !isOn ? sizeActive : sizeInactive;
					var sizeTo = isOn ? sizeActive : sizeInactive;
					float lerp = 0;
					var rectTransform = transform as RectTransform;
					DOTween.Kill(GetInstanceID() + 1);

					// Main tween loop
					DOTween.To(() => lerp, p_x => lerp = p_x, 1, tweenTime)
						.OnUpdate(() =>
						{
							// Animate this toggle's size
							if (enableSizeSwitch)
							{
								var size = Vector2.Lerp(sizeFrom, sizeTo, lerp);
								if (layoutElement == null)
									rectTransform.sizeDelta = size;
								else
								{
									layoutElement.minWidth = size.x;
									layoutElement.minHeight = size.y;
								}
							}

							// Animate size transitions
							foreach (var transition1 in sizeTransitions)
							{
								if (transition1.target == null) continue;
								var size = isOn ? Vector2.Lerp(transition1.off, transition1.on, lerp) : Vector2.Lerp(transition1.on, transition1.off, lerp);
								if (transition1.layoutElement != null)
								{
									transition1.layoutElement.minWidth = size.x;
									transition1.layoutElement.minHeight = size.y;
								}
								transition1.target.sizeDelta = size;
							}

							// Animate position transitions
							foreach (var transition1 in positionTransitions)
								if (transition1.target != null)
									transition1.target.anchoredPosition = isOn ? Vector2.Lerp(transition1.off, transition1.on, lerp) : Vector2.Lerp(transition1.on, transition1.off, lerp);

							// Animate color transitions
							foreach (var transition1 in colorTransitions)
								if (transition1.target != null)
									transition1.target.color = isOn ? Color.Lerp(transition1.off, transition1.on, lerp) : Color.Lerp(transition1.on, transition1.off, lerp);

							// Animate sprite transitions with a cross-fade effect
							foreach (var transition1 in spriteTransitions)
							{
								if (transition1.target == null) continue;
								if (lerp < 0.5f)
								{
									var tempColor = transition1.target.color;
									tempColor.a = Mathf.Lerp(1, 0.3f, lerp * 2);
									transition1.target.color = tempColor;
									transition1.target.sprite = isOn ? transition1.off : transition1.on;
								}
								else
								{
									var tempColor = transition1.target.color;
									tempColor.a = Mathf.Lerp(0.3f, 1, (lerp - 0.5f) * 2);
									transition1.target.color = tempColor;
									transition1.target.sprite = isOn ? transition1.on : transition1.off;
								}
							}
						})
						.OnComplete(() =>
						{
							// Re-enable interaction and set final values
							if (m_customToggleGroup != null)
								m_customToggleGroup.SetToggleInteractable(true);
							Refresh(); // Set final state precisely
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

		/// <summary>
		/// Callback for the onValueChanged event. Triggers sounds and visual updates.
		/// </summary>
		private void OnValueChanged(bool p_pIsOn)
		{
			// Play sound only if triggered by a user click
			if (m_clicked)
			{
				if (IsOn && !string.IsNullOrEmpty(sfxClip))
					EventDispatcher.Raise(new Audio.UISfxTriggeredEvent(sfxClip));
				else if (!isOn && !string.IsNullOrEmpty(sfxClipOff))
					EventDispatcher.Raise(new Audio.UISfxTriggeredEvent(sfxClipOff));
				m_clicked = false;
			}

#if DOTWEEN
			// Use tweened refresh if applicable
			if (Application.isPlaying && tweenTime > 0 && transition != Transition.Animation)
			{
				RefreshByTween();
				return;
			}
#endif
			// Otherwise, refresh instantly
			Refresh();
		}

		/// <summary>
		/// Coroutine to simulate a sprite "lerp" by swapping them halfway through the duration.
		/// </summary>
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
		/// <summary>
		/// Custom editor for the JustToggle component to provide a more organized and user-friendly Inspector.
		/// </summary>
		[CustomEditor(typeof(JustToggle), true)]
		public class CustomToggleTabEditor : UnityEditor.UI.ToggleEditor
		{
			private JustToggle m_mToggle;

			// Add SerializedProperty fields for your arrays
			private SerializedProperty imgBackgroundProp;
			private SerializedProperty txtLabelProp;
			private SerializedProperty contentsActiveProp;
			private SerializedProperty contentsInactiveProp;
			private SerializedProperty sfxClipProp;
			private SerializedProperty sfxClipOffProp;
			private SerializedProperty scaleBounceEffectProp;
			private SerializedProperty enableSizeSwitchProp;
			private SerializedProperty sizeActiveProp;
			private SerializedProperty sizeInactiveProp;
			private SerializedProperty tweenTimeProp;
			private SerializedProperty sizeTransitionsProp;
			private SerializedProperty positionTransitionsProp;
			private SerializedProperty colorTransitionsProp;
			private SerializedProperty spriteTransitionsProp;


			protected override void OnEnable()
			{
				base.OnEnable();
				m_mToggle = (JustToggle)target;

				// Find the properties by name
				imgBackgroundProp = serializedObject.FindProperty(nameof(m_mToggle.imgBackground));
				txtLabelProp = serializedObject.FindProperty(nameof(m_mToggle.txtLabel));
				contentsActiveProp = serializedObject.FindProperty(nameof(m_mToggle.contentsActive));
				contentsInactiveProp = serializedObject.FindProperty(nameof(m_mToggle.contentsInactive));
				sfxClipProp = serializedObject.FindProperty(nameof(m_mToggle.sfxClip));
				sfxClipOffProp = serializedObject.FindProperty(nameof(m_mToggle.sfxClipOff));
				scaleBounceEffectProp = serializedObject.FindProperty(nameof(m_mToggle.scaleBounceEffect));
				enableSizeSwitchProp = serializedObject.FindProperty(nameof(m_mToggle.enableSizeSwitch));
				sizeActiveProp = serializedObject.FindProperty(nameof(m_mToggle.sizeActive));
				sizeInactiveProp = serializedObject.FindProperty(nameof(m_mToggle.sizeInactive));
				tweenTimeProp = serializedObject.FindProperty(nameof(m_mToggle.tweenTime));
				sizeTransitionsProp = serializedObject.FindProperty(nameof(m_mToggle.sizeTransitions));
				positionTransitionsProp = serializedObject.FindProperty(nameof(m_mToggle.positionTransitions));
				colorTransitionsProp = serializedObject.FindProperty(nameof(m_mToggle.colorTransitions));
				spriteTransitionsProp = serializedObject.FindProperty(nameof(m_mToggle.spriteTransitions));
			}

			public override void OnInspectorGUI()
			{
				// It's good practice to call this at the start
				serializedObject.Update();

				// Custom fields section
				EditorGUILayout.LabelField("JustToggle Properties", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(imgBackgroundProp);
				EditorGUILayout.PropertyField(txtLabelProp);
				EditorGUILayout.PropertyField(contentsActiveProp);
				EditorGUILayout.PropertyField(contentsInactiveProp);
				EditorGUILayout.PropertyField(sfxClipProp);
				EditorGUILayout.PropertyField(sfxClipOffProp);
				EditorGUILayout.PropertyField(scaleBounceEffectProp);

				// Size Switch section
				EditorGUILayout.PropertyField(enableSizeSwitchProp);
				if (enableSizeSwitchProp.boolValue)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.BeginVertical("box");
					EditorGUILayout.PropertyField(sizeActiveProp);
					EditorGUILayout.PropertyField(sizeInactiveProp);
					EditorGUILayout.EndVertical();
					EditorGUI.indentLevel--;
				}

				// Transitions section
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(tweenTimeProp);
				EditorGUILayout.PropertyField(sizeTransitionsProp);
				EditorGUILayout.PropertyField(positionTransitionsProp);
				EditorGUILayout.PropertyField(colorTransitionsProp);
				EditorGUILayout.PropertyField(spriteTransitionsProp);


				// Live label editing
				if (m_mToggle.txtLabel != null)
				{
					// Use EditorGUI.BeginChangeCheck to save changes to the text component
					EditorGUI.BeginChangeCheck();
					string newLabelText = EditorGUILayout.TextField("Label", m_mToggle.txtLabel.text);
					if (EditorGUI.EndChangeCheck())
					{
						Undo.RecordObject(m_mToggle.txtLabel, "Change Label Text");
						m_mToggle.txtLabel.text = newLabelText;
					}
				}

				// Apply any changes made to the serialized properties
				serializedObject.ApplyModifiedProperties();

				EditorHelper.Separator();

				// Default Toggle Inspector
				EditorGUILayout.LabelField("Base Toggle Properties", EditorStyles.boldLabel);
				base.OnInspectorGUI();
			}
		}
#endif
	}
}