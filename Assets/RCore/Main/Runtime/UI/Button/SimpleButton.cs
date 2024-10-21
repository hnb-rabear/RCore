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
		[FormerlySerializedAs("mLabel")]
		[SerializeField] protected Text m_label;
		public Text Label
		{
			get
			{
				if (m_label == null && !m_findLabel)
				{
					m_label = GetComponentInChildren<Text>();
					m_findLabel = true;
				}
				return m_label;
			}
		}
		private bool m_findLabel;

#if UNITY_EDITOR
		[ContextMenu("Validate")]
		protected override void OnValidate()
		{
			base.OnValidate();

			if (m_label == null)
				m_label = GetComponentInChildren<Text>();
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
					var label = EditorHelper.SerializeField(serializedObject, "m_label");
					var text = label.objectReferenceValue as Text;
					if (text != null)
					{
						var textObj = new SerializedObject(text);
						EditorHelper.SerializeField(textObj, "m_Text");

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