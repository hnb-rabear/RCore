using UnityEngine;

namespace RCore.UI
{
	/// <summary>
	/// Arranges child RectTransforms on a circular arc with first item at top.
	/// Other items fan out symmetrically to both sides. Scale changes from maxScale at top to minScale at bottom.
	/// </summary>
	public class UICircleRewardArranger : UICircleArrangerBase
	{
		[Header("Reward Arc Settings")]
		[Range(180, 360)] public float maxDegree = 360f;

		[Header("Scale by Position")]
		[Range(0, 1)] public float minScale = 0.6f;
		[Range(0, 1.5f)] public float maxScale = 1f;

		protected override void CalculatePositions()
		{
			if (m_targets == null)
				return;

			int count = m_targets.Count;
			if (count == 0)
				return;

			if (m_newPositions == null || m_newPositions.Length != count)
				m_newPositions = new Vector3[count];
			if (m_newRotations == null || m_newRotations.Length != count)
				m_newRotations = new Quaternion[count];
			if (m_targetScales == null || m_targetScales.Length != count)
				m_targetScales = new float[count];

			float angleStep = count > 1 ? maxDegree / count : 0f;
			float currentRadius = GetRadius(count, angleStep);
			Vector2 center = GetCenterPosition();
			for (int i = 0; i < count; i++)
			{
				float angle;
				if (i == 0)
				{
					angle = 90f;
				}
				else
				{
					int side = (i + 1) / 2;
					float offset = angleStep * side;
					angle = i % 2 == 1 ? 90f + offset : 90f - offset;
				}

				float rad = angle * Mathf.Deg2Rad;
				m_newPositions[i] = new Vector2(Mathf.Cos(rad) * currentRadius, Mathf.Sin(rad) * currentRadius) + center;
				m_newRotations[i] = Quaternion.identity;
				m_targetScales[i] = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(rad) + 1f) / 2f);
			}
		}
	}
}
