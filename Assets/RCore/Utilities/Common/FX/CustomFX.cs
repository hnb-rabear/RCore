/**
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

//#define USE_SPINE
//#define USE_LEANTWEEN
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
    public class CustomFX
    {
        public Action onHidden;

        internal GameObject container { get { return mContainer; } }
        internal Transform transform { get { return mContainer.transform; } }
        internal bool available { get; set; }

        [SerializeField]
        protected GameObject mContainer;
        protected Renderer[] mRenderers;
        protected WaitForSeconds mWaitForDeactive = null;
        protected Coroutine mDeactiveCoroutine;

        public CustomFX(GameObject pObj)
        {
            mContainer = pObj;

            Initialize();
        }

        protected float mLifeTime;
        protected bool mInitialized;

        private bool mEnable;
        public bool enabled
        {
            get { return mEnable; }
            set
            {
                mEnable = value;

                if (value)
                    mContainer.SetActive(true);
                else
                    mContainer.SetActive(false);
            }
        }

        protected virtual void Initialize() { }

        public virtual float GetLifeTime()
        {
            if (!mInitialized)
                Initialize();

            return mLifeTime;
        }

        public virtual void Play(bool pAutoDeactive)
        {
            if (pAutoDeactive)
                AutoDeactive();
        }

        public virtual void Stop() { }

        public virtual void Clear() { }

        public void SetPosition(Vector3 pPosition)
        {
            mContainer.transform.position = pPosition;
        }

        public void AutoDeactive(float pCustomTime = 0)
        {
            if (mDeactiveCoroutine != null)
                CoroutineUtil.StopCoroutine(mDeactiveCoroutine);

            if (pCustomTime != 0)
                mDeactiveCoroutine = CoroutineUtil.StartCoroutine(iEAutoDeactive(new WaitForSeconds(pCustomTime)));
            else
                mDeactiveCoroutine = CoroutineUtil.StartCoroutine(iEAutoDeactive(mWaitForDeactive));
        }

        private IEnumerator iEAutoDeactive(WaitForSeconds pTime)
        {
            yield return pTime;

            Clear();
            Stop();
            enabled = false;
            if (onHidden != null) onHidden();
        }

        public virtual void SetSortingOrder(int pValue)
        {
            if (!mInitialized)
                Initialize();

            if (mRenderers != null)
                for (int i = 0; i < mRenderers.Length; i++)
                {
                    mRenderers[i].sortingOrder = pValue;
                }
        }
    }

    //===============================================

    [System.Serializable]
    public class CFX_UAnimation : CustomFX
    {
        public Animator animator;

        private int mTriggerValue;
        private int triggerValue
        {
            set
            {
                if (mTriggerValue != value)
                {
                    animator.SetInteger("trigger", value);

                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    mLifeTime = info.length;

                    mWaitForDeactive = new WaitForSeconds(mLifeTime);
                }
            }
            get { return mTriggerValue; }
        }

        public CFX_UAnimation(GameObject pObj) : base(pObj)
        {
        }

        protected override void Initialize()
        {
            if (mInitialized)
                return;

            mInitialized = true;

#if UNITY_2019_2_OR_NEWER
            mContainer.TryGetComponent(out animator);
#else
            if (animator == null)
                animator = mContainer.GetComponent<Animator>();
#endif
            if (animator != null)
            {
                available = true;

                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                mLifeTime = info.length;

                mWaitForDeactive = new WaitForSeconds(mLifeTime);
            }

            int count = 0;
#if UNITY_2019_2_OR_NEWER
            mContainer.TryGetComponent(out Renderer p);
#else
            var p = mContainer.GetComponent<Renderer>();
#endif
            if (p != null)
                count++;

            var childP = mContainer.GetComponentsInChildren<Renderer>();
            if (childP != null)
                count += childP.Length;

            if (count > 0)
            {
                mRenderers = new Renderer[count];
                if (p != null)
                    mRenderers[0] = p;
                if (childP != null)
                    for (int i = 1; i <= childP.Length; i++)
                    {
                        if (p != null)
                            mRenderers[i] = childP[i - 1];
                        else
                            mRenderers[i - 1] = childP[i - 1];
                    }
            }
        }

        public void Play(int pTriggerValue, bool pAutoDeactive)
        {
            if (!mInitialized)
                Initialize();

            if (animator != null)
            {
                mContainer.SetActive(true);
                animator.enabled = true;
                animator.Rebind();
                triggerValue = pTriggerValue;

                base.Play(pAutoDeactive);
            }
        }

        public override void Play(bool pAutoDeactive)
        {
            if (!mInitialized)
                Initialize();

            if (animator != null)
            {
                mContainer.SetActive(true);
                animator.enabled = true;
                animator.Rebind();

                base.Play(pAutoDeactive);
            }
        }

        public override void Stop()
        {
            enabled = false;
        }

        public override float GetLifeTime()
        {
            if (animator != null)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                mLifeTime = info.length;
            }

            return mLifeTime;
        }

        //[DEBUG]
        public int GetCurrentTriggerValue()
        {
            return animator.GetInteger("trigger");
        }
    }

    //===============================================

    [System.Serializable]
    public class CFX_Particle : CustomFX
    {
        public Action onFinishedMovement;
        public Action onFinishedMovementSeparately;

        private ParticleSystem[] mParticleSystems;

        //Setup controlable emits
        [SerializeField]
        private ParticleSystem mControllableParticle;
        private ParticleSystem.Particle[] mParticles;
        private Vector3[] mParticlesPos;

        protected override void Initialize()
        {
            if (mInitialized)
                return;

            mInitialized = true;

            List<float> lifeTimes = new List<float>();
            List<float> durations = new List<float>();

            if (mParticleSystems == null)
            {
                mParticleSystems = mContainer.FindComponentsInChildren<ParticleSystem>().ToArray();
                mRenderers = mContainer.FindComponentsInChildren<Renderer>().ToArray();

                for (int i = 0; i < mParticleSystems.Length; i++)
                {
                    if (mParticleSystems[i].main.startLifetime.constantMax > 0)
                        lifeTimes.Add(mParticleSystems[i].main.startLifetime.constantMax);
                    else
                        lifeTimes.Add(mParticleSystems[i].main.startLifetime.constant);
                    durations.Add(mParticleSystems[i].main.duration);
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

                mLifeTime = maxLifeTime > maxDuration ? maxLifeTime : maxDuration;
            }

            mWaitForDeactive = new WaitForSeconds(GetLifeTime());
        }

        public override void Play(bool pAutoDeactive)
        {
            if (!mInitialized)
                Initialize();

            if (mParticleSystems != null)
            {
                mContainer.SetActive(true);

                foreach (var p in mParticleSystems)
                {
                    p.Clear();
                    p.Play();

                    var emission = p.emission;
                    emission.enabled = true;
                }

                base.Play(pAutoDeactive);
            }
        }

        public override void Stop()
        {
            if (!mInitialized)
                Initialize();

            if (mParticleSystems != null)
            {
                foreach (var p in mParticleSystems)
                {
                    p.Stop();
                    var emission = p.emission;
                    emission.enabled = false;
                }
            }
        }

        public override void Clear()
        {
            if (!mInitialized)
                Initialize();

            if (mParticleSystems != null)
            {
                foreach (var p in mParticleSystems)
                    p.Clear();
            }
        }

        public ParticleSystem[] GetParticleSystems()
        {
            return mParticleSystems;
        }

        private ParticleSystem.Particle[] GetEmits()
        {
            if (!mInitialized)
                Initialize();

            mParticles = new ParticleSystem.Particle[mControllableParticle.particleCount];
            return mParticles;
        }

        private Vector3[] GetEmitsPosition()
        {
            if (!mInitialized)
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

                float perc = lerpTime / pDuration;

                for (int i = 0; i < mParticles.Length; i++)
                {
                    mParticles[i].position = Vector3.Lerp(mParticlesPos[i], pDestination.position, perc);
                }

                mControllableParticle.SetParticles(mParticles, mParticles.Length);

                if (lerpTime == pDuration)
                    break;
                else
                    yield return null;
            }

            if (onFinishedMovement != null) onFinishedMovement();
        }

        public CFX_Particle(GameObject pObj) : base(pObj)
        {
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

#if USE_LEANTWEEN
        public IEnumerator IE_MoveEmitsEase(Transform pDestination, float pDuration, float pDelay = 0)
        {
            if (pDelay > 0)
                yield return new WaitForSeconds(pDelay);

            GetEmits();
            GetEmitsPosition();

            bool process = true;

            LeanTween.value(0, 1f, pDuration)
                .setEase(LeanTweenType.easeInCirc)
                .setOnUpdate((float perc) =>
                {
                    for (int i = 0; i < mParticles.Length; i++)
                    {
                        mParticles[i].position = Vector3.Lerp(mParticlesPos[i], pDestination.position, perc);
                    }

                    mControllableParticle.SetParticles(mParticles, mParticles.Length);
                })
                .setOnComplete(() =>
                {
                    process = false;
                });

            yield return new WaitUntil(() => !process);

            if (onFinishedMovement != null) onFinishedMovement();
        }
#elif USE_DOTWEEN
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

            if (onFinishedMovement != null) onFinishedMovement();
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

            if (onFinishedMovement != null) onFinishedMovement();
        }
#endif

        public IEnumerator IE_MoveEmitsSeparately(Transform pDestination, float pDuration, float pDelay = 0)
        {
            if (pDelay > 0)
                yield return new WaitForSeconds(pDelay);

            GetEmits();
            GetEmitsPosition();

            bool process = true;

            WaitForSeconds mDelayEach = new WaitForSeconds(pDuration - pDuration / mParticles.Length);

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

            if (onFinishedMovement != null) onFinishedMovement();
        }

#if USE_LEANTWEEN
        private void MoveEmit(Transform pDestination, float pDuration, int pIndex)
        {
            if (mParticles.Length > pIndex)
            {
                bool tweenBreaked = false;
                LeanTween.value(0, 1f, pDuration)
                    .setEase(LeanTweenType.easeInCirc)
                    .setOnUpdate((float perc) =>
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
                    .setOnComplete(() =>
                    {
                        try
                        {
                            mParticles[pIndex].startColor = Color.clear;
                            if (onFinishedMovementSeparately != null) onFinishedMovementSeparately();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex.ToString());
                        }
                    });
            }
        }
#elif USE_DOTWEEN
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
                            if (onFinishedMovementSeparately != null) onFinishedMovementSeparately();
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
                    if (onFinishedMovementSeparately != null) onFinishedMovementSeparately();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }
        }
#endif

        public void SetColor(Color pColor, bool pOverideAlpha = true)
        {
            if (!mInitialized)
                Initialize();

            for (int i = 0; i < mParticleSystems.Length; i++)
            {
                var main = mParticleSystems[i].main;
                if (pOverideAlpha)
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

        public void SetGradient(Color[] pClolors, float[] pAlphas)
        {
            if (!mInitialized)
                Initialize();

            var colorKeysList = new List<GradientColorKey>();
            var alphaKeysList = new List<GradientAlphaKey>();

            float keyPosition = 0;
            for (int i = 0; i < pClolors.Length; i++)
            {
                colorKeysList.Add(new GradientColorKey(pClolors[i], keyPosition));
                if (pAlphas.Length > i)
                    alphaKeysList.Add(new GradientAlphaKey(pClolors[i].a, keyPosition));
                else
                    alphaKeysList.Add(new GradientAlphaKey(pAlphas[i], keyPosition));
                keyPosition += 1f / (pClolors.Length - 1);
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

        public void SetColors(Color pColor1, Color pColor2, bool pOverideAlpha = true)
        {
            if (!mInitialized)
                Initialize();
            for (int i = 0; i < mParticleSystems.Length; i++)
            {
                var main = mParticleSystems[i].main;
                var startColor = main.startColor;
                var color2 = main.startColor;
                var pColor0 = pColor1;
                if (!pOverideAlpha)
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
    public class CFX_SpineAnimationFX : CustomFX
    {
        [SerializeField]
        [SpineAnimation]
        private string mAnimationName;
        private SkeletonAnimation mAnimation;
        private Renderer[] mRenderers;

        public CustomSpineAnimationFX(GameObject pObj) : base(pObj)
        {
        }

        protected override void initialize()
        {
            if (mInitialized)
                return;

            mInitialized = true;

            if (mAnimation == null)
                mAnimation = mContainer.GetComponent<SkeletonAnimation>();
            if (mAnimation == null)
                mAnimation = mContainer.GetComponentInChildren<SkeletonAnimation>();

            if (mAnimation != null)
            {
                available = true;

                if (string.IsNullOrEmpty(mAnimationName))
                    mAnimationName = mAnimation.AnimationName;

                mLifeTime = mAnimation.SkeletonDataAsset.GetSkeletonData(true).FindAnimation(mAnimationName).duration;
                mWaitForDeactive = new WaitForSeconds(getLifeTime());

                mRenderers = mAnimation.GetComponents<Renderer>();
            }
        }

        public void setAnimation(string pAnimationName)
        {
            mAnimationName = pAnimationName;
        }

        public override void play()
        {
            if (!mInitialized)
                initialize();

            if (mAnimation != null)
            {
                mContainer.SetActive(true);

                mAnimation.AnimationName = mAnimationName;
                mLifeTime = mAnimation.SkeletonDataAsset.GetSkeletonData(true).FindAnimation(mAnimationName).duration;

                if (mAnimationName != mAnimation.AnimationName)
                    mWaitForDeactive = new WaitForSeconds(getLifeTime());
            }
        }

        public override void stop()
        {
            if (!mInitialized)
                initialize();

            if (mAnimation != null)
                mAnimation.AnimationName = "";
        }
    }

#endif

    //===============================================
}