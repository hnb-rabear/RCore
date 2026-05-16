using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RevCore.UI
{
	/// <summary>Optional interface a child of a <see cref="UICircleArranger"/> can implement to receive start/finish callbacks during animated arrangement.</summary>
	public interface ITweenItem
	{
		/// <summary>Called when the per-item tween starts.</summary>
		void OnStart();
		/// <summary>Called when the per-item tween completes.</summary>
		void OnFinish();
	}

	/// <summary>
	/// Arranges children on one or two concentric circles. Used for radial menus and "card fan" UI.
	/// Provides three animation entry points: instant <c>Arrange</c>, edge-to-target sweep
	/// (<see cref="ArrangeFromEdgeWithTween"/>), and center-to-target burst with curves
	/// (<see cref="ArrangeFromCenterWithTween"/>).
	/// </summary>
	public class UICircleArranger : MonoBehaviour
	{
		/// <summary>Outer ring radius in local units.</summary>
		public float radius = 500f;
		/// <summary>Distance between the outer and inner rings.</summary>
		public float radiusStep = 200f;
		/// <summary>When true, each child rotates so its local Y axis points outward.</summary>
		public bool enableRotation;
		/// <summary>Tween duration in seconds.</summary>
		public float tweenDuration = 0.4f;
		/// <summary>Maximum angular gap between adjacent children (degrees).</summary>
		[Range(0, 90)] public float maxDegreeBetween = 30;
		/// <summary>Start angle in degrees (used when <see cref="centerOnTop"/> is false).</summary>
		[Range(0, 360)] public float startDegree = 45;
		/// <summary>Total arc covered by the children. 0 or 360 means full circle.</summary>
		[Range(0, 360)] public float maxDegree = 90;
		/// <summary>When true, layout is centered around the top (90°). Otherwise starts at <see cref="startDegree"/>.</summary>
		public bool centerOnTop = true;
		/// <summary>Stagger between consecutive item tweens (seconds).</summary>
		public float emitInterval = 0.03f;
		/// <summary>Curve evaluated 0..1 controlling each child's scale during the tween.</summary>
		public AnimationCurve scaleOverLifeTime;
		/// <summary>Curve evaluated 0..1 controlling X movement during the tween.</summary>
		public AnimationCurve positionXOverMoveTime;
		/// <summary>Curve evaluated 0..1 controlling Y movement during the tween.</summary>
		public AnimationCurve positionYOverMoveTime;
		/// <summary>Children excluded from arrangement (e.g. a center anchor).</summary>
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

			ArrangeTargetsOnCircle(0, outerCount, currentRadius);
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

		private void Arrange()
		{
			CalculatePositions();

			for (var i = 0; i < m_targets.Count; i++)
			{
				m_targets[i].anchoredPosition = m_newPositions[i];
				m_targets[i].rotation = m_newRotations[i];
			}
		}

		/// <summary>Animates items sweeping in from the edge of the layout. <paramref name="leftToRight"/> picks the direction.</summary>
		public void ArrangeFromEdgeWithTween(bool leftToRight)
		{
			CalculatePositions();

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
				{
					target.rotation = endRotation;
				}
			}
		}


		/// <summary>Animates items bursting outward from center using the position/scale curves. <paramref name="pCallback"/> fires when the last item finishes.</summary>
		public void ArrangeFromCenterWithTween(Action pCallback)
		{
			CalculatePositions();

			if (m_targets.Count == 0)
			{
				pCallback?.Invoke();
				return;
			}
#if DOTWEEN
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
#else
			StartCoroutine(ArrangeFromCenterCoroutine(pCallback));
#endif
		}

#if !DOTWEEN
		private IEnumerator ArrangeFromCenterCoroutine(Action pCallback)
		{
			for (var i = 0; i < m_targets.Count; i++)
			{
				int index = i;
				var target = m_targets[i];
				float delay = emitInterval * index;
				StartCoroutine(ArrangeOneTargetFromCenter(target, index, delay, index == m_targets.Count - 1 ? pCallback : null));
			}
			yield return null;
		}

		private IEnumerator ArrangeOneTargetFromCenter(RectTransform target, int index, float delay, Action pCallback)
		{
			if (delay > 0)
				yield return new WaitForSeconds(delay);

			if (m_newRotations[index] != Quaternion.identity)
				target.rotation = m_newRotations[index];

			target.localPosition = Vector3.zero;
			target.localScale = Vector3.zero;

			if (target.TryGetComponent(out ITweenItem item))
				item.OnStart();

			float elapsed = 0;
			while (elapsed < tweenDuration)
			{
				elapsed += Time.deltaTime;
				float lerp = elapsed / tweenDuration;

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
				yield return null;
			}

			var finalPos = m_newPositions[index];
			target.anchoredPosition = finalPos;
			target.localScale = Vector3.one;

			pCallback?.Invoke();
			if (target.TryGetComponent(out ITweenItem itemFinish))
				itemFinish.OnFinish();
		}
#endif

		/// <summary>Animates items from their current positions to the freshly computed target positions. Use after the child set changes.</summary>
		public void RefreshTargetPositionsWithTween()
		{
			CalculatePositions();
#if DOTWEEN
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
#else
			StopAllCoroutines();
			StartCoroutine(RefreshTargetPositionsCoroutine());
#endif
		}
		
		private IEnumerator RefreshTargetPositionsCoroutine()
		{
			for (var i = 0; i < m_targets.Count; i++)
			{
				int index = i;
				var target = m_targets[i];
				StartCoroutine(RefreshOneTargetPosition(target, index));
			}
			yield return null;
		}

		private IEnumerator RefreshOneTargetPosition(RectTransform target, int index)
		{
			var targetPrePosition = target.anchoredPosition;
			var startRotation = target.rotation;
			float elapsed = 0;

			while (elapsed < tweenDuration)
			{
				elapsed += Time.deltaTime;
				float lerp = elapsed / tweenDuration;

				if (index < m_newRotations.Length && m_newRotations[index] != Quaternion.identity)
				{
					var rotation = Quaternion.LerpUnclamped(startRotation, m_newRotations[index], lerp);
					target.rotation = rotation;
				}
				if (index < m_newPositions.Length)
					target.anchoredPosition = Vector3.LerpUnclamped(targetPrePosition, m_newPositions[index], lerp);

				yield return null;
			}

			if (index < m_newRotations.Length && m_newRotations[index] != Quaternion.identity)
				target.rotation = m_newRotations[index];
			if (index < m_newPositions.Length)
				target.anchoredPosition = m_newPositions[index];
		}
	}
}
