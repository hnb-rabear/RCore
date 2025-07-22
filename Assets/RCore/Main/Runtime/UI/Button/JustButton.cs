/***
 * Author HNB-RaBear - 2018
 **/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
#if DOTWEEN
using DG.Tweening;
#endif
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
using UnityEditor.UI;
#endif

namespace RCore.UI
{
	/// <summary>
	/// An enhanced version of the standard Unity Button.
	/// It includes additional features like a scale bounce effect on click, sound effects,
	/// automatic greyscale for the disabled state, sprite swapping, and automatic aspect ratio
	/// correction for sliced images to prevent distortion.
	/// </summary>
	[AddComponentMenu("RCore/UI/JustButton")]
	public class JustButton : Button
	{
		private static Material m_GreyMat;

		[SerializeField] protected Image m_img;

		/// <summary>If true, the button will perform a scaling animation when pressed and released.</summary>
		public bool scaleBounceEffect = true;
		/// <summary>Controls how the image's pixelsPerUnitMultiplier is adjusted for sliced images to prevent stretching.</summary>
		public PerfectRatio perfectRatio = PerfectRatio.Height;
		/// <summary>If true, the button's image will be rendered with a greyscale material when disabled.</summary>
		public bool greyscaleEffect;
		/// <summary>If true, the button will swap between 'imgOn' and 'imgOff' sprites when its state changes.</summary>
		public bool imgOnOffSwap;
		/// <summary>The sprite to display when the button is in its 'On' or enabled state.</summary>
		public Sprite imgOn;
		/// <summary>The sprite to display when the button is in its 'Off' or disabled state.</summary>
		public Sprite imgOff;
		/// <summary>The name of the sound effect clip to play when the button is clicked.</summary>
		public string clickSfx = "button";

		/// <summary>
		/// Gets the Image component of the button. It defaults to the targetGraphic.
		/// </summary>
		public Image img
		{
			get
			{
				if (m_img == null)
					m_img = targetGraphic as Image;
				return m_img;
			}
		}

		/// <summary>
		/// Gets or sets the material of the button's Image component.
		/// </summary>
		public Material imgMaterial { get => img.material; set => img.material = value; }

		/// <summary>
		/// Gets the RectTransform of the button's target graphic.
		/// </summary>
		public RectTransform rectTransform => targetGraphic.rectTransform;

		private Action m_inactionStateAction;
		private bool m_active = true;
		private int m_perfectSpriteId;
		private Vector3 m_initialScale;

		protected override void Awake()
		{
			base.Awake();

			// Store the initial scale for use in animations and state resets.
			m_initialScale = transform.localScale;
		}

		/// <summary>
		/// Sets the button's logical enabled state. This is different from the base `enabled` property.
		/// It controls visual states like greyscaling or sprite swapping.
		/// </summary>
		/// <param name="pValue">True to enable the button, false to disable it.</param>
		public virtual void SetEnable(bool pValue)
		{
			m_active = pValue;
			enabled = pValue || m_inactionStateAction != null;
			if (pValue)
			{
				if (imgOnOffSwap)
					m_img.sprite = imgOn;
				else
					imgMaterial = null; // Reset to default material
			}
			else
			{
				if (imgOnOffSwap)
				{
					m_img.sprite = imgOff;
				}
				else
				{
					if (m_initialScale != Vector3.zero)
						transform.localScale = m_initialScale;

					// Use grey material for disabled state if enabled
					if (greyscaleEffect)
						imgMaterial = GetGreyMat();
				}
			}
		}

		/// <summary>
		/// Sets an action to be executed when the button is clicked while in its "inactive" (m_active = false) state.
		/// This allows for feedback (e.g., a "locked" sound) even when the primary onClick event is disabled.
		/// </summary>
		/// <param name="pAction">The action to execute.</param>
		public virtual void SetInactiveStateAction(Action pAction)
		{
			m_inactionStateAction = pAction;
			// Keep the component enabled to receive pointer events if there's an inactive state action
			enabled = m_active || m_inactionStateAction != null;
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			
			// Reset scale when the GameObject is disabled to avoid visual glitches.
			if (scaleBounceEffect)
				transform.localScale = m_initialScale;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			
			// Reset scale when the GameObject is enabled.
			if (scaleBounceEffect)
				transform.localScale = m_initialScale;
			
			// Check if the sprite has changed to re-apply the perfect ratio logic.
			if (m_img != null && m_img.sprite != null && m_perfectSpriteId != m_img.sprite.GetInstanceID())
				CheckPerfectRatio();
		}

		/// <summary>
		/// Handles pointer down events. Triggers sound effects and the bounce animation.
		/// Also handles the inactive state action if the button is not active.
		/// </summary>
		public override void OnPointerDown(PointerEventData eventData)
		{
			if (m_active)
			{
				base.OnPointerDown(eventData);

				if (!string.IsNullOrEmpty(clickSfx))
					EventDispatcher.Raise(new Audio.UISfxTriggeredEvent(clickSfx));
			}
			else if (m_inactionStateAction != null)
			{
				m_inactionStateAction();
				// Manually trigger the "Pressed" animation state if an Animator is present
				if (TryGetComponent(out Animator component) && component.enabled)
						component.SetTrigger("Pressed");
			}

			if (scaleBounceEffect)
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
		/// Handles pointer up events. Reverts the bounce animation.
		/// </summary>
		public override void OnPointerUp(PointerEventData eventData)
		{
			if (m_active)
				base.OnPointerUp(eventData);

			if (scaleBounceEffect)
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
		/// Overridden to only trigger the click if the button is in an active state.
		/// </summary>
		public override void OnPointerClick(PointerEventData eventData)
		{
			if (m_active)
				base.OnPointerClick(eventData);
		}

		/// <summary>
		/// Overridden to only trigger select events if the button is in an active state.
		/// </summary>
		public override void OnSelect(BaseEventData eventData)
		{
			if (m_active)
				base.OnSelect(eventData);
		}

		/// <summary>
		/// Retrieves or loads the shared Greyscale material from the Resources folder.
		/// </summary>
		/// <returns>The Greyscale material.</returns>
		public Material GetGreyMat()
		{
			if (m_GreyMat == null)
				m_GreyMat = Resources.Load<Material>("Greyscale");
			return m_GreyMat;
		}

		/// <summary>
		/// Sets whether the greyscale effect should be used for the disabled state.
		/// </summary>
		/// <param name="pValue">True to enable the greyscale effect.</param>
		public void EnableGrey(bool pValue)
		{
			greyscaleEffect = pValue;
		}

		/// <summary>
		/// Checks if the button is currently enabled and logically active.
		/// </summary>
		/// <returns>True if the button can be interacted with.</returns>
		public bool Enabled()
		{
			return enabled && m_active;
		}
		
		/// <summary>
		/// A helper method to set the active ('On') sprite.
		/// </summary>
		/// <param name="pSprite">The sprite to use for the active state.</param>
		public void SetActiveSprite(Sprite pSprite)
		{
			imgOn = pSprite;
		}

		/// <summary>
		/// Adjusts the 'pixelsPerUnitMultiplier' on a sliced image to make it fit perfectly
		/// within the RectTransform's bounds without stretching, based on the 'perfectRatio' setting.
		/// </summary>
		protected void CheckPerfectRatio()
		{
			var image1 = m_img;
			if (image1 == null || image1.sprite == null || image1.type != Image.Type.Sliced || m_perfectSpriteId == image1.sprite.GetInstanceID())
				return;

			if (perfectRatio == PerfectRatio.Width)
			{
				var nativeSize = image1.sprite.NativeSize();
				var rectSize = rectTransform.sizeDelta;
				if (rectSize.x > 0 && rectSize.x < nativeSize.x)
				{
					var ratio = nativeSize.x / rectSize.x;
					image1.pixelsPerUnitMultiplier = ratio;
				}
				else
					image1.pixelsPerUnitMultiplier = 1;
				m_perfectSpriteId = image1.sprite.GetInstanceID();
			}
			else if (perfectRatio == PerfectRatio.Height)
			{
				var nativeSize = image1.sprite.NativeSize();
				var rectSize = rectTransform.sizeDelta;
				if (rectSize.y > 0 && rectSize.y < nativeSize.y)
				{
					var ratio = nativeSize.y / rectSize.y;
					image1.pixelsPerUnitMultiplier = ratio;
				}
				else
					image1.pixelsPerUnitMultiplier = 1;
				m_perfectSpriteId = image1.sprite.GetInstanceID();
			}
		}

		/// <summary>
		/// Plays a continuous "breathing" or "bubbling" animation on the button for a specified duration.
		/// Useful for drawing attention to the button. Requires DOTween.
		/// </summary>
		/// <param name="duration">The total duration for the effect to play.</param>
		public void PlayBubbleEffect(float duration)
		{
#if DOTWEEN
			float scaleDuration = 0.6f;
			int loopCount = Mathf.Max(2, Mathf.RoundToInt(duration / scaleDuration));
			
			// Ensure an even loop count for a smooth return to the original scale
			if (loopCount % 2 != 0)
				loopCount++;

			DOTween.Kill(GetInstanceID());
			var bubbleSequence = DOTween.Sequence();
			bubbleSequence.Append(transform.DOScale(0.9f, scaleDuration * 0.4f));
			bubbleSequence.Append(transform.DOScale(1.1f, scaleDuration * 0.6f)).SetEase(Ease.OutSine);
			bubbleSequence.SetLoops(loopCount, LoopType.Yoyo);
			bubbleSequence.SetId(GetInstanceID());
			bubbleSequence.OnComplete(() => transform.localScale = m_initialScale);
#else
			UnityEngine.Debug.LogError("Bubble Effect Requires DOTween");
#endif
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

			// Auto-assign target graphic if not set
			if (targetGraphic == null)
			{
				var images = gameObject.GetComponentsInChildren<Image>();
				if (images.Length > 0)
				{
					targetGraphic = images[0];
					m_img = (Image)targetGraphic;
				}
			}
			if (targetGraphic != null && m_img == null)
				m_img = targetGraphic as Image;

			// Disable animator if using bounce effect, or configure animator if using animation transition
			if (scaleBounceEffect)
			{
				if (transition == Transition.Animation)
					transition = Transition.None;

				if (gameObject.TryGetComponent(out Animator component))
					component.enabled = false;
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
			
			// Reset and re-check ratio in editor
			m_perfectSpriteId = 0;
			CheckPerfectRatio();
		}
		
		/// <summary>
		/// Custom editor for the JustButton to provide a more organized Inspector.
		/// </summary>
		[CanEditMultipleObjects]
		[CustomEditor(typeof(JustButton), true)]
		public class JustButtonEditor : ButtonEditor
		{
			private JustButton m_target;

			protected override void OnEnable()
			{
				base.OnEnable();
				m_target = (JustButton)target;
			}

			public override void OnInspectorGUI()
			{
				// Show the default Button inspector first
				base.OnInspectorGUI();
				
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("JustButton Properties", EditorStyles.boldLabel);

				// Update perfect ratio live in the editor
				m_target.CheckPerfectRatio();
				EditorGUILayout.BeginVertical("box");
				{
					// Display custom fields
					serializedObject.SerializeField(nameof(m_img));
					serializedObject.SerializeField(nameof(scaleBounceEffect));
					serializedObject.SerializeField(nameof(greyscaleEffect));
					serializedObject.SerializeField(nameof(clickSfx));
					serializedObject.SerializeField(nameof(perfectRatio));
					
					// Show sprite fields only if sprite swap is enabled
					var imgSwapEnabled = serializedObject.SerializeField(nameof(imgOnOffSwap));
					if (imgSwapEnabled.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField(nameof(imgOn));
						serializedObject.SerializeField(nameof(imgOff));
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
				serializedObject.ApplyModifiedProperties();
			}

			/// <summary>
			/// A menu item to replace standard Unity Buttons with JustButtons on the selected GameObjects.
			/// It preserves the original button's properties like onClick events.
			/// </summary>
			[MenuItem("GameObject/RCore/UI/Replace Button By JustButton")]
			private static void ReplaceButton()
			{
				var gameObjects = Selection.gameObjects;
				for (int i = 0; i < gameObjects.Length; i++)
				{
					var buttons = gameObjects[i].GetComponentsInChildren<Button>(true);
					for (int j = 0; j < buttons.Length; j++)
					{
						var btn = buttons[j];
						// Ensure we don't replace a button that is already a JustButton
						if (btn is not JustButton)
						{
							var go = btn.gameObject;
							// Copy properties
							var onClick = btn.onClick;
							var enabled = btn.enabled;
							var interactable = btn.interactable;
							var transition = btn.transition;
							var targetGraphic = btn.targetGraphic;
							var colors = btn.colors;
							// Replace component
							DestroyImmediate(btn);
							var newBtn = go.AddComponent<JustButton>();
							// Paste properties
							newBtn.onClick = onClick;
							newBtn.enabled = enabled;
							newBtn.interactable = interactable;
							newBtn.transition = transition;
							newBtn.targetGraphic = targetGraphic;
							newBtn.colors = colors;
							EditorUtility.SetDirty(go);
						}
					}
				}
			}
		}

#endif
	}
}