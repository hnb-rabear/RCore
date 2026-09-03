using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace RCore.UI
{
	public interface ITweenItem
	{
		void OnStart();
		void OnFinish();
	}

	/// <summary>
	/// Base class for arranging child RectTransforms along circular paths with tweening and Editor preview support.
	/// </summary>
	public abstract class UICircleArrangerBase : MonoBehaviour
	{
		[Header("Base Circle Settings")]
		public float minRadius = 200f;
		public float maxRadius = 500f;
		public float preferDistance = 150f;

		[Header("Circle Center")]
		[SerializeField] private RectTransform m_center;

		[Header("Tween Settings")]
		public float tweenDuration = 0.4f;
		public float emitInterval = 0.03f;
		public AnimationCurve scaleOverLifeTime = AnimationCurve.EaseInOut(0, 0, 1, 1);
		public AnimationCurve positionXOverMoveTime = AnimationCurve.EaseInOut(0, 0, 1, 1);
		public AnimationCurve positionYOverMoveTime = AnimationCurve.EaseInOut(0, 0, 1, 1);

		[Header("Editor Preview")]
		[SerializeField, Range(0f, 1f)] protected float m_fill = 1f;

		[Header("Exceptions")]
		public RectTransform[] exceptions;

		protected List<RectTransform> m_targets = new List<RectTransform>();
		protected Vector3[] m_newPositions;
		protected Quaternion[] m_newRotations;
		protected float[] m_targetScales;

		public virtual float FillAmount
		{
			get => m_fill;
			set
			{
				m_fill = Mathf.Clamp01(value);
				SetFill(m_fill);
			}
		}

		public virtual Vector2 GetStartPosition()
		{
			return Vector2.zero;
		}

		/// <summary>
		/// Circle center in local space. If m_center assigned, converts its world position;
		/// otherwise returns Vector2.zero (arranger's own origin).
		/// </summary>
		public virtual Vector2 GetCenterPosition()
		{
			if (m_center != null)
				return transform.InverseTransformPoint(m_center.position);
			return Vector2.zero;
		}

		/// <summary>
		/// Compute radius from target count and angle step.
		/// Keeps preferDistance between neighbors; clamps to [minRadius, maxRadius].
		/// When radius hits maxRadius, distance between targets shrinks naturally.
		/// </summary>
		public virtual float GetRadius(int pCount, float pAngleStep)
		{
			if (pCount <= 1 || pAngleStep <= 0 || preferDistance <= 0)
				return minRadius;

			float sine = Mathf.Sin(pAngleStep * Mathf.Deg2Rad * 0.5f);
			if (sine <= 0.0001f)
				return maxRadius;

			float needed = preferDistance / (2f * sine);
			return Mathf.Clamp(needed, minRadius, maxRadius);
		}

		protected virtual void Start()
		{
			Arrange();
		}

		protected virtual void OnValidate()
		{
			CollectTargets();
			CalculatePositions();
			SetFill(m_fill);
		}

		protected void CollectTargets()
		{
			if (m_targets == null)
				m_targets = new List<RectTransform>();
			else
				m_targets.Clear();

			foreach (Transform t in transform)
			{
				if (t.gameObject.activeSelf && (exceptions == null || !exceptions.Contains(t)))
				{
					if (t is RectTransform rt)
						m_targets.Add(rt);
				}
			}

			int count = m_targets.Count;
			if (m_newPositions == null || m_newPositions.Length != count)
				m_newPositions = new Vector3[count];
			if (m_newRotations == null || m_newRotations.Length != count)
				m_newRotations = new Quaternion[count];
			if (m_targetScales == null || m_targetScales.Length != count)
				m_targetScales = new float[count];
		}

		protected abstract void CalculatePositions();

#if ODIN_INSPECTOR
		[Button]
#endif
		public virtual void Arrange()
		{
			CollectTargets();
			CalculatePositions();
			SetFill(1f);
		}

		public virtual void SetFill(float pFill)
		{
			if (m_targets == null || m_targets.Count == 0)
				return;

			var start = GetStartPosition();
			int count = m_targets.Count;
			float totalDuration = tweenDuration + emitInterval * (count - 1);

			for (int i = 0; i < count; i++)
			{
				var target = m_targets[i];
				if (target == null)
					continue;

				// Per-target fill: accounts for emitInterval stagger
				float startTime = emitInterval * i;
				float currentTime = pFill * totalDuration;
				float targetFill = tweenDuration > 0
					? Mathf.Clamp01((currentTime - startTime) / tweenDuration)
					: pFill;

				if (m_newPositions != null && i < m_newPositions.Length)
				{
					var targetPos = m_newPositions[i];
					float posX = positionXOverMoveTime != null && positionXOverMoveTime.keys.Length > 0
						? Mathf.LerpUnclamped(start.x, targetPos.x, positionXOverMoveTime.Evaluate(targetFill))
						: Mathf.LerpUnclamped(start.x, targetPos.x, targetFill);

					float posY = positionYOverMoveTime != null && positionYOverMoveTime.keys.Length > 0
						? Mathf.LerpUnclamped(start.y, targetPos.y, positionYOverMoveTime.Evaluate(targetFill))
						: Mathf.LerpUnclamped(start.y, targetPos.y, targetFill);

					target.anchoredPosition = new Vector2(posX, posY);
				}

				float scale = (m_targetScales != null && i < m_targetScales.Length && m_targetScales[i] > 0) ? m_targetScales[i] : 1f;
				if (scaleOverLifeTime != null && scaleOverLifeTime.keys.Length > 0)
				{
					float s = Mathf.LerpUnclamped(0, scale, scaleOverLifeTime.Evaluate(targetFill));
					target.localScale = Vector3.one * s;
				}
				else
				{
					target.localScale = Vector3.one * Mathf.LerpUnclamped(0, scale, targetFill);
				}

				if (m_newRotations != null && i < m_newRotations.Length && m_newRotations[i] != Quaternion.identity)
					target.rotation = Quaternion.LerpUnclamped(Quaternion.identity, m_newRotations[i], targetFill);
				else
					target.rotation = Quaternion.identity;
			}
		}

#if UNITY_EDITOR
		private double m_previewStartTime = -1;

#if ODIN_INSPECTOR
		[Button("Preview Animation"), HideIf("@UnityEngine.Application.isPlaying")]
#endif
		public void PreviewAnimation()
		{
			CollectTargets();
			CalculatePositions();
			m_previewStartTime = EditorApplication.timeSinceStartup;
			EditorApplication.update -= EditorPreviewTick;
			EditorApplication.update += EditorPreviewTick;
		}

		private void EditorPreviewTick()
		{
			if (this == null)
			{
				EditorApplication.update -= EditorPreviewTick;
				return;
			}

			int count = m_targets != null ? m_targets.Count : 0;
			float totalDuration = tweenDuration + emitInterval * Mathf.Max(0, count - 1);
			float elapsed = (float)(EditorApplication.timeSinceStartup - m_previewStartTime);
			float fill = totalDuration > 0 ? Mathf.Clamp01(elapsed / totalDuration) : 1f;

			m_fill = fill;
			SetFill(fill);

			if (fill >= 1f)
				EditorApplication.update -= EditorPreviewTick;
		}

		private void OnDestroy()
		{
			EditorApplication.update -= EditorPreviewTick;
		}
#endif

#if ODIN_INSPECTOR
		[Button, ShowIf("@UnityEngine.Application.isPlaying")]
#endif
		public virtual void ArrangeFromCenterWithTween(Action pCallback)
		{
			CollectTargets();
			CalculatePositions();

			if (m_targets.Count == 0)
			{
				pCallback?.Invoke();
				return;
			}

			var start = GetStartPosition();

#if DOTWEEN
			for (int i = 0; i < m_targets.Count; i++)
			{
				int index = i;
				var target = m_targets[i];
				float targetScale = (m_targetScales != null && i < m_targetScales.Length && m_targetScales[i] > 0) ? m_targetScales[i] : 1f;

				if (m_newRotations != null && index < m_newRotations.Length && m_newRotations[index] != Quaternion.identity)
					target.DORotateQuaternion(m_newRotations[index], tweenDuration).SetDelay(emitInterval * index).SetUpdate(true);

				float lerp = 0;
				target.anchoredPosition = start;
				target.localScale = Vector3.zero;

				DOTween.To(() => lerp, x => lerp = x, 1, tweenDuration)
					.OnStart(() =>
					{
						if (target.TryGetComponent(out ITweenItem item))
							item.OnStart();
					})
					.OnUpdate(() =>
					{
						var targetPos = m_newPositions[index];
						float posX = positionXOverMoveTime != null && positionXOverMoveTime.keys.Length > 0
							? Mathf.LerpUnclamped(start.x, targetPos.x, positionXOverMoveTime.Evaluate(lerp))
							: Mathf.LerpUnclamped(start.x, targetPos.x, lerp);

						float posY = positionYOverMoveTime != null && positionYOverMoveTime.keys.Length > 0
							? Mathf.LerpUnclamped(start.y, targetPos.y, positionYOverMoveTime.Evaluate(lerp))
							: Mathf.LerpUnclamped(start.y, targetPos.y, lerp);

						target.anchoredPosition = new Vector2(posX, posY);

						if (scaleOverLifeTime != null && scaleOverLifeTime.keys.Length > 0)
						{
							float s = Mathf.LerpUnclamped(0, targetScale, scaleOverLifeTime.Evaluate(lerp));
							target.localScale = Vector3.one * s;
						}
						else
						{
							target.localScale = Vector3.one * Mathf.LerpUnclamped(0, targetScale, lerp);
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
		protected virtual IEnumerator ArrangeFromCenterCoroutine(Action pCallback)
		{
			var start = GetStartPosition();
			for (int i = 0; i < m_targets.Count; i++)
			{
				int index = i;
				var target = m_targets[i];
				float delay = emitInterval * index;
				StartCoroutine(ArrangeOneTargetFromCenter(target, index, delay, start, index == m_targets.Count - 1 ? pCallback : null));
			}
			yield return null;
		}

		protected virtual IEnumerator ArrangeOneTargetFromCenter(RectTransform target, int index, float delay, Vector2 start, Action pCallback)
		{
			if (delay > 0)
				yield return new WaitForSeconds(delay);

			if (m_newRotations != null && index < m_newRotations.Length && m_newRotations[index] != Quaternion.identity)
				target.rotation = m_newRotations[index];

			float targetScale = (m_targetScales != null && index < m_targetScales.Length && m_targetScales[index] > 0) ? m_targetScales[index] : 1f;
			target.anchoredPosition = start;
			target.localScale = Vector3.zero;

			if (target.TryGetComponent(out ITweenItem item))
				item.OnStart();

			float elapsed = 0;
			while (elapsed < tweenDuration)
			{
				elapsed += Time.deltaTime;
				float lerp = elapsed / tweenDuration;

				var targetPos = m_newPositions[index];
				float posX = positionXOverMoveTime != null && positionXOverMoveTime.keys.Length > 0
					? Mathf.LerpUnclamped(start.x, targetPos.x, positionXOverMoveTime.Evaluate(lerp))
					: Mathf.LerpUnclamped(start.x, targetPos.x, lerp);

				float posY = positionYOverMoveTime != null && positionYOverMoveTime.keys.Length > 0
					? Mathf.LerpUnclamped(start.y, targetPos.y, positionYOverMoveTime.Evaluate(lerp))
					: Mathf.LerpUnclamped(start.y, targetPos.y, lerp);

				target.anchoredPosition = new Vector2(posX, posY);

				if (scaleOverLifeTime != null && scaleOverLifeTime.keys.Length > 0)
				{
					float s = Mathf.LerpUnclamped(0, targetScale, scaleOverLifeTime.Evaluate(lerp));
					target.localScale = Vector3.one * s;
				}
				else
				{
					target.localScale = Vector3.one * Mathf.LerpUnclamped(0, targetScale, lerp);
				}
				yield return null;
			}

			target.anchoredPosition = m_newPositions[index];
			target.localScale = Vector3.one * targetScale;

			pCallback?.Invoke();
			if (target.TryGetComponent(out ITweenItem itemFinish))
				itemFinish.OnFinish();
		}
#endif

#if ODIN_INSPECTOR
		[Button, ShowIf("@UnityEngine.Application.isPlaying")]
#endif
		public virtual void RefreshTargetPositionsWithTween()
		{
			CollectTargets();
			CalculatePositions();
#if DOTWEEN
			DOTween.Kill(GetInstanceID());
			for (int i = 0; i < m_targets.Count; i++)
			{
				int index = i;
				var target = m_targets[i];
				var targetPrePosition = target.anchoredPosition;
				var startRotation = target.rotation;
				var startScale = target.localScale;
				float targetScale = (m_targetScales != null && index < m_targetScales.Length && m_targetScales[index] > 0) ? m_targetScales[index] : 1f;
				float lerp = 0;

				DOTween.To(() => lerp, x => lerp = x, 1f, tweenDuration)
					.OnUpdate(() =>
					{
						if (m_newRotations != null && index < m_newRotations.Length && m_newRotations[index] != Quaternion.identity)
							target.rotation = Quaternion.LerpUnclamped(startRotation, m_newRotations[index], lerp);

						target.anchoredPosition = Vector3.LerpUnclamped(targetPrePosition, m_newPositions[index], lerp);
						target.localScale = Vector3.LerpUnclamped(startScale, Vector3.one * targetScale, lerp);
					})
					.SetUpdate(true)
					.SetId(GetInstanceID());
			}
#else
			StopAllCoroutines();
			StartCoroutine(RefreshTargetPositionsCoroutine());
#endif
		}

#if !DOTWEEN
		protected virtual IEnumerator RefreshTargetPositionsCoroutine()
		{
			for (int i = 0; i < m_targets.Count; i++)
			{
				StartCoroutine(RefreshOneTargetPosition(m_targets[i], i));
			}
			yield return null;
		}

		protected virtual IEnumerator RefreshOneTargetPosition(RectTransform target, int index)
		{
			var targetPrePosition = target.anchoredPosition;
			var startRotation = target.rotation;
			var startScale = target.localScale;
			float targetScale = (m_targetScales != null && index < m_targetScales.Length && m_targetScales[index] > 0) ? m_targetScales[index] : 1f;
			float elapsed = 0;

			while (elapsed < tweenDuration)
			{
				elapsed += Time.deltaTime;
				float lerp = elapsed / tweenDuration;

				if (m_newRotations != null && index < m_newRotations.Length && m_newRotations[index] != Quaternion.identity)
					target.rotation = Quaternion.LerpUnclamped(startRotation, m_newRotations[index], lerp);

				if (m_newPositions != null && index < m_newPositions.Length)
					target.anchoredPosition = Vector3.LerpUnclamped(targetPrePosition, m_newPositions[index], lerp);

				target.localScale = Vector3.LerpUnclamped(startScale, Vector3.one * targetScale, lerp);
				yield return null;
			}

			if (m_newRotations != null && index < m_newRotations.Length && m_newRotations[index] != Quaternion.identity)
				target.rotation = m_newRotations[index];
			if (m_newPositions != null && index < m_newPositions.Length)
				target.anchoredPosition = m_newPositions[index];
			target.localScale = Vector3.one * targetScale;
		}
#endif
	}
}
