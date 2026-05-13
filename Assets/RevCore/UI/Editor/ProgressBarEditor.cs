using UnityEditor;
using UnityEngine;

namespace RevCore.UI.Editor
{
	[CustomEditor(typeof(ProgressBar))]
	public class ProgressBarEditor : UnityEditor.Editor
	{
		private ProgressBar m_bar;

		private void OnEnable()
		{
			m_bar = target as ProgressBar;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (m_bar.txtValue != null)
				m_bar.txtValue.text = EditorGUILayout.TextField("Progress", m_bar.txtValue.text);
			if (m_bar.txtRank != null)
				m_bar.txtRank.text = EditorGUILayout.TextField("Rank", m_bar.txtRank.text);

			if (!m_bar.fillByBarSize || m_bar.imgBackground == null || m_bar.imgProgressValue == null)
				return;

			var barTransform = m_bar.imgProgressValue.transform as RectTransform;
			var pivot = m_bar.fillDirection switch
			{
				ProgressBar.FillDirection.Left => new Vector2(0, 0.5f),
				ProgressBar.FillDirection.Right => new Vector2(1, 0.5f),
				ProgressBar.FillDirection.Bottom => new Vector2(0.5f, 0),
				ProgressBar.FillDirection.Top => new Vector2(0.5f, 1),
				_ => new Vector2(0, 0.5f)
			};

			if (barTransform.pivot == pivot)
				return;

			var size = barTransform.rect.size;
			var deltaPivot = barTransform.pivot - pivot;
			var deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
			barTransform.pivot = pivot;
			barTransform.localPosition -= deltaPosition;
		}
	}
}
