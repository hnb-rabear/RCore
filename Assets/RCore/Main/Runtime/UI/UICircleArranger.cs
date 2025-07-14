using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;
using System.Linq;

namespace RCore.UI
{
	public interface ITweenItem
	{
		public void OnStart();
		public void OnFinish();
	}

	public class UICircleArranger : MonoBehaviour
	{
		public float radius = 500f;
		public float radiusStep = 200f;
		public bool enableRotation;
		public float tweenDuration = 0.4f;
		[Range(0, 90)] public float maxDegreeBetween = 30;
		[Range(0, 360)] public float startDegree = 45;
		[Range(0, 360)] public float maxDegree = 90;
		public bool centerOnTop = true;
		public float emitInterval = 0.03f;
		public AnimationCurve scaleOverLifeTime;
		public AnimationCurve positionXOverMoveTime;
		public AnimationCurve positionYOverMoveTime;
		public RectTransform[] exceptions;

		private List<RectTransform> m_targets;
		private Vector3[] m_newPositions;
		private Quaternion[] m_newRotations;

		private void Start()
		{
			Arrange();
		}

		private void OnValidate()
		{
			Arrange();
		}

		private void CalculatePositions()
		{
			m_targets = new List<RectTransform>();
			foreach (Transform t in transform)
			{
				if (t.gameObject.activeSelf && !exceptions.Contains(t))
				{
					m_targets.Add(t as RectTransform);
				}
			}

			m_newPositions = new Vector3[m_targets.Count];
			m_newRotations = new Quaternion[m_targets.Count];

			int totalTargets = m_targets.Count;
			int outerCount, innerCount;
			float currentRadius;

			// Determine the number of targets per circle
			if (totalTargets <= 8)
			{
				outerCount = Mathf.Min(5, totalTargets);
				innerCount = Mathf.Min(3, totalTargets - outerCount);
				currentRadius = radius;
			}
			else if (totalTargets <= 10)
			{
				outerCount = 6;
				innerCount = Mathf.Min(4, totalTargets - outerCount);
				currentRadius = radius;
			}
			else
			{
				outerCount = totalTargets * 2 / 3;
				innerCount = totalTargets - outerCount;
				currentRadius = radius;
			}

			// Arrange outer circle
			ArrangeTargetsOnCircle(0, outerCount, currentRadius);
			// Arrange inner circle
			ArrangeTargetsOnCircle(outerCount, innerCount, currentRadius - radiusStep);
		}

		private void ArrangeTargetsOnCircle(int startIdx, int count, float radius)
		{
			if (count == 0)
				return;

			float angleStep = maxDegree <= 0 || maxDegree > 360 ? 360f / count : maxDegree / (count - 1);
			if (angleStep > maxDegreeBetween && maxDegreeBetween > 0)
				angleStep = maxDegreeBetween;

			float startAngle = centerOnTop ? 90f - angleStep * (count - 1) / 2 : startDegree;

			for (int i = 0; i < count; i++)
			{
				int idx = startIdx + i;
				if (idx >= m_targets.Count)
					break;

				float xPos = Mathf.Cos(startAngle * Mathf.Deg2Rad) * radius;
				float yPos = Mathf.Sin(startAngle * Mathf.Deg2Rad) * radius;

				m_newPositions[idx] = new Vector2(xPos, yPos);

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
		[Button]
#endif
		private void Arrange()
		{
			CalculatePositions();

			for (var i = 0; i < m_targets.Count; i++)
			{
				m_targets[i].anchoredPosition = m_newPositions[i];
				m_targets[i].rotation = m_newRotations[i];
			}
		}

#if ODIN_INSPECTOR
		[Button, ShowIf("@UnityEngine.Application.isPlaying")]
#endif
		public void ArrangeFromEdgeWithTween(bool leftToRight)
		{
			CalculatePositions();

			// If left to right = true, reverse the array
			if (leftToRight)
			{
				Array.Reverse(m_newPositions);
				Array.Reverse(m_newRotations);
			}

			for (var i = 0; i < m_targets.Count; i++)
			{
				if (exceptions.Contains(m_targets[i]))
					continue;

				var target = m_targets[i];
				target.anchoredPosition = m_newPositions[0];
				target.rotation = m_newRotations[0];

				StartCoroutine(MoveToPosition(target, m_newPositions, m_newRotations, i));

				// var moveSequence = DOTween.Sequence();
				// for (int index = 0; index <= i; index++)
				// {
				// 	var position = m_newPositions[index];
				// 	moveSequence.Append(target.DOAnchorPos(position, tweenDuration / m_newPositions.Length));
				// 	if (m_newRotations[index] != Quaternion.identity)
				// 		moveSequence.Join(target.DORotateQuaternion(m_newRotations[index], tweenDuration / m_newPositions.Length));
				// }
				// moveSequence.Play();
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

				// Ensure final position and rotation are exactly at the target
				target.anchoredPosition = endPosition;
				if (endRotation != Quaternion.identity)
				{
					target.rotation = endRotation;
				}
			}
		}


#if ODIN_INSPECTOR
		[Button, ShowIf("@UnityEngine.Application.isPlaying")]
#endif
		public void ArrangeFromCenterWithTween(Action pCallback)
		{
			CalculatePositions();

			if (m_targets.Count == 0)
			{
				pCallback?.Invoke();
				return;
			}
			for (var i = 0; i < m_targets.Count; i++)
			{
				int index = i;
				var target = m_targets[i];
				if (m_newRotations[i] != Quaternion.identity)
					target.DORotateQuaternion(m_newRotations[i], tweenDuration).SetUpdate(true);

				float lerp = 0;
				target.localPosition = Vector3.zero;
				target.localScale = Vector3.zero;
				DOTween.To(() => lerp, x => lerp = x, 1, tweenDuration)
					.OnStart(() =>
					{
						if (target.TryGetComponent(out ITweenItem item))
							item.OnStart();
					})
					.OnUpdate(() =>
					{
						var pos = m_newPositions[index];
						if (m_newPositions.Length > 0)
							pos.x = Mathf.LerpUnclamped(0, m_newPositions[index].x, positionXOverMoveTime.Evaluate(lerp));
						if (m_newPositions.Length > 0)
							pos.y = Mathf.LerpUnclamped(0, m_newPositions[index].y, positionYOverMoveTime.Evaluate(lerp));
						target.anchoredPosition = pos;

						if (scaleOverLifeTime.keys.Length > 0)
						{
							var scale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, scaleOverLifeTime.Evaluate(lerp));
							target.transform.localScale = scale;
						}
					})
					.OnComplete(() =>
					{
						if (index == m_targets.Count - 1)
							pCallback?.Invoke();

						if (target.TryGetComponent(out ITweenItem item))
							item.OnFinish();
					})
					.SetDelay(emitInterval * index)
					.SetUpdate(true);
			}
		}

#if ODIN_INSPECTOR
		[Button, ShowIf("@UnityEngine.Application.isPlaying")]
#endif
		public void RefreshTargetPositionsWithTween()
		{
			CalculatePositions();

			for (var i = 0; i < m_targets.Count; i++)
			{
				int i1 = i;
				var target = m_targets[i];
				var targetPrePosition = target.anchoredPosition;
				float lerp = 0;
				DOTween.Kill(GetInstanceID());
				DOTween.To(() => lerp, x => lerp = x, 1f, tweenDuration)
					.OnUpdate(() =>
					{
						if (m_newRotations[i1] != Quaternion.identity)
						{
							var rotation = Quaternion.LerpUnclamped(Quaternion.identity, m_newRotations[i1], lerp);
							target.rotation = rotation;
						}
						target.anchoredPosition = Vector3.LerpUnclamped(targetPrePosition, m_newPositions[i1], lerp);
					}).SetUpdate(true).SetId(GetInstanceID());
			}
		}
	}
}