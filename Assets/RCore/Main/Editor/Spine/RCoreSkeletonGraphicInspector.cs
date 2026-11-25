#if UNITY_EDITOR
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;

namespace RCore.Editor
{
	/// <summary>
	/// Extends Spine's own SkeletonGraphic inspector with the Edit Mode animation preview, so the panel
	/// sits inside the component instead of floating above the whole GameObject.
	///
	/// Subclassing keeps the preview out of the Spine package (a Spine upgrade cannot wipe it) while still
	/// drawing in the right place. Spine cannot reference back into RCore — that would be circular — so
	/// inheriting its editor is the only way in.
	/// </summary>
	[CustomEditor(typeof(SkeletonGraphic))]
	[CanEditMultipleObjects]
	public class RCoreSkeletonGraphicInspector : SkeletonGraphicInspector
	{
		private SpineEditorPreview m_preview;

		// No OnEnable here on purpose. The base declares a private, non-virtual OnEnable that wires up every
		// SerializedProperty it draws; declaring one here would hide it and leave those fields null.

		private void OnDisable()
		{
			m_preview?.OnInspectorDisabled();
			m_preview = null;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (targets != null && targets.Length > 1) return;
			if (!(target is SkeletonGraphic skeletonGraphic)) return;
			// The base already draws its own "assign a SkeletonDataAsset" notice and returns early.
			if (skeletonGraphic.skeletonDataAsset == null) return;

			if (m_preview == null || m_preview.Target != skeletonGraphic)
				m_preview = new SpineEditorPreview(skeletonGraphic, Repaint);

			m_preview.DrawGUI();
		}
	}
}
#endif
