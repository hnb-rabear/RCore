#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RCore.UI
{
	[AddComponentMenu("RCore/UI/SimpleButton")]
	public class SimpleButton : JustButton
	{
		[FormerlySerializedAs("m_label")]
		public Text label;
		
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
		
		private bool m_findLabel;

#if UNITY_EDITOR
		[ContextMenu("Validate")]
		protected override void OnValidate()
		{
			base.OnValidate();

			if (label == null)
				label = GetComponentInChildren<Text>();
		}

		[CanEditMultipleObjects]
		[CustomEditor(typeof(SimpleButton), true)]
		public class SimpleButtonEditor : JustButtonEditor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.BeginVertical("box");
				{
					var label = serializedObject.SerializeField("m_label");
					var text = label.objectReferenceValue as Text;
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
						if (btn is not SimpleButton)
						{
							var go = btn.gameObject;
							var onClick = btn.onClick;
							var enabled = btn.enabled;
							var interactable = btn.interactable;
							var transition = btn.transition;
							var targetGraphic = btn.targetGraphic;
							var colors = btn.colors;
							DestroyImmediate(btn);
							var newBtn = go.AddComponent<SimpleButton>();
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