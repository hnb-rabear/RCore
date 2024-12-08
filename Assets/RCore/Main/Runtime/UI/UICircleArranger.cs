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
		public bool enableRotation;
		public float tweenDuration = 0.4f;
		[Range(0, 90)] public float maxDegreeBetween = 30;
		[Range(0, 360)] public float startDegree = 45;
		[Range(0, 360)] public float maxDegree = 90;
		public bool centerOnTop = true;
		public float emitInterval = 0.03f;
		public AnimationCurve scaleOverLifeTime;
		public AnimationCurve positionOverLifeTime;
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
				if (t.gameObject.activeSelf && !exceptions.Contains(t))
					m_targets.Add(t as RectTransform);

			// Calculate the angle step based on maxDegree or evenly distributed if maxDegree is 0
			float angleStep = maxDegree <= 0 || maxDegree > 360 ? 360f / m_targets.Count : maxDegree / (m_targets.Count - 1);
			if (angleStep > maxDegreeBetween && maxDegreeBetween > 0)
				angleStep = maxDegreeBetween;
			float angle = startDegree;

			m_newPositions = new Vector3[m_targets.Count];
			m_newRotations = new Quaternion[m_targets.Count];

			for (int i = 0; i < m_targets.Count; i++)
			{
				float xPos = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
				float yPos = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

				var newPos = new Vector2(xPos, yPos);
				m_newPositions[i] = newPos;

				if (enableRotation)
				{
					// Calculate the rotation angle to match the angle on the circle
					float rotationAngle = Mathf.Atan2(yPos, xPos) * Mathf.Rad2Deg;
					m_newRotations[i] = Quaternion.Euler(0, 0, rotationAngle);
				}
				else
					m_newRotations[i] = Quaternion.identity;

				angle += angleStep;
			}

			if (centerOnTop)
			{
				// Arrange targets to extend to both sides, starting from the center top of the circle
				float startAngle = 90f - angleStep * (m_targets.Count - 1) / 2;
				for (int i = 0; i < m_targets.Count; i++)
				{
					float xPos = Mathf.Cos(startAngle * Mathf.Deg2Rad) * radius;
					float yPos = Mathf.Sin(startAngle * Mathf.Deg2Rad) * radius;

					var newPos = new Vector2(xPos, yPos);
					m_newPositions[i] = newPos;

					if (enableRotation)
					{
						// Calculate the rotation angle to match the angle on the circle
						float rotationAngle = Mathf.Atan2(yPos, xPos) * Mathf.Rad2Deg;
						m_newRotations[i] = Quaternion.Euler(0, 0, rotationAngle);
					}
					else
						m_newRotations[i] = Quaternion.identity;

					startAngle += angleStep;
				}
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
						Vector3 position;
						if (positionOverLifeTime.keys.Length > 0)
							position = Vector3.LerpUnclamped(Vector3.zero, m_newPositions[index], positionOverLifeTime.Evaluate(lerp));
						else
							position = Vector3.Lerp(Vector3.zero, m_newPositions[index], lerp);
						target.anchoredPosition = position;

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