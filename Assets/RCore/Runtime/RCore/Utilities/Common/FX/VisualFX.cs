/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

//#define USE_SPINE
//#define USE_DOTWEEN
#pragma warning disable 0649

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if USE_DOTWEEN
using DG.Tweening;
#endif

#if USE_SPINE
using Spine.Unity;
#endif

namespace RCore.Common
{
	public class VFXBase
	{
		public Action onHidden;

		internal GameObject container => m_Container;
		internal Transform transform => m_Container.transform;
		internal bool available { get; set; }

		[SerializeField]
		protected GameObject m_Container;
		protected Renderer[] m_Renderers;
		protected WaitForSeconds m_WaitForInactivate;
		protected Coroutine m_DeactivateCoroutine;
		protected float m_LifeTime;
		protected bool m_Initialized;
		protected bool m_Enable;

		public VFXBase(GameObject pObj)
		{
			m_Container = pObj;

			Initialize();
		}

		public bool enabled
		{
			get => m_Enable;
			set
			{
				m_Enable = value;

				m_Container.SetActive(value);
			}
		}

		protected virtual void Initialize() { }

		public virtual float GetLifeTime()
		{
			if (!m_Initialized)
				Initialize();

			return m_LifeTime;
		}

		public virtual void Play(bool pAutoInactive)
		{
			if (pAutoInactive)
				AutoInactive();
		}

		public virtual void Stop() { }

		public virtual void Clear() { }

		public void SetPosition(Vector3 pPosition)
		{
			m_Container.transform.position = pPosition;
		}

		public void AutoInactive(float pCustomTime = 0)
		{
			if (m_DeactivateCoroutine != null)
				CoroutineUtil.StopCoroutine(m_DeactivateCoroutine);

			m_DeactivateCoroutine = CoroutineUtil.StartCoroutine(pCustomTime != 0 ? IEAutoInactive(new WaitForSeconds(pCustomTime)) : IEAutoInactive(m_WaitForInactivate));
		}

		private IEnumerator IEAutoInactive(WaitForSeconds pTime)
		{
			yield return pTime;

			Clear();
			Stop();
			enabled = false;
			onHidden?.Invoke();
		}

		public virtual void SetSortingOrder(int pValue)
		{
			if (!m_Initialized)
				Initialize();

			if (m_Renderers != null)
				for (int i = 0; i < m_Renderers.Length; i++)
				{
					m_Renderers[i].sortingOrder = pValue;
				}
		}
	}

	//===============================================

	[Serializable]
	public class VFX_UAnimation : VFXBase
	{
		public Animator animator;

		private int m_TriggerValue;
		private int triggerValue
		{
			set
			{
				if (m_TriggerValue != value)
				{
					animator.SetInteger("trigger", value);

					var info = animator.GetCurrentAnimatorStateInfo(0);
					m_LifeTime = info.length;

					m_WaitForInactivate = new WaitForSeconds(m_LifeTime);
				}
			}
			get => m_TriggerValue;
		}

		public VFX_UAnimation(GameObject pObj) : base(pObj)
		{
		}

		protected override void Initialize()
		{
			if (m_Initialized)
				return;

			m_Initialized = true;

#if UNITY_2019_2_OR_NEWER
			m_Container.TryGetComponent(out animator);
#else
            if (animator == null)
                animator = mContainer.GetComponent<Animator>();
#endif
			if (animator != null)
			{
				available = true;

				var info = animator.GetCurrentAnimatorStateInfo(0);
				m_LifeTime = info.length;

				m_WaitForInactivate = new WaitForSeconds(m_LifeTime);
			}

			int count = 0;
#if UNITY_2019_2_OR_NEWER
			m_Container.TryGetComponent(out Renderer p);
#else
            var p = mContainer.GetComponent<Renderer>();
#endif
			if (p != null)
				count++;

			var childP = m_Container.GetComponentsInChildren<Renderer>();
			if (childP != null)
				count += childP.Length;

			if (count > 0)
			{
				m_Renderers = new Renderer[count];
				if (p != null)
					m_Renderers[0] = p;
				if (childP != null)
					for (int i = 1; i <= childP.Length; i++)
					{
						if (p != null)
							m_Renderers[i] = childP[i - 1];
						else
							m_Renderers[i - 1] = childP[i - 1];
					}
			}
		}

		public void Play(int pTriggerValue, bool pAutoInactive)
		{
			if (!m_Initialized)
				Initialize();

			if (animator != null)
			{
				m_Container.SetActive(true);
				animator.enabled = true;
				animator.Rebind();
				triggerValue = pTriggerValue;

				base.Play(pAutoInactive);
			}
		}

		public override void Play(bool pAutoInactive)
		{
			if (!m_Initialized)
				Initialize();

			if (animator == null)
				return;

			m_Container.SetActive(true);
			animator.enabled = true;
			animator.Rebind();

			base.Play(pAutoInactive);
		}

		public override void Stop()
		{
			enabled = false;
		}

		public override float GetLifeTime()
		{
			if (animator != null)
			{
				var info = animator.GetCurrentAnimatorStateInfo(0);
				m_LifeTime = info.length;
			}

			return m_LifeTime;
		}

		//[DEBUG]
		public int GetCurrentTriggerValue()
		{
			return animator.GetInteger("trigger");
		}
	}

	//===============================================

	[Serializable]
	public class VFX_Particle : VFXBase
	{
		public Action onFinishedMovement;
		public Action onFinishedMovementSeparately;

		private ParticleSystem[] m_ParticleSystems;

		//Setup controlable emits
		[SerializeField]
		private ParticleSystem m_ControllableParticle;
		private ParticleSystem.Particle[] m_Particles;
		private Vector3[] m_ParticlesPos;

		protected override void Initialize()
		{
			if (m_Initialized)
				return;

			m_Initialized = true;

			var lifeTimes = new List<float>();
			var durations = new List<float>();

			if (m_ParticleSystems == null)
			{
				m_ParticleSystems = m_Container.FindComponentsInChildren<ParticleSystem>().ToArray();
				m_Renderers = m_Container.FindComponentsInChildren<Renderer>().ToArray();

				for (int i = 0; i < m_ParticleSystems.Length; i++)
				{
					if (m_ParticleSystems[i].main.startLifetime.constantMax > 0)
						lifeTimes.Add(m_ParticleSystems[i].main.startLifetime.constantMax);
					else
						lifeTimes.Add(m_ParticleSystems[i].main.startLifetime.constant);
					durations.Add(m_ParticleSystems[i].main.duration);
				}
			}

			if (lifeTimes.Count > 0)
			{
				available = true;
				float maxLifeTime = lifeTimes[0];
				float maxDuration = durations[0];
				for (int i = 0; i < lifeTimes.Count; i++)
				{
					if (maxLifeTime < lifeTimes[i])
						maxLifeTime = lifeTimes[i];

					if (maxDuration < durations[i])
						maxDuration = durations[i];
				}

				m_LifeTime = maxLifeTime > maxDuration ? maxLifeTime : maxDuration;
			}

			m_WaitForInactivate = new WaitForSeconds(GetLifeTime());
		}

		public override void Play(bool pAutoInactive)
		{
			if (!m_Initialized)
				Initialize();

			if (m_ParticleSystems != null)
			{
				m_Container.SetActive(true);

				foreach (var p in m_ParticleSystems)
				{
					p.Clear();
					p.Play();

					var emission = p.emission;
					emission.enabled = true;
				}

				base.Play(pAutoInactive);
			}
		}

		public override void Stop()
		{
			if (!m_Initialized)
				Initialize();

			if (m_ParticleSystems != null)
			{
				foreach (var p in m_ParticleSystems)
				{
					p.Stop();
					var emission = p.emission;
					emission.enabled = false;
				}
			}
		}

		public override void Clear()
		{
			if (!m_Initialized)
				Initialize();

			if (m_ParticleSystems != null)
			{
				foreach (var p in m_ParticleSystems)
					p.Clear();
			}
		}

		public ParticleSystem[] GetParticleSystems()
		{
			return m_ParticleSystems;
		}

		private ParticleSystem.Particle[] GetEmits()
		{
			if (!m_Initialized)
				Initialize();

			m_Particles = new ParticleSystem.Particle[m_ControllableParticle.particleCount];
			return m_Particles;
		}

		private Vector3[] GetEmitsPosition()
		{
			if (!m_Initialized)
				Initialize();

			m_ParticlesPos = new Vector3[m_ControllableParticle.particleCount];
			int particlesCount = m_ControllableParticle.GetParticles(m_Particles);
			for (int i = 0; i < particlesCount; i++)
			{
				m_ParticlesPos[i] = m_Particles[i].position;
			}

			return m_ParticlesPos;
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

				for (int i = 0; i < m_Particles.Length; i++)
				{
					m_Particles[i].position = Vector3.Lerp(m_ParticlesPos[i], pDestination.position, rate);
				}

				m_ControllableParticle.SetParticles(m_Particles, m_Particles.Length);

				if (lerpTime == pDuration)
					break;

				yield return null;
			}

			onFinishedMovement?.Invoke();
		}

		public VFX_Particle(GameObject pObj) : base(pObj)
		{
		}

		public void StopMovement()
		{
			GetEmits();
			GetEmitsPosition();

			for (int i = 0; i < m_Particles.Length; i++)
			{
				m_Particles[i].velocity = Vector3.zero;
			}

			m_ControllableParticle.SetParticles(m_Particles, m_Particles.Length);
		}

#if USE_DOTWEEN
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
					for (int i = 0; i < m_Particles.Length; i++)
					{
						m_Particles[i].position = Vector3.Lerp(m_ParticlesPos[i], pDestination.position, perc);
					}

					m_ControllableParticle.SetParticles(m_Particles, m_Particles.Length);
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
				for (int i = 0; i < m_Particles.Length; i++)
					m_Particles[i].position = Vector3.Lerp(m_ParticlesPos[i], pDestination.position, lerp);

				m_ControllableParticle.SetParticles(m_Particles, m_Particles.Length);

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

			var mDelayEach = new WaitForSeconds(pDuration - pDuration / m_Particles.Length);

			for (int i = 0; i < m_Particles.Length; i++)
			{
				MoveEmit(pDestination, pDuration, i);

				yield return new WaitForSeconds(pDuration / m_Particles.Length);

				if (i == m_Particles.Length - 1)
				{
					yield return mDelayEach;
					process = false;
				}
			}

			yield return new WaitUntil(() => !process);

			onFinishedMovement?.Invoke();
		}

#if USE_DOTWEEN
		private void MoveEmit(Transform pDestination, float pDuration, int pIndex)
		{
			if (m_Particles.Length > pIndex)
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
							m_Particles[pIndex].position = Vector3.Lerp(m_ParticlesPos[pIndex], pDestination.position, perc);
							m_ControllableParticle.SetParticles(m_Particles, m_Particles.Length);
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
							m_Particles[pIndex].startColor = Color.clear;
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
			CoroutineUtil.StartCoroutine(IEMoveEmit(pDestination, pDuration, pIndex));
		}

		private IEnumerator IEMoveEmit(Transform pDestination, float pDuration, int pIndex)
		{
			if (m_Particles.Length > pIndex)
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
						m_Particles[pIndex].position = Vector3.Lerp(m_ParticlesPos[pIndex], pDestination.position, lerp);
						m_ControllableParticle.SetParticles(m_Particles, m_Particles.Length);
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
					m_Particles[pIndex].startColor = Color.clear;
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
			if (!m_Initialized)
				Initialize();

			for (int i = 0; i < m_ParticleSystems.Length; i++)
			{
				var main = m_ParticleSystems[i].main;
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
			for (int i = 0; i < m_ParticleSystems.Length; i++)
			{
				var main = m_ParticleSystems[i].main;
				var gradient = main.startColor.gradient;
				main.startColor = pGradient;
			}
		}

		public void SetGradient(Color[] pColors, float[] pAlphas)
		{
			if (!m_Initialized)
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

			for (int i = 0; i < m_ParticleSystems.Length; i++)
			{
				var main = m_ParticleSystems[i].main;

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

		public void SetColors(Color pColor1, Color pColor2, bool pOverrideAlpha = true)
		{
			if (!m_Initialized)
				Initialize();
			for (int i = 0; i < m_ParticleSystems.Length; i++)
			{
				var main = m_ParticleSystems[i].main;
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
	}

	//===============================================

#if USE_SPINE

    [System.Serializable]
    public class VFX_SpineAnimationFX : VFXBase
    {
        [SerializeField]
        [SpineAnimation]
        private string m_AnimationName;
        private SkeletonAnimation m_Animation;
        private Renderer[] m_Renderers;

        public CustomSpineAnimationFX(GameObject pObj) : base(pObj)
        {
        }

        protected override void initialize()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            if (m_Animation == null)
                m_Animation = mContainer.GetComponent<SkeletonAnimation>();
            if (m_Animation == null)
                m_Animation = mContainer.GetComponentInChildren<SkeletonAnimation>();

            if (m_Animation != null)
            {
                available = true;

                if (string.IsNullOrEmpty(m_AnimationName))
                    m_AnimationName = m_Animation.AnimationName;

                m_LifeTime = m_Animation.SkeletonDataAsset.GetSkeletonData(true).FindAnimation(m_AnimationName).duration;
                m_WaitForDeactive = new WaitForSeconds(getLifeTime());

                m_Renderers = m_Animation.GetComponents<Renderer>();
            }
        }

        public void setAnimation(string pAnimationName)
        {
            m_AnimationName = pAnimationName;
        }

        public override void play()
        {
            if (!m_Initialized)
                initialize();

            if (m_Animation != null)
            {
                mContainer.SetActive(true);

                m_Animation.AnimationName = m_AnimationName;
                m_LifeTime = m_Animation.SkeletonDataAsset.GetSkeletonData(true).FindAnimation(m_AnimationName).duration;

                if (m_AnimationName != m_Animation.AnimationName)
                    m_WaitForDeactive = new WaitForSeconds(getLifeTime());
            }
        }

        public override void stop()
        {
            if (!m_Initialized)
                initialize();

            if (m_Animation != null)
                m_Animation.AnimationName = "";
        }
    }

#endif

	//===============================================
}