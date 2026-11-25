#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
	// First and last children sit flush at the container's left/right edges (minus padding).
	// Remaining children are spread with equal edge-to-edge gaps between them.
	// Middle Bias shifts the middle children: -1 packs them against the first child, 1 against the last,
	// using Spacing as the packed gap.
	[ExecuteAlways]
	[AddComponentMenu("RCore/UI/Edge Aligned Horizontal Layout Group")]
	public class EdgeAlignedHorizontalLayoutGroup : HorizontalLayoutGroup
	{
		[SerializeField, Range(-1f, 1f)] private float m_middleBias;

		protected override void OnEnable()
		{
			base.OnEnable();
			ForceRebuildInEditor();
		}

		protected override void OnTransformChildrenChanged()
		{
			base.OnTransformChildrenChanged();
			ForceRebuildInEditor();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();
			ForceRebuildInEditor();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			ForceRebuildInEditor();
		}
#endif

		// LayoutGroup.SetDirty() reschedules via StartCoroutine when a rebuild is already in
		// progress, which never runs outside Play mode — so Edit-mode changes can silently
		// fail to refresh. Force an immediate synchronous rebuild instead.
		private void ForceRebuildInEditor()
		{
			if (Application.isPlaying || rectTransform == null)
				return;

			LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
		}

		public override void SetLayoutHorizontal()
		{
			int count = rectChildren.Count;
			if (count == 0)
				return;

			float width = rectTransform.rect.width - padding.horizontal;

			if (count == 1)
			{
				RectTransform only = rectChildren[0];
				if (childControlWidth)
					SetChildAlongAxis(only, 0, padding.left, LayoutUtility.GetPreferredSize(only, 0));
				else
					SetChildAlongAxis(only, 0, padding.left);
				return;
			}

			// Only drive size when childControlWidth is on — the 4-arg overload marks
			// SizeDelta as driven, which locks out manual/LayoutElement width edits.
			float[] childWidths = new float[count];
			float totalChildWidth = 0f;
			for (int i = 0; i < count; i++)
			{
				RectTransform child = rectChildren[i];
				childWidths[i] = childControlWidth ? LayoutUtility.GetPreferredSize(child, 0) : child.rect.width;
				totalChildWidth += childWidths[i];
			}

			float gap = (width - totalChildWidth) / (count - 1);
			// Never pack wider than the equal gap, so packed children cannot overlap the edge child.
			float packedGap = Mathf.Min(spacing, gap);

			float x = padding.left;
			float widthBefore = 0f;
			for (int i = 0; i < count; i++)
			{
				RectTransform child = rectChildren[i];
				float pos = x;
				if (i > 0 && i < count - 1 && m_middleBias != 0f)
				{
					float packedPos = m_middleBias < 0f
						? padding.left + widthBefore + i * packedGap
						: padding.left + width - (totalChildWidth - widthBefore) - (count - 1 - i) * packedGap;
					pos = Mathf.Lerp(x, packedPos, Mathf.Abs(m_middleBias));
				}

				if (childControlWidth)
					SetChildAlongAxis(child, 0, pos, childWidths[i]);
				else
					SetChildAlongAxis(child, 0, pos);
				x += childWidths[i] + gap;
				widthBefore += childWidths[i];
			}
		}

#if UNITY_EDITOR
		// Unity's HorizontalLayoutGroup editor only draws its own fields, so draw Middle Bias here.
		[CustomEditor(typeof(EdgeAlignedHorizontalLayoutGroup)), CanEditMultipleObjects]
		public class EdgeAlignedHorizontalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
		{
			private SerializedProperty m_middleBiasProperty;

			protected override void OnEnable()
			{
				base.OnEnable();
				m_middleBiasProperty = serializedObject.FindProperty(nameof(m_middleBias));
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				serializedObject.Update();
				EditorGUILayout.PropertyField(m_middleBiasProperty);
				serializedObject.ApplyModifiedProperties();
			}
		}
#endif
	}
}
