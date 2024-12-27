/***
 * Author HNB-RaBear - 2019
 **/

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using RCore.Editor;
#endif

namespace RCore.UI
{
	[Obsolete("Use CustomToggleTab instead")]
	[AddComponentMenu("RCore/UI/CustomToggleSlider")]
	public class CustomToggleSlider : Toggle
	{
		public TextMeshProUGUI txtLabel;

		[Tooltip("Marker which move to On/Off position")]
		public RectTransform toggleTransform;
		[Tooltip("Position that marker move to when toggle is on")]
		public Vector2 onPosition;
		[Tooltip("Position that marker move to when toggle is off")]
		public Vector2 offPosition;

		public bool enableOnOffContent;
		[Tooltip("Objects which active when toggle is on")]
		public GameObject[] onObjects;
		[Tooltip("Objects which active when toggle is off")]
		public GameObject[] offObjects;

		public bool enableOnOffColor;
		public Color onColor;
		public Color offColor;

		public string sfxClip = "button";
		public string sfxClipOff = "button";

		private bool m_clicked;

		protected override void OnEnable()
		{
			base.OnEnable();

			Refresh();
			onValueChanged.AddListener(OnValueChanged);
		}

		protected override void OnDisable()
		{
			onValueChanged.RemoveListener(OnValueChanged);
		}

		private void OnValueChanged(bool pIsOn)
		{
			if (m_clicked)
			{
				if (isOn && !string.IsNullOrEmpty(sfxClip))
					EventDispatcher.Raise(new Audio.UISfxTriggeredEvent(sfxClip));
				else if (!isOn && !string.IsNullOrEmpty(sfxClipOff))
					EventDispatcher.Raise(new Audio.UISfxTriggeredEvent(sfxClipOff));
				m_clicked = false;
			}
			Refresh();
		}

		private void Refresh()
		{
			if (enableOnOffContent)
			{
				if (onObjects != null)
					foreach (var onObject in onObjects)
						onObject.SetActive(isOn);
				if (offObjects != null)
					foreach (var offObject in offObjects)
						offObject.SetActive(!isOn);
			}
			if (toggleTransform != null)
				toggleTransform.anchoredPosition = isOn ? onPosition : offPosition;
			if (enableOnOffColor)
			{
				var targetImg = toggleTransform.GetComponent<Image>();
				if (targetImg != null)
					targetImg.color = isOn ? onColor : offColor;
			}
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			m_clicked = true;
			base.OnPointerClick(eventData);
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			if (txtLabel == null)
				txtLabel = gameObject.GetComponentInChildren<TextMeshProUGUI>();

			if (graphic != null)
				graphic.gameObject.SetActive(isOn);

			if (toggleTransform != null)
				toggleTransform.anchoredPosition = isOn ? onPosition : offPosition;

			if (enableOnOffContent)
			{
				if (onObjects != null)
					foreach (var onObject in onObjects)
						onObject.SetActive(isOn);
				if (offObjects != null)
					foreach (var offObject in offObjects)
						offObject.SetActive(!isOn);
			}

			if (targetGraphic == null)
			{
				var images = GetComponentsInChildren<Image>();
				if (images.Length > 0)
					targetGraphic = images[0];
			}

			if (enableOnOffColor)
			{
				var targetImg = toggleTransform.GetComponent<Image>();
				if (targetImg != null)
					targetImg.color = isOn ? onColor : offColor;
			}
		}
#endif

#if UNITY_EDITOR
		[UnityEditor.CustomEditor(typeof(CustomToggleSlider), true)]
		public class CustomToggleEditor : UnityEditor.UI.ToggleEditor
		{
			private CustomToggleSlider m_toggle;

			protected override void OnEnable()
			{
				base.OnEnable();

				m_toggle = (CustomToggleSlider)target;
			}

			public override void OnInspectorGUI()
			{
				UnityEditor.EditorGUILayout.BeginVertical("box");
				{
					serializedObject.SerializeField(nameof(txtLabel));
					serializedObject.SerializeField(nameof(toggleTransform));
					serializedObject.SerializeField(nameof(onPosition));
					serializedObject.SerializeField(nameof(offPosition));
					serializedObject.SerializeField(nameof(sfxClip));
					serializedObject.SerializeField(nameof(sfxClipOff));

					var property1 = serializedObject.SerializeField(nameof(enableOnOffContent));
					if (property1.boolValue)
					{
						UnityEditor.EditorGUI.indentLevel++;
						UnityEditor.EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField(nameof(onObjects));
						serializedObject.SerializeField(nameof(offObjects));
						UnityEditor.EditorGUILayout.EndVertical();
						UnityEditor.EditorGUI.indentLevel--;
					}
					var property2 = serializedObject.SerializeField(nameof(enableOnOffColor));
					if (property2.boolValue)
					{
						UnityEditor.EditorGUI.indentLevel++;
						UnityEditor.EditorGUILayout.BeginVertical("box");
						serializedObject.SerializeField(nameof(onColor));
						serializedObject.SerializeField(nameof(offColor));
						UnityEditor.EditorGUILayout.EndVertical();
						UnityEditor.EditorGUI.indentLevel--;
					}

					if (m_toggle.txtLabel != null)
						m_toggle.txtLabel.text = UnityEditor.EditorGUILayout.TextField("Label", m_toggle.txtLabel.text);

					serializedObject.ApplyModifiedProperties();
				}
				UnityEditor.EditorGUILayout.EndVertical();

				base.OnInspectorGUI();
			}
		}
#endif
	}
}