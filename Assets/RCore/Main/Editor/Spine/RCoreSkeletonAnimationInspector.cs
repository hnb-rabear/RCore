#if UNITY_EDITOR
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;

namespace RCore.Editor
{
	/// <summary>
	/// Extends Spine's own SkeletonAnimation inspector with the Edit Mode animation preview.
	/// Same subclassing reason as <see cref="RCoreSkeletonGraphicInspector"/>.
	/// </summary>
	[CustomEditor(typeof(SkeletonAnimation))]
	[CanEditMultipleObjects]
	public class RCoreSkeletonAnimationInspector : SkeletonAnimationInspector
	{
		private SpineEditorPreview m_preview;

		private void OnDisable()
		{
			m_preview?.OnInspectorDisabled();
			m_preview = null;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (targets != null && targets.Length > 1) return;
			if (!(target is SkeletonAnimation skeletonAnimation)) return;
			if (skeletonAnimation.skeletonDataAsset == null) return;

			if (m_preview == null || m_preview.Target != skeletonAnimation)
				m_preview = new SpineEditorPreview(skeletonAnimation, Repaint);

			m_preview.DrawGUI();
		}
	}
}
#endif
