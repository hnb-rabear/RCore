using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
	// First and last children sit flush at the container's left/right edges (minus padding).
	// Remaining children are spread with equal edge-to-edge gaps between them.
	[ExecuteAlways]
	[AddComponentMenu("RCore/UI/Edge Aligned Horizontal Layout Group")]
	public class EdgeAlignedHorizontalLayoutGroup : HorizontalLayoutGroup
	{
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

			float x = padding.left;
			for (int i = 0; i < count; i++)
			{
				RectTransform child = rectChildren[i];
				if (childControlWidth)
					SetChildAlongAxis(child, 0, x, childWidths[i]);
				else
					SetChildAlongAxis(child, 0, x);
				x += childWidths[i] + gap;
			}
		}
	}
}
