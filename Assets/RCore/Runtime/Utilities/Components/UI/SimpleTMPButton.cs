/***
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RCore.Common;
using System;
using System.Linq;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
	[AddComponentMenu("RCore/UI/SimpleTMPButton")]
	public class SimpleTMPButton : JustButton
	{
		[SerializeField] protected TextMeshProUGUI mLabelTMP;

		public TextMeshProUGUI labelTMP
		{
			get
			{
				if (mLabelTMP == null && !mFindLabel)
				{
					mLabelTMP = GetComponentInChildren<TextMeshProUGUI>();
					mFindLabel = true;
				}
				return mLabelTMP;
			}
		}

		private bool mFindLabel;

		[SerializeField] protected bool mFontColorSwap;
		[SerializeField] protected Color mFontColorActive;
		[SerializeField] protected Color mFontColorInactive;

		[SerializeField] protected bool m_LabelMatSwap;
		[SerializeField] private Material m_LabelMatActive;
		[SerializeField] private Material m_LabelMatInactive;

#if UNITY_EDITOR
		[ContextMenu("Validate")]
		protected override void OnValidate()
		{
			base.OnValidate();

			if (mLabelTMP == null)
				mLabelTMP = GetComponentInChildren<TextMeshProUGUI>();
			if (mLabelTMP == null)
				m_LabelMatSwap = false;
			if (!m_LabelMatSwap)
			{
				m_LabelMatActive = null;
				m_LabelMatInactive = null;
			}
			else if (m_LabelMatActive == null)
			{
				m_LabelMatActive = mLabelTMP.fontSharedMaterial;
			}
		}
#endif
		
		public override void SetEnable(bool pValue)
		{
			base.SetEnable(pValue);

			if (pValue)
			{
				if (mFontColorSwap)
					mLabelTMP.color = mFontColorActive;
				if (m_LabelMatSwap && m_LabelMatActive != null && m_LabelMatInactive != null)
				{
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var label in labels)
					{
						if (label.font == mLabelTMP.font)
							label.fontSharedMaterial = m_LabelMatActive;
					}
				}
			}
			else
			{
				if (mFontColorSwap)
					mLabelTMP.color = mFontColorInactive;
				if (m_LabelMatSwap && m_LabelMatActive != null && m_LabelMatInactive != null)
				{
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var label in labels)
					{
						if (label.font == mLabelTMP.font)
							label.fontSharedMaterial = m_LabelMatInactive;
					}
				}
			}
		}

#if UNITY_EDITOR
		[CanEditMultipleObjects]
		[CustomEditor(typeof(SimpleTMPButton), true)]
		private class SimpleTMPButtonEditor : JustButtonEditor
		{
			private SimpleTMPButton mButton;
			private string[] m_MatsName;
			private Material[] m_LabelMats;

			protected override void OnEnable()
			{
				base.OnEnable();

				mButton = (SimpleTMPButton)target;
				m_MatsName = Array.Empty<string>();
				m_LabelMats = Array.Empty<Material>();
				if (mButton.labelTMP != null)
				{
					m_LabelMats = TMPro.EditorUtilities.TMP_EditorUtility.FindMaterialReferences(mButton.labelTMP.font);
					m_MatsName = new string[m_LabelMats.Length];
					for (int i = 0; i < m_LabelMats.Length; i++)
						m_MatsName[i] = m_LabelMats[i].name;
				}
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.BeginVertical("box");
				{
					var fontColorSwap = EditorHelper.SerializeField(serializedObject, "mFontColorSwap");
					if (fontColorSwap.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						EditorHelper.SerializeField(serializedObject, "mFontColorActive");
						EditorHelper.SerializeField(serializedObject, "mFontColorInactive");
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}

					var labelMatSwap = EditorHelper.SerializeField(serializedObject, "m_LabelMatSwap");
					if (labelMatSwap.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						var labelMatActiveName = EditorHelper.DropdownList(mButton.m_LabelMatActive ? mButton.m_LabelMatActive.name : "", "Active Mat", m_MatsName.ToArray());
						mButton.m_LabelMatActive = m_MatsName.Contains(labelMatActiveName) ? m_LabelMats[m_MatsName.IndexOf(labelMatActiveName)] : null;
						var labelMatInactiveName = EditorHelper.DropdownList(mButton.m_LabelMatInactive? mButton.m_LabelMatInactive.name : "", "Inactive Mat", m_MatsName.ToArray());
						mButton.m_LabelMatInactive = m_MatsName.Contains(labelMatInactiveName) ? m_LabelMats[m_MatsName.IndexOf(labelMatInactiveName)] : null;
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}
					
					var label = EditorHelper.SerializeField(serializedObject, "mLabelTMP");
					var text = label.objectReferenceValue as TextMeshProUGUI;
					if (text != null)
					{
						var textObj = new SerializedObject(text);
						EditorHelper.SerializeField(textObj, "m_text");

						if (GUI.changed)
							textObj.ApplyModifiedProperties();
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();

				if (GUI.changed)
					serializedObject.ApplyModifiedProperties();
			}

			[MenuItem("RCore/UI/Replace Button By SimpleTMPButton")]
			private static void ReplaceButton()
			{
				var gameObjects = Selection.gameObjects;
				for (int i = 0; i < gameObjects.Length; i++)
				{
					var buttons = gameObjects[i].FindComponentsInChildren<Button>();
					for (int j = 0; j < buttons.Count; j++)
					{
						var btn = buttons[j];
						if (btn is not SimpleTMPButton)
						{
							var obj = btn.gameObject;
							DestroyImmediate(btn);
							obj.AddComponent<SimpleTMPButton>();
						}
					}
				}
			}
		}
#endif
	}
}