/***
 * Author RaBear - HNB - 2018
 **/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
using UnityEditor.UI;
#endif

namespace RCore.UI
{
	[AddComponentMenu("RCore/UI/JustButton")]
	public class JustButton : Button
	{
		private static Material m_GreyMat;

		[SerializeField] protected Image m_img;

		[FormerlySerializedAs("m_scaleBounceEffect")]
		[FormerlySerializedAs("mEnabledFX")]
		public bool scaleBounceEffect = true;
		[FormerlySerializedAs("mImg")]
		[FormerlySerializedAs("m_perfectRatio")]
		[FormerlySerializedAs("m_PerfectRatio")]
		public PerfectRatio perfectRatio = PerfectRatio.Height;
		[FormerlySerializedAs("m_greyscaleEffect")]
		[FormerlySerializedAs("mGreyMatEnabled")]
		public bool greyscaleEffect;
		[FormerlySerializedAs("m_imgOnOffSwap")]
		[FormerlySerializedAs("mImgSwapEnabled")]
		public bool imgOnOffSwap;
		[FormerlySerializedAs("m_imgOn")]
		[FormerlySerializedAs("mImgActive")]
		public Sprite imgOn;
		[FormerlySerializedAs("m_imgOff")]
		[FormerlySerializedAs("mImgInactive")]
		public Sprite imgOff;
		public TapFeedback tapFeedback = TapFeedback.Haptic;
		[FormerlySerializedAs("m_clickSfx")]
		[FormerlySerializedAs("m_SfxClip")]
		public string clickSfx = "button";
		public Image img
		{
			get
			{
				if (m_img == null)
					m_img = targetGraphic as Image;
				return m_img;
			}
		}
		public Material imgMaterial { get => img.material; set => img.material = value; }
		public RectTransform rectTransform => targetGraphic.rectTransform;

		private Action m_inactionStateAction;
		private bool m_active = true;
		private int m_perfectSpriteId;
		private Vector2 m_initialScale;

		protected override void Awake()
		{
			base.Awake();

			m_initialScale = transform.localScale;
		}

		public virtual void SetEnable(bool pValue)
		{
			m_active = pValue;
			enabled = pValue || m_inactionStateAction != null;
			if (pValue)
			{
				if (imgOnOffSwap)
					m_img.sprite = imgOn;
				else
					imgMaterial = null;
			}
			else
			{
				if (imgOnOffSwap)
				{
					m_img.sprite = imgOff;
				}
				else
				{
					transform.localScale = m_initialScale;

					//Use grey material here
					if (greyscaleEffect)
						imgMaterial = GetGreyMat();
				}
			}
		}

		public virtual void SetInactiveStateAction(Action pAction)
		{
			m_inactionStateAction = pAction;
			enabled = m_active || m_inactionStateAction != null;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (scaleBounceEffect)
				transform.localScale = m_initialScale;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (scaleBounceEffect)
				transform.localScale = m_initialScale;

			if (m_img != null && m_img.sprite != null && m_perfectSpriteId != m_img.sprite.GetInstanceID())
				CheckPerfectRatio();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (!m_active)
			{
				if (m_inactionStateAction != null)
				{
					m_inactionStateAction();
					if (TryGetComponent(out Animator component) && component.enabled)
						component.SetTrigger("Pressed");
				}
			}

			if (m_active)
			{
				base.OnPointerDown(eventData);
				
				if (tapFeedback == TapFeedback.Haptic || tapFeedback == TapFeedback.SoundAndHaptic)
					Vibration.VibratePop();
				
				if ((tapFeedback == TapFeedback.Sound || tapFeedback == TapFeedback.SoundAndHaptic) && !string.IsNullOrEmpty(clickSfx))
					EventDispatcher.Raise(new Audio.SFXTriggeredEvent(clickSfx));
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

		public override void OnPointerClick(PointerEventData eventData)
		{
			if (m_active)
				base.OnPointerClick(eventData);
		}

		public override void OnSelect(BaseEventData eventData)
		{
			if (m_active)
				base.OnSelect(eventData);
		}

		public Material GetGreyMat()
		{
			if (m_GreyMat == null)
				m_GreyMat = Resources.Load<Material>("Greyscale");
			return m_GreyMat;
		}

		public void EnableGrey(bool pValue)
		{
			greyscaleEffect = pValue;
		}

		public bool Enabled()
		{
			return enabled && m_active;
		}

		public void SetActiveSprite(Sprite pSprite)
		{
			imgOn = pSprite;
		}

		protected void CheckPerfectRatio()
		{
			if (perfectRatio == PerfectRatio.Width)
			{
				var image1 = m_img;
				if (image1 != null && image1.sprite != null && image1.type == Image.Type.Sliced && m_perfectSpriteId != image1.sprite.GetInstanceID())
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
					m_perfectSpriteId = image1.sprite.GetInstanceID();
				}
			}
			else if (perfectRatio == PerfectRatio.Height)
			{
				var image1 = m_img;
				if (image1 != null && image1.sprite != null && image1.type == Image.Type.Sliced && m_perfectSpriteId != image1.sprite.GetInstanceID())
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
					m_perfectSpriteId = image1.sprite.GetInstanceID();
				}
			}
		}

		public void PlayBubbleEffect(float duration)
		{
#if DOTWEEN
			float scaleDuration = 0.6f;
			int loopCount = Mathf.Max(2, Mathf.RoundToInt(duration / scaleDuration));

			// Ensure loop count is even
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
					m_img = (Image)targetGraphic;
				}
			}
			if (targetGraphic != null && m_img == null)
				m_img = targetGraphic as Image;

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

			m_perfectSpriteId = 0;
			CheckPerfectRatio();
		}

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
				base.OnInspectorGUI();

				m_target.CheckPerfectRatio();
				EditorGUILayout.BeginVertical("box");
				{
					serializedObject.SerializeField("m_img");
					serializedObject.SerializeField("scaleBounceEffect");
					serializedObject.SerializeField("greyscaleEffect");
					serializedObject.SerializeField("clickSfx");
					serializedObject.SerializeField("tapFeedback");
					serializedObject.SerializeField("perfectRatio");
					var imgSwapEnabled = serializedObject.SerializeField("imgOnOffSwap");
					if (imgSwapEnabled.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField("imgOn");
						serializedObject.SerializeField("imgOff");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
				serializedObject.ApplyModifiedProperties();
			}

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
						if (btn is not JustButton)
						{
							var go = btn.gameObject;
							var onClick = btn.onClick;
							var enabled = btn.enabled;
							var interactable = btn.interactable;
							var transition = btn.transition;
							var targetGraphic = btn.targetGraphic;
							var colors = btn.colors;
							DestroyImmediate(btn);
							var newBtn = go.AddComponent<JustButton>();
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