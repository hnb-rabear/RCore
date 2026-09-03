using UnityEngine;
using System;
using System.Collections;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace RCore.UI
{
	/// <summary>
	/// Arranges child RectTransforms in a 2-tier circle layout with support for animations.
	/// </summary>
	public class UICircleArranger : UICircleArrangerBase
	{
		[Header("2-Tier Circle Configuration")]
		public float radiusStep = 200f;
		public bool enableRotation;
		[Range(0, 90)] public float maxDegreeBetween = 30;
		[Range(0, 360)] public float startDegree = 45;
		[Range(0, 360)] public float maxDegree = 90;
		public bool centerOnTop = true;

		protected override void CalculatePositions()
		{
			if (m_targets == null)
				return;

			int totalTargets = m_targets.Count;
			if (totalTargets == 0)
				return;

			if (m_newPositions == null || m_newPositions.Length != totalTargets)
				m_newPositions = new Vector3[totalTargets];
			if (m_newRotations == null || m_newRotations.Length != totalTargets)
				m_newRotations = new Quaternion[totalTargets];
			if (m_targetScales == null || m_targetScales.Length != totalTargets)
				m_targetScales = new float[totalTargets];

			for (int i = 0; i < totalTargets; i++)
				m_targetScales[i] = 1f;

			int outerCount, innerCount;

			// Determine the number of targets per circle
			if (totalTargets <= 8)
			{
				outerCount = Mathf.Min(5, totalTargets);
				innerCount = Mathf.Min(3, totalTargets - outerCount);
			}
			else if (totalTargets <= 10)
			{
				outerCount = 6;
				innerCount = Mathf.Min(4, totalTargets - outerCount);
			}
			else
			{
				outerCount = totalTargets * 2 / 3;
				innerCount = totalTargets - outerCount;
			}

			float outerRadius = GetRadius(outerCount, GetAngleStep(outerCount));
			// Arrange outer circle
			ArrangeTargetsOnCircle(0, outerCount, outerRadius);
			// Arrange inner circle
			ArrangeTargetsOnCircle(outerCount, innerCount, outerRadius - radiusStep);
		}

		private float GetAngleStep(int pCount)
		{
			if (pCount <= 0) return 0f;
			float step = maxDegree <= 0 || maxDegree > 360 ? 360f / pCount : (pCount > 1 ? maxDegree / (pCount - 1) : 0f);
			if (step > maxDegreeBetween && maxDegreeBetween > 0)
				step = maxDegreeBetween;
			return step;
		}

		private void ArrangeTargetsOnCircle(int startIdx, int count, float currentRadius)
		{
			if (count == 0)
				return;

			float angleStep = GetAngleStep(count);

			float startAngle = centerOnTop ? 90f - angleStep * (count - 1) / 2 : startDegree;
			Vector2 center = GetCenterPosition();

			for (int i = 0; i < count; i++)
			{
				int idx = startIdx + i;
				if (idx >= m_targets.Count)
					break;

				float xPos = Mathf.Cos(startAngle * Mathf.Deg2Rad) * currentRadius;
				float yPos = Mathf.Sin(startAngle * Mathf.Deg2Rad) * currentRadius;

				m_newPositions[idx] = new Vector2(xPos, yPos) + center;

				if (enableRotation)
				{
					float rotationAngle = Mathf.Atan2(yPos, xPos) * Mathf.Rad2Deg;
					m_newRotations[idx] = Quaternion.Euler(0, 0, rotationAngle);
				}
				else
				{
					m_newRotations[idx] = Quaternion.identity;
				}

				startAngle += angleStep;
			}
		}

#if ODIN_INSPECTOR
		[Button, ShowIf("@UnityEngine.Application.isPlaying")]
#endif
		/// <summary>
		/// Arranges children by moving them from one edge of the circle layout to their target positions.
		/// </summary>
		public void ArrangeFromEdgeWithTween(bool leftToRight)
		{
			CollectTargets();
			CalculatePositions();

			if (leftToRight)
			{
				Array.Reverse(m_newPositions);
				Array.Reverse(m_newRotations);
			}

			for (var i = 0; i < m_targets.Count; i++)
			{
				if (exceptions != null && exceptions.Contains(m_targets[i]))
					continue;

				var target = m_targets[i];
				target.anchoredPosition = m_newPositions[0];
				target.rotation = m_newRotations[0];

				StartCoroutine(MoveToPosition(target, m_newPositions, m_newRotations, i));
			}
		}

		private IEnumerator MoveToPosition(RectTransform target, Vector3[] positions, Quaternion[] rotations, int endIndex)
		{
			float timePerStep = tweenDuration / positions.Length;
			for (int index = 0; index <= endIndex; index++)
			{
				Vector3 startPosition = target.anchoredPosition;
				var startRotation = target.rotation;

				var endPosition = positions[index];
				var endRotation = rotations[index];

				for (float t = 0; t < timePerStep; t += Time.deltaTime)
				{
					float progress = t / timePerStep;
					target.anchoredPosition = Vector2.Lerp(startPosition, endPosition, progress);
					if (endRotation != Quaternion.identity)
						target.rotation = Quaternion.Lerp(startRotation, endRotation, progress);
					yield return null;
				}

				target.anchoredPosition = endPosition;
				if (endRotation != Quaternion.identity)
					target.rotation = endRotation;
			}
		}
	}
}
