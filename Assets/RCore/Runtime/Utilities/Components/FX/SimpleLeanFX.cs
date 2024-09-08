/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

//#define USE_DOTWEEN

using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if USE_DOTWEEN
using DG.Tweening;
#endif

namespace RCore.Components
{
    /// <summary>
    /// Just and example of some simple fx
    /// </summary>
    public class SimpleLeanFX : MonoBehaviour
    {
        private static SimpleLeanFX m_Instance;
        public static SimpleLeanFX Instance => m_Instance;

        [SerializeField] private AnimationCurve mBubbleAnim;
        [SerializeField] private AnimationCurve mFadeInAndOutAnim;
        [SerializeField] private AnimationCurve mShakeAnim;

        public Image imgTest;
        public Transform transformTest;

        private void Start()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
        }

        public int Bubble(Transform pTarget, Vector3 defaultScale, float time = 0.5f, Action pOnFinished = null)
        {
            return SimulateBubble(pTarget, defaultScale, 0, 1, Instance.mBubbleAnim, time, pOnFinished);
        }

        public int SingleHightLight(Image pTarget, float time = 0.5f, Action pOnFinished = null)
        {
            var defaultAlpha = Color.white.a;
            int id = 0;
#if USE_DOTWEEN
            float val = 0;
            DOTween.Kill(pTarget.GetInstanceID() + GetInstanceID());
            //var tween = DOTween.To(() => val, x => val = x, 1f, time)
            var tween = DOTween.To(tweenVal => val = tweenVal, 0f, 1f, time)
                .OnUpdate(() =>
                {
                    float curve = Instance.mFadeInAndOutAnim.Evaluate(val);
                    var color = pTarget.color;
                    color.a = defaultAlpha * curve;
                    pTarget.color = color;
                })
                .OnComplete(() =>
				{
					pOnFinished?.Invoke();
				})
                .SetId(pTarget.GetInstanceID() + GetInstanceID()).SetUpdate(true);
            id = tween.intId;
#endif
            return id;
        }

        private int SimulateBubble(Component pTarget, Vector3 defaultScale, float pFrom, float pTo, AnimationCurve pAnim, float time, Action pOnFinished)
        {
            int id = 0;
#if USE_DOTWEEN
            float val = 0;
            DOTween.Kill(pTarget.GetInstanceID() + GetInstanceID());
            //var tween = DOTween.To(() => val, x => val = x, 1f, time)
            var tween = DOTween.To(tweenVal => val = tweenVal, 0f, 1f, time)
                .OnUpdate(() =>
                {
                    float curve = pAnim.Evaluate(val);

                    pTarget.transform.localScale = defaultScale * curve;
                })
                .OnComplete(() =>
                {
                    pTarget.transform.localScale = defaultScale;

					pOnFinished?.Invoke();
				})
                .SetId(pTarget.GetInstanceID() + GetInstanceID()).SetUpdate(true);
            id = tween.intId;
#endif
            return id;
        }

        public void Shake(Transform pTarget, float pTime, float pIntensity, Action pOnFinished = null)
        {
            var defaultPos = pTarget.position;
            var defaultScale = pTarget.localScale;
            var defaultRotaion = pTarget.rotation;

#if USE_DOTWEEN
            float val = 0;
            DOTween.Kill(pTarget.GetInstanceID() + GetInstanceID());
            //var tween = DOTween.To(() => val, x => val = x, 1f, time)
            var tween = DOTween.To(tweenVal => val = tweenVal, 0f, 1f, pTime)
                .OnUpdate(() =>
                {
                    float curve = Instance.mShakeAnim.Evaluate(val);
                    float intensity = pIntensity * curve;

                    var shakingPos = defaultPos + Random.insideUnitSphere * intensity;
                    pTarget.position = shakingPos;

                    var z = defaultRotaion.z + Random.Range(-intensity, intensity);
                    var w = defaultRotaion.w + Random.Range(-intensity, intensity);
                    pTarget.rotation = new Quaternion(0, 0, z, w);
                })
                .OnComplete(() =>
                {
                    pTarget.position = defaultPos;
                    pTarget.localScale = defaultScale;
                    pTarget.rotation = defaultRotaion;

					pOnFinished?.Invoke();
				})
                .SetId(pTarget.GetInstanceID() + GetInstanceID()).SetUpdate(true);
#endif
        }

        public void FadeIn(Image pImage, float pTime, Action pOnFinished = null)
        {
            var color = pImage.color;
#if USE_DOTWEEN
            color.a = 0f;
            pImage.color = color;
            pImage.DOFade(1f, pTime)
                .OnComplete(() =>
				{
					pOnFinished?.Invoke();
				})
                .SetId(pImage.GetInstanceID() + GetInstanceID()).SetUpdate(true);
#endif
        }

        public void FadeOut(Image pImage, float pTime, Action pOnFinished = null)
        {
            var color = pImage.color;
#if USE_DOTWEEN
            color.a = 1f;
            pImage.color = color;
            pImage.DOFade(0f, pTime)
                .OnComplete(() =>
				{
					pOnFinished?.Invoke();
				})
                .SetId(pImage.GetInstanceID() + GetInstanceID()).SetUpdate(true);
#endif
        }

        public void CreateAnimationCurves()
        {
            mBubbleAnim = new AnimationCurve();
            mBubbleAnim.keys = new Keyframe[] {
                new Keyframe(0, 1f),
                new Keyframe(0.25f, 1.1f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.9f),
                new Keyframe(1f, 1f),
            };

            mFadeInAndOutAnim = new AnimationCurve();
            mFadeInAndOutAnim.keys = new Keyframe[] {
                new Keyframe(0, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1, 0f)
            };

            mShakeAnim = new AnimationCurve();
            mShakeAnim.keys = new Keyframe[] {
                new Keyframe(0, 1f),
                new Keyframe(0.25f, 1.1f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.9f),
                new Keyframe(1f, 1f),
            };
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(SimpleLeanFX), true)]
    public class SimpleLeanFXEditor : UnityEditor.Editor
    {
        private SimpleLeanFX mObj;

        private void OnEnable()
        {
            mObj = (SimpleLeanFX)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Bubble"))
                mObj.Bubble(mObj.transformTest.transform, Vector3.one);

            if (GUILayout.Button("SingleHightLight"))
                mObj.SingleHightLight(mObj.imgTest);

            if (GUILayout.Button("Shake"))
                mObj.Shake(mObj.transformTest.transform, 1f, 0.1f);

            if (GUILayout.Button("FadeIn"))
                mObj.FadeIn(mObj.imgTest, 2f);

            if (GUILayout.Button("FadeOut"))
                mObj.FadeOut(mObj.imgTest, 2f);

            if (GUILayout.Button("CreateAnimationCurves"))
                mObj.CreateAnimationCurves();
        }
    }

#endif
}