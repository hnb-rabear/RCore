#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RCore.UI
{
	/// <summary>
	/// An extension of the JustButton class that includes a direct reference to a legacy Unity UI Text component.
	/// NOTE: This class is considered obsolete. It is recommended to use SimpleTMPButton for new development,
	/// as it supports TextMeshPro, which is the current standard for text in Unity.
	/// </summary>
	[Obsolete("Use SimpleTMPButton instead")]
	[AddComponentMenu("RCore/UI/SimpleButton")]
	public class SimpleButton : JustButton
	{
		/// <summary>
		/// A direct reference to the legacy UI Text component used as this button's label.
		/// </summary>
		[Tooltip("A direct reference to the legacy UI Text component used as this button's label.")]
		public Text label;
		
		/// <summary>
		/// A property to get the label Text component.
		/// If the 'label' field is not assigned, it will attempt to find the Text component in its children once.
		/// </summary>
		public Text Label
		{
			get
			{
				if (label == null && !m_findLabel)
				{
					label = GetComponentInChildren<Text>();
					m_findLabel = true;
				}
				return label;
			}
		}
		
		/// <summary>A flag to ensure the search for the label component only happens once.</summary>
		private bool m_findLabel;

#if UNITY_EDITOR
		/// <summary>
		/// (Editor-only) Automatically called when the script is loaded or a value is changed in the Inspector.
		/// Attempts to find and assign the child Text component to the 'label' field if it is not already set.
		/// </summary>
		[ContextMenu("Validate")]
		protected override void OnValidate()
		{
			base.OnValidate();

			if (label == null)
				label = GetComponentInChildren<Text>();
		}
		
		/// <summary>
		/// Custom editor for the SimpleButton to provide a more organized and user-friendly Inspector.
		/// </summary>
		[CanEditMultipleObjects]
		[CustomEditor(typeof(SimpleButton), true)]
		public class SimpleButtonEditor : JustButtonEditor
		{
			/// <summary>
			/// Overrides the default inspector GUI to add custom fields for the SimpleButton.
			/// </summary>
			public override void OnInspectorGUI()
			{
				// Draw the inspector for the base JustButton
				base.OnInspectorGUI();
				
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("SimpleButton Properties", EditorStyles.boldLabel);

				EditorGUILayout.BeginVertical("box");
				{
					// Draw the field for the label
					var label1 = serializedObject.SerializeField(nameof(label));
					var text = label1.objectReferenceValue as Text;

					// If a label is assigned, this attempts to show its text property directly in the inspector.
					// Note: This part has limited functionality in modern Unity versions but shows the intent.
					if (text != null)
					{
						var textObj = new SerializedObject(text);
						textObj.SerializeField("m_Text");

						if (GUI.changed)
							textObj.ApplyModifiedProperties();
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();

				if (GUI.changed)
					serializedObject.ApplyModifiedProperties();
			}
			
			/// <summary>
			/// A menu item to replace standard Unity Buttons with SimpleButtons on the selected GameObjects.
			/// It preserves the original button's properties like onClick events, interactable state, etc.
			/// </summary>
			[MenuItem("GameObject/RCore/UI/Replace Button By SimpleButton")]
			private static void ReplaceButton()
			{
				var gameObjects = Selection.gameObjects;
				for (int i = 0; i < gameObjects.Length; i++)
				{
					var buttons = gameObjects[i].GetComponentsInChildren<Button>(true);
					for (int j = 0; j < buttons.Length; j++)
					{
						var btn = buttons[j];
						// Ensure we don't replace a button that is already a SimpleButton
						if (btn is not SimpleButton)
						{
							var go = btn.gameObject;
							// Copy properties from the old button
							var onClick = btn.onClick;
							var enabled = btn.enabled;
							var interactable = btn.interactable;
							var transition = btn.transition;
							var targetGraphic = btn.targetGraphic;
							var colors = btn.colors;
							
							// Replace the component
							DestroyImmediate(btn);
							var newBtn = go.AddComponent<SimpleButton>();
							
							// Paste the properties to the new button
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