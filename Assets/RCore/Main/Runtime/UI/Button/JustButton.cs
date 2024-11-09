/***
 * Author RaBear - HNB - 2018
 **/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Serialization;
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

		private static Material m_GreyMat;

		[FormerlySerializedAs("mPivotForFX")]
		[SerializeField] protected PivotForScale m_pivotForScaleBounce = PivotForScale.Center;
		[FormerlySerializedAs("mEnabledFX")]
		[SerializeField] protected bool m_scaleBounceEffect = true;
		[FormerlySerializedAs("mImg")]
		[SerializeField] protected Image m_img;
		[FormerlySerializedAs("mInitialScale")]
		[SerializeField] protected Vector2 m_initialScale = Vector2.one;
		[FormerlySerializedAs("m_PerfectRatio")]
		[SerializeField] protected PerfectRatio m_perfectRatio = PerfectRatio.Height;

		[FormerlySerializedAs("mGreyMatEnabled")]
		[SerializeField] protected bool m_greyscaleEffect;
		[FormerlySerializedAs("mImgSwapEnabled")]
		[SerializeField] protected bool m_imgOnOffSwap;
		[FormerlySerializedAs("mImgActive")]
		[SerializeField] protected Sprite m_imgOn;
		[FormerlySerializedAs("mImgInactive")]
		[SerializeField] protected Sprite m_imgOff;
		[FormerlySerializedAs("m_SfxClip")]
		[FormerlySerializedAs("m_SFXClip")]
		[SerializeField] protected string m_clickSfx;

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

		private PivotForScale m_prePivot;
		private Action m_inactionStateAction;
		private bool m_active = true;
		private int m_PerfectSpriteId;

		public virtual void SetEnable(bool pValue)
		{
			m_active = pValue;
			enabled = pValue || m_inactionStateAction != null;
			if (pValue)
			{
				if (m_imgOnOffSwap)
					m_img.sprite = m_imgOn;
				else
					imgMaterial = null;
			}
			else
			{
				if (m_imgOnOffSwap)
				{
					m_img.sprite = m_imgOff;
				}
				else
				{
					transform.localScale = m_initialScale;

					//Use grey material here
					if (m_greyscaleEffect)
						imgMaterial = GetGreyMat();
				}
			}
		}

		public virtual void SetInactiveStateAction(Action pAction)
		{
			m_inactionStateAction = pAction;
			enabled = m_active || m_inactionStateAction != null;
		}

		protected override void Start()
		{
			base.Start();

			m_prePivot = m_pivotForScaleBounce;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (m_scaleBounceEffect)
				transform.localScale = m_initialScale;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_scaleBounceEffect)
				transform.localScale = m_initialScale;

			if (m_img != null && m_img.sprite != null && m_PerfectSpriteId != m_img.sprite.GetInstanceID())
				CheckPerfectRatio();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (!m_active)
			{
				if (m_inactionStateAction != null)
				{
					m_inactionStateAction();
					if (TryGetComponent(out Animator component))
						component.SetTrigger("Pressed");
				}
			}

			if (m_active)
			{
				base.OnPointerDown(eventData);
				if (!string.IsNullOrEmpty(m_clickSfx))
					EventDispatcher.Raise(new Audio.SFXTriggeredEvent(m_clickSfx));
			}

			if (m_scaleBounceEffect)
			{
				if (m_pivotForScaleBounce != m_prePivot)
				{
					m_prePivot = m_pivotForScaleBounce;
					RefreshPivot(rectTransform);
				}

				transform.localScale = m_initialScale * 0.95f;
			}
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			if (m_active)
				base.OnPointerUp(eventData);

			if (m_scaleBounceEffect)
			{
				transform.localScale = m_initialScale;
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

		public void RefreshPivot()
		{
			RefreshPivot(rectTransform);
		}

		private void RefreshPivot(RectTransform pRect)
		{
			switch (m_pivotForScaleBounce)
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
				m_GreyMat = Resources.Load<Material>("Greyscale");
			return m_GreyMat;
		}

		public void EnableGrey(bool pValue)
		{
			m_greyscaleEffect = pValue;
		}

		public bool Enabled()
		{
			return enabled && m_active;
		}

		public void SetActiveSprite(Sprite pSprite)
		{
			m_imgOn = pSprite;
		}

		protected void CheckPerfectRatio()
		{
			if (m_perfectRatio == PerfectRatio.Width)
			{
				var image1 = m_img;
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
			else if (m_perfectRatio == PerfectRatio.Height)
			{
				var image1 = m_img;
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

			if (transition == Transition.Animation)
			{
				var _animator = gameObject.GetOrAddComponent<Animator>();
				_animator.updateMode = AnimatorUpdateMode.UnscaledTime;
				m_scaleBounceEffect = false;
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
			}

			if (m_scaleBounceEffect)
				m_initialScale = transform.localScale;

			RefreshPivot();

			m_PerfectSpriteId = 0;
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
					EditorHelper.SerializeField(serializedObject, "m_img");
					EditorHelper.SerializeField(serializedObject, "m_pivotForScaleBounce");
					EditorHelper.SerializeField(serializedObject, "m_scaleBounceEffect");
					EditorHelper.SerializeField(serializedObject, "m_greyscaleEffect");
					EditorHelper.SerializeField(serializedObject, "m_clickSfx");
					EditorHelper.SerializeField(serializedObject, "m_perfectRatio");
					var imgSwapEnabled = EditorHelper.SerializeField(serializedObject, "m_imgOnOffSwap");
					if (imgSwapEnabled.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						EditorHelper.SerializeField(serializedObject, "m_imgOn");
						EditorHelper.SerializeField(serializedObject, "m_imgOff");
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