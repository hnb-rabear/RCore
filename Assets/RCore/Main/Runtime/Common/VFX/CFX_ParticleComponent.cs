/***
 * Author HNB-RaBear - 2019
 **/

#pragma warning disable 0649

#if DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore
{
	public class CFX_ParticleComponent : CFX_Component
	{
		public bool isLoop;
		public Action onFinishedMovement;
		public Action onFinishedMovementSeparately;
		[SerializeField] private ParticleSystem mMain;
		[SerializeField] private ParticleSystem[] mParticleSystems;
		[SerializeField] private ParticleSystem mControllableParticle;
		[FormerlySerializedAs("radius")] [SerializeField] private float m_radius;
		[SerializeField] private bool m_Gizmos;
		[FormerlySerializedAs("gizmosColor")] public Color m_gizmosColor;

		private ParticleSystem.Particle[] mParticles;
		private Vector3[] mParticlesPos;

		private void OnDrawGizmos()
		{
			if (!m_Gizmos)
				return;
			Gizmos.color = m_gizmosColor.SetAlpha(0.15f);
			Gizmos.DrawWireSphere(mMain.transform.position, m_radius);
		}

		public override void Play(bool pAutoDeactivate, float pCustomLifeTime = 0)
		{
			if (!initialized)
				Initialize();

			gameObject.SetActive(true);

			foreach (var p in mParticleSystems)
			{
				p.Clear();
				p.Play();

				var emission = p.emission;
				emission.enabled = true;
			}

			base.Play(pAutoDeactivate, pCustomLifeTime);
		}

		public override void Stop()
		{
			if (!initialized)
				Initialize();

			foreach (var p in mParticleSystems)
			{
				p.Stop();
				var emission = p.emission;
				emission.enabled = false;
			}
		}

		public override void Clear()
		{
			if (!initialized)
				Initialize();

			foreach (var p in mParticleSystems)
				p.Clear();
		}

		public ParticleSystem[] GetParticleSystems()
		{
			return mParticleSystems;
		}

		private ParticleSystem.Particle[] GetEmits()
		{
			if (!initialized)
				Initialize();

			mParticles = new ParticleSystem.Particle[mControllableParticle.particleCount];
			return mParticles;
		}

		private Vector3[] GetEmitsPosition()
		{
			if (!initialized)
				Initialize();

			mParticlesPos = new Vector3[mControllableParticle.particleCount];
			int particlesCount = mControllableParticle.GetParticles(mParticles);
			for (int i = 0; i < particlesCount; i++)
			{
				mParticlesPos[i] = mParticles[i].position;
			}

			return mParticlesPos;
		}

		public IEnumerator IE_MoveEmits(Transform pDestination, float pDuration, float pDelay = 0)
		{
			if (pDelay > 0)
				yield return new WaitForSeconds(pDelay);

			GetEmits();
			GetEmitsPosition();

			float lerpTime = 0;
			while (true)
			{
				lerpTime += Time.deltaTime;

				if (lerpTime > pDuration)
					lerpTime = pDuration;

				float rate = lerpTime / pDuration;

				for (int i = 0; i < mParticles.Length; i++)
				{
					mParticles[i].position = Vector3.Lerp(mParticlesPos[i], pDestination.position, rate);
				}

				mControllableParticle.SetParticles(mParticles, mParticles.Length);

				if (lerpTime == pDuration)
					break;
				else
					yield return null;
			}

			onFinishedMovement?.Invoke();
		}

		public void StopMovement()
		{
			GetEmits();
			GetEmitsPosition();

			for (int i = 0; i < mParticles.Length; i++)
			{
				mParticles[i].velocity = Vector3.zero;
			}

			mControllableParticle.SetParticles(mParticles, mParticles.Length);
		}

		public void Loop(bool pValue)
		{
			if (!initialized)
				Initialize();

			isLoop = pValue;
			foreach (var ps in mParticleSystems)
			{
				var main = ps.main;
				main.loop = pValue;
			}
		}

#if DOTWEEN
        public IEnumerator IE_MoveEmitsEase(Transform pDestination, float pDuration, float pDelay = 0)
        {
            if (pDelay > 0)
                yield return new WaitForSeconds(pDelay);

            GetEmits();
            GetEmitsPosition();

            bool process = true;

            float perc = 0;
            DOTween.To(tweenVal => perc = tweenVal, 0f, 1f, pDuration)
                .SetEase(Ease.InCirc)
                .OnUpdate(() =>
                {
                    for (int i = 0; i < mParticles.Length; i++)
                    {
                        mParticles[i].position = Vector3.Lerp(mParticlesPos[i], pDestination.position, perc);
                    }

                    mControllableParticle.SetParticles(mParticles, mParticles.Length);
                })
                .OnComplete(() =>
                {
                    process = false;
                });

            yield return new WaitUntil(() => !process);

			onFinishedMovement?.Invoke();
		}
#else
		public IEnumerator IE_MoveEmitsEase(Transform pDestination, float pDuration, float pDelay = 0)
		{
			if (pDelay > 0)
				yield return new WaitForSeconds(pDelay);

			GetEmits();
			GetEmitsPosition();

			float time = 0;
			while (true)
			{
				yield return null;
				time += Time.deltaTime;
				if (time >= pDuration)
					time = pDuration;
				var lerp = time / pDuration;
				for (int i = 0; i < mParticles.Length; i++)
					mParticles[i].position = Vector3.Lerp(mParticlesPos[i], pDestination.position, lerp);

				mControllableParticle.SetParticles(mParticles, mParticles.Length);

				if (lerp >= 1)
					break;
			}

			onFinishedMovement?.Invoke();
		}
#endif

		public IEnumerator IE_MoveEmitsSeparately(Transform pDestination, float pDuration, float pDelay = 0)
		{
			if (pDelay > 0)
				yield return new WaitForSeconds(pDelay);

			GetEmits();
			GetEmitsPosition();

			bool process = true;

			var mDelayEach = new WaitForSeconds(pDuration - pDuration / mParticles.Length);

			for (int i = 0; i < mParticles.Length; i++)
			{
				MoveEmit(pDestination, pDuration, i);

				yield return new WaitForSeconds(pDuration / mParticles.Length);

				if (i == mParticles.Length - 1)
				{
					yield return mDelayEach;
					process = false;
				}
			}

			yield return new WaitUntil(() => !process);

			onFinishedMovement?.Invoke();
		}

#if DOTWEEN
        private void MoveEmit(Transform pDestination, float pDuration, int pIndex)
        {
            if (mParticles.Length > pIndex)
            {
                bool tweenBreaked = false;

                float perc = 0;
                DOTween.To(tweenVal => perc = tweenVal, 0f, 1f, pDuration)
                    .SetEase(Ease.InCirc)
                    .OnUpdate(() =>
                    {
                        if (tweenBreaked)
                            return;

                        try
                        {
                            mParticles[pIndex].position = Vector3.Lerp(mParticlesPos[pIndex], pDestination.position, perc);
                            mControllableParticle.SetParticles(mParticles, mParticles.Length);
                        }
                        catch (Exception ex)
                        {
                            tweenBreaked = true;

                            Debug.LogError(ex.ToString());
                        }
                    })
                    .OnComplete(() =>
                    {
                        try
                        {
                            mParticles[pIndex].startColor = Color.clear;
							onFinishedMovementSeparately?.Invoke();
						}
                        catch (Exception ex)
                        {
                            Debug.LogError(ex.ToString());
                        }
                    });
            }
        }
#else
		private void MoveEmit(Transform pDestination, float pDuration, int pIndex)
		{
			StartCoroutine(IEMoveEmit(pDestination, pDuration, pIndex));
		}

		private IEnumerator IEMoveEmit(Transform pDestination, float pDuration, int pIndex)
		{
			if (mParticles.Length > pIndex)
			{
				bool broken = false;

				float time = 0;
				while (true)
				{
					yield return null;
					time += Time.time;
					if (time >= pDuration)
						time = pDuration;
					float lerp = time / pDuration;

					try
					{
						mParticles[pIndex].position = Vector3.Lerp(mParticlesPos[pIndex], pDestination.position, lerp);
						mControllableParticle.SetParticles(mParticles, mParticles.Length);
					}
					catch (Exception ex)
					{
						broken = true;
						Debug.LogError(ex.ToString());
					}

					if (broken || lerp >= 1)
						break;
				}
				try
				{
					mParticles[pIndex].startColor = Color.clear;
					onFinishedMovementSeparately?.Invoke();
				}
				catch (Exception ex)
				{
					Debug.LogError(ex.ToString());
				}
			}
		}
#endif

		public void SetColor(Color pColor, bool pOverrideAlpha = true)
		{
			if (!initialized)
				Initialize();

			for (int i = 0; i < mParticleSystems.Length; i++)
			{
				var main = mParticleSystems[i].main;
				if (pOverrideAlpha)
				{
					var preColor = main.startColor.color;
					pColor.a = preColor.a;
				}
				main.startColor = pColor;
			}
		}

		public void SetGradient(Gradient pGradient)
		{
			for (int i = 0; i < mParticleSystems.Length; i++)
			{
				var main = mParticleSystems[i].main;
				var gradient = main.startColor.gradient;
				main.startColor = pGradient;
			}
		}

		public void SetGradient(Color[] pColors, float[] pAlphas)
		{
			if (!initialized)
				Initialize();

			var colorKeysList = new List<GradientColorKey>();
			var alphaKeysList = new List<GradientAlphaKey>();

			float keyPosition = 0;
			for (int i = 0; i < pColors.Length; i++)
			{
				colorKeysList.Add(new GradientColorKey(pColors[i], keyPosition));
				if (pAlphas.Length > i)
					alphaKeysList.Add(new GradientAlphaKey(pColors[i].a, keyPosition));
				else
					alphaKeysList.Add(new GradientAlphaKey(pAlphas[i], keyPosition));
				keyPosition += 1f / (pColors.Length - 1);
			}

			var colorKeysArray = colorKeysList.ToArray();
			var alphaKeysArray = alphaKeysList.ToArray();

			for (int i = 0; i < mParticleSystems.Length; i++)
			{
				var main = mParticleSystems[i].main;

				var gradient = main.startColor.gradient;
				if (gradient != null)
				{
					gradient.SetKeys(colorKeysArray, alphaKeysArray);
					main.startColor = gradient;
				}
				else
				{
#if UNITY_EDITOR
					Debug.LogError("Particle must set start color as gradient!");
#endif
				}
			}
		}

		public void Scale(float pRadius)
		{
			transform.localScale = Vector3.one * pRadius / m_radius;
		}

		public void Scale(SphereCollider pRadius)
		{
			transform.localScale = Vector3.one * pRadius.radius / m_radius;
		}

		public void Scale(CapsuleCollider pRadius)
		{
			transform.localScale = Vector3.one * pRadius.radius / m_radius;
		}

		public void Scale(Collider pRadius)
		{
			transform.localScale = pRadius switch
			{
				CapsuleCollider capsuleCollider => Vector3.one * capsuleCollider.radius / m_radius,
				SphereCollider sphereCollider => Vector3.one * sphereCollider.radius / m_radius,
				BoxCollider boxCollider => Vector3.one * (boxCollider.size.x + boxCollider.size.z) / 3 / m_radius,
				_ => transform.localScale
			};
		}

		public void SetColors(Color pColor1, Color pColor2, bool pOverrideAlpha = true)
		{
			if (!initialized)
				Initialize();
			for (int i = 0; i < mParticleSystems.Length; i++)
			{
				var main = mParticleSystems[i].main;
				var startColor = main.startColor;
				var color2 = main.startColor;
				var pColor0 = pColor1;
				if (!pOverrideAlpha)
				{
					pColor0.a = startColor.color.a;
					pColor1.a = startColor.colorMin.a;
					pColor2.a = startColor.colorMax.a;
				}
				startColor.color = pColor0;
				startColor.colorMin = pColor1;
				startColor.colorMax = pColor2;
				main.startColor = startColor;
			}
		}

		[ContextMenu("Validate")]
		protected override void Validate()
		{
			var lifeTimes = new List<float>();
			var durations = new List<float>();
			isLoop = false;

			if (mMain == null)
				mMain = gameObject.GetComponentInChildren<ParticleSystem>();
			if (mParticleSystems == null || mParticleSystems.Length == 0)
				mParticleSystems = gameObject.FindComponentsInChildren<ParticleSystem>().ToArray();
			if (renderers == null || renderers.Length == 0)
				renderers = gameObject.FindComponentsInChildren<Renderer>().ToArray();

			for (int i = 0; i < mParticleSystems.Length; i++)
			{
				if (mParticleSystems[i] == null)
				{
					Debug.LogError(gameObject.name);
					continue;
				}
				float time = 0;
				var main = mParticleSystems[i].main;
				if (main.startLifetime.constantMax > 0)
					time = main.startLifetime.constantMax;
				else
					time = main.startLifetime.constant;
				if (mParticleSystems[i].emission.enabled)
				{
					lifeTimes.Add(time);
					if (main.loop)
						isLoop = true;
				}
				durations.Add(main.duration);
			}

			if (lifeTimes.Count > 0)
			{
				float maxLifeTime = lifeTimes[0];
				float maxDuration = durations[0];
				for (int i = 0; i < lifeTimes.Count; i++)
				{
					if (maxLifeTime < lifeTimes[i])
						maxLifeTime = lifeTimes[i];

					if (maxDuration < durations[i])
						maxDuration = durations[i];
				}

				lifeTime = maxLifeTime > maxDuration ? maxLifeTime : maxDuration;
			}
		}

#if UNITY_EDITOR
		[CanEditMultipleObjects]
		[CustomEditor(typeof(CFX_ParticleComponent))]
		public class CFX_ParticleComponentEditor : UnityEditor.Editor
		{
			private List<CFX_ParticleComponent> mTargets;
			private float mScaleDuration = 1;
			private float mScale = 1;

			private void OnEnable()
			{
				mTargets = new List<CFX_ParticleComponent>();
				for (int i = 0; i < targets.Length; i++)
				{
					var component = targets[i] as CFX_ParticleComponent;
					if (component != null)
						mTargets.Add(component);
				}
			}

			private ParticleSystem.MinMaxCurve Scale(float pScale, ParticleSystem.MinMaxCurve start)
			{
				start.constant = start.constant * pScale;
				start.constantMin = start.constantMin * pScale;
				start.constantMax = start.constantMax * pScale;

				var curveMin = start.curveMin;
				var curveMax = start.curveMax;
				var curve = start.curve;
				if (curve != null && curve.keys != null)
					for (int j = 0; j < curve.keys.Length; j++)
						curve.keys[j].value *= pScale;
				if (curveMin != null && curveMin.keys != null)
					for (int j = 0; j < curveMin.keys.Length; j++)
						curveMin.keys[j].value *= pScale;
				if (curveMax != null && curveMax.keys != null)
					for (int j = 0; j < curveMax.keys.Length; j++)
						curveMax.keys[j].value *= pScale;
				start.curveMin = curveMin;
				start.curveMax = curveMax;

				return start;
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorHelper.Separator();

				EditorHelper.BoxHorizontal(() =>
				{
					mScaleDuration = EditorHelper.FloatField(mScaleDuration, "Scale Duration", 120, 100);
					if (EditorHelper.Button("Scale Duration"))
					{
						foreach (var target in mTargets)
						{
							for (int i = 0; i < target.mParticleSystems.Length; i++)
							{
								var ps = target.mParticleSystems[i];
								var main = ps.main;
								var emission = ps.emission;
								var startLifeTime = main.startLifetime;
								var duration = main.duration;
								var constant = main.startLifetime.constant;
								var constantMin = startLifeTime.constantMin;
								var constantMax = startLifeTime.constantMax;
								//Ingore particle system which not work
								if (emission.enabled)
								{
									main.duration = duration * mScaleDuration;
									startLifeTime.constant = constant * mScaleDuration;
									startLifeTime.constant = constantMin * mScaleDuration;
									startLifeTime.constant = constantMax * mScaleDuration;
									main.startLifetime = startLifeTime;
								}
							}
							EditorUtility.SetDirty(target.gameObject);
						}
					}
				});
				EditorHelper.BoxHorizontal(() =>
				{
					mScale = EditorHelper.FloatField(mScale, "Scale Size", 120, 100);
					if (EditorHelper.Button("Scale Size"))
					{
						foreach (var target in mTargets)
						{
							for (int i = 0; i < target.mParticleSystems.Length; i++)
							{
								var ps = target.mParticleSystems[i];
								var emission = ps.emission;

								emission.rateOverDistance = Scale(mScale, emission.rateOverDistance);
								emission.rateOverTime = Scale(mScale, emission.rateOverTime);
								for (int b = 0; b < emission.burstCount; b++)
								{
									var burst = emission.GetBurst(b);
									burst.cycleCount = Mathf.RoundToInt(burst.cycleCount * mScale);
									burst.count = Scale(mScale, burst.count);
									emission.SetBurst(b, burst);
								}

								var main = ps.main;
								main.startSize = Scale(mScale, main.startSize);

								main.startSpeed = Scale(mScale, main.startSpeed);

								var sizeOverLifetime = ps.sizeOverLifetime;
								sizeOverLifetime.size = Scale(mScale, sizeOverLifetime.size);

								var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
								limitVelocityOverLifetime.limit = Scale(mScale, limitVelocityOverLifetime.limit);

								var sizeBySpeed = ps.sizeBySpeed;
								sizeBySpeed.size = Scale(mScale, sizeBySpeed.size);
							}
							EditorUtility.SetDirty(target.gameObject);
						}
					}
				});
				if (EditorHelper.Button("Async duration"))
				{
					foreach (var target in mTargets)
					{
						target.mParticleSystems = target.gameObject.FindComponentsInChildren<ParticleSystem>().ToArray();
						target.renderers = target.gameObject.FindComponentsInChildren<Renderer>().ToArray();
						for (int i = 0; i < target.mParticleSystems.Length; i++)
						{
							var ps = target.mParticleSystems[i];
							ps.Stop();
							var main = ps.main;
							float lifeTime = 0;
							if (main.startLifetime.constant > 0)
								lifeTime = ps.main.startLifetime.constant;
							if (main.startLifetime.constantMax > 0)
								lifeTime = ps.main.startLifetime.constantMax;
							main.duration = lifeTime;
						}
						EditorUtility.SetDirty(target.gameObject);
					}
					(target as CFX_ParticleComponent).Validate();
				}

				if (EditorHelper.Button("Validate"))
				{
					foreach (var target in mTargets)
					{
						target.renderers = null;
						target.mParticleSystems = null;
						target.Validate();
						EditorUtility.SetDirty(target.gameObject);
					}
				}
				if (mTargets.Count == 1)
				{
					if (mTargets[0].isLoop && EditorHelper.Button("Unloop"))
					{
						mTargets[0].Loop(false);
						EditorUtility.SetDirty(mTargets[0].gameObject);
					}
				}
				else
				{
					if (EditorHelper.Button("Unloop"))
						foreach (var target in mTargets)
						{
							target.Loop(false);
							EditorUtility.SetDirty(target.gameObject);
						}
				}

				if (mTargets.Count == 1)
				{
					if (!mTargets[0].isLoop && EditorHelper.Button("Loop"))
					{
						mTargets[0].Loop(true);
						EditorUtility.SetDirty(mTargets[0].gameObject);
					}
				}
				else
				{
					if (EditorHelper.Button("Loop"))
						foreach (var target in mTargets)
						{
							target.Loop(true);
							EditorUtility.SetDirty(target.gameObject);
						}
				}

				if (EditorHelper.Button("Create Root PS"))
				{
					foreach (var target in mTargets)
					{
						var ps = target.gameObject.GetComponent<ParticleSystem>();
						if (ps != null)
							continue;

						ps = target.gameObject.AddComponent<ParticleSystem>();
						var main = ps.main;
						var emission = ps.emission;
						var shape = ps.shape;
						main.loop = false;
						main.duration = 0;
						main.startLifetime = 0;
						main.maxParticles = 0;
						emission.enabled = false;
						shape.enabled = false;
						EditorUtility.SetDirty(target.gameObject);
					}
				}
			}
		}
#endif
	}
}