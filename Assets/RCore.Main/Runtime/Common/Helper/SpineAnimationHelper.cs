#if SPINE_ANIMATION

using System.Collections;
using UnityEngine;
using Spine;
using Spine.Unity;

namespace Utilities.Common
{
    public static class SpineAnimationHelper
    {
        #region Skeleton Animation

        public static void Play(this SkeletonAnimation skeleton, string animationToSet, bool isLoop,
            Spine.AnimationState.TrackEntryDelegate OnComplete = null,
            Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        {
            TrackEntry trackEntry;

            if (skeleton.AnimationName != animationToSet)
            {
                skeleton.AnimationName = animationToSet;
                skeleton.AnimationState.ClearTrack(0);
                trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);
            }
            else
            {
                trackEntry = skeleton.AnimationState.AddAnimation(0, animationToSet, isLoop, 0f);
            }

            if (OnEvent != null) trackEntry.Event += OnEvent;
            if (OnComplete != null) trackEntry.Complete += OnComplete;
        }

        public static void PlayFromStart(this SkeletonAnimation skeleton, string animationToSet, bool isLoop,
            Spine.AnimationState.TrackEntryDelegate OnComplete = null,
            Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        {
            TrackEntry trackEntry;

            if (skeleton.AnimationState != null)
            {
                if (skeleton.Skeleton == null)
                    skeleton.Initialize(true);

                bool hasAnim = false;
                foreach (var anim in skeleton.Skeleton.Data.Animations)
                {
                    if (anim.Name == animationToSet)
                        hasAnim = true;
                }
                if (!hasAnim)
                    return;

                skeleton.AnimationName = animationToSet;
                skeleton.AnimationState.ClearTrack(0);
                trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);

                if (OnEvent != null) trackEntry.Event += OnEvent;
                if (OnComplete != null) trackEntry.Complete += OnComplete;
            }
        }

        public static IEnumerator IEPlay(this SkeletonAnimation skeleton, string animationToSet, bool isLoop,
            Spine.AnimationState.TrackEntryDelegate OnComplete = null,
            Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        {
            TrackEntry trackEntry;

            if (skeleton.AnimationName != animationToSet)
            {
                skeleton.AnimationName = animationToSet;
                skeleton.AnimationState.ClearTrack(0);
                trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);
            }
            else
            {
                trackEntry = skeleton.AnimationState.AddAnimation(0, animationToSet, isLoop, 0);
            }

            if (OnEvent != null) trackEntry.Event += OnEvent;
            if (OnComplete != null) trackEntry.Complete += OnComplete;

            yield return new WaitForSeconds(trackEntry.AnimationTime);
        }

        #endregion

        //===============================================================

        #region Skeleton Graphic Animation

        public static void Play(this SkeletonGraphic skeleton, string animationToSet, bool isLoop,
            Spine.AnimationState.TrackEntryDelegate OnComplete = null,
            Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        {
            TrackEntry trackEntry;

            if (skeleton.AnimationState != null)
            {
                if (skeleton.Skeleton == null)
                    skeleton.Initialize(true);

                bool hasAnim = false;
                foreach (var anim in skeleton.Skeleton.Data.Animations)
                {
                    if (anim.Name == animationToSet)
                        hasAnim = true;
                }
                if (!hasAnim)
                    return;

                if (skeleton.startingAnimation != animationToSet)
                {
                    skeleton.startingAnimation = animationToSet;
                    skeleton.AnimationState.ClearTrack(0);
                    trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);
                }
                else
                {
                    trackEntry = skeleton.AnimationState.AddAnimation(0, animationToSet, isLoop, 0f);
                }

                if (OnEvent != null) trackEntry.Event += OnEvent;
                if (OnComplete != null) trackEntry.Complete += OnComplete;
            }
        }

        public static void PlayFromStart(this SkeletonGraphic skeleton, string animationToSet, bool isLoop,
            Spine.AnimationState.TrackEntryDelegate OnComplete = null,
            Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        {
            TrackEntry trackEntry;

            if (skeleton.AnimationState != null)
            {
                if (skeleton.Skeleton == null)
                    skeleton.Initialize(true);

                skeleton.startingAnimation = animationToSet;
                skeleton.AnimationState.ClearTrack(0);
                trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);

                if (OnEvent != null) trackEntry.Event += OnEvent;
                if (OnComplete != null) trackEntry.Complete += OnComplete;
            }
        }

        public static IEnumerator IEPlay(this SkeletonGraphic skeleton, string animationToSet, bool isLoop,
            Spine.AnimationState.TrackEntryDelegate OnComplete = null,
            Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        {
            TrackEntry trackEntry;

            if (skeleton.startingAnimation != animationToSet)
            {
                skeleton.startingAnimation = animationToSet;
                skeleton.AnimationState.ClearTrack(0);
                trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);
            }
            else
            {
                trackEntry = skeleton.AnimationState.AddAnimation(0, animationToSet, isLoop, 0);
            }

            if (OnEvent != null) trackEntry.Event += OnEvent;
            if (OnComplete != null) trackEntry.Complete += OnComplete;

            yield return new WaitForSeconds(trackEntry.AnimationTime);
        }

        public static float GetDuration(this SkeletonAnimation skeleton, string animation)
        {
            var animData = skeleton.skeletonDataAsset.GetSkeletonData(true).FindAnimation(animation);
            if (animData != null)
                return animData.Duration;

            return 0;
        }

        #endregion

        //================================================================

        //public static void Play(this SkeletonGraphicMultiObject skeleton, string animationToSet, bool isLoop,
        //    Spine.AnimationState.TrackEntryDelegate OnComplete = null,
        //    Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        //{
        //    TrackEntry trackEntry;

        //    if (skeleton.AnimationState != null)
        //    {
        //        if (skeleton.Skeleton == null)
        //            skeleton.Initialize(true);

        //        if (!skeleton.Skeleton.Data.ContainAnim(animationToSet))
        //        {
        //            Debug.Log("No animation " + animationToSet);
        //            return;
        //        }

        //        if (skeleton.startingAnimation != animationToSet)
        //        {
        //            skeleton.startingAnimation = animationToSet;
        //            skeleton.AnimationState.ClearTrack(0);
        //            trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);
        //        }
        //        else
        //        {
        //            trackEntry = skeleton.AnimationState.AddAnimation(0, animationToSet, isLoop, 0f);
        //        }

        //        if (OnEvent != null) trackEntry.Event += OnEvent;
        //        if (OnComplete != null) trackEntry.Complete += OnComplete;
        //    }
        //}

        //public static void PlayFromStart(this SkeletonGraphicMultiObject skeleton, string animationToSet, bool isLoop,
        //    Spine.AnimationState.TrackEntryDelegate OnComplete = null,
        //    Spine.AnimationState.TrackEntryEventDelegate OnEvent = null)
        //{
        //    TrackEntry trackEntry;

        //    if (skeleton.AnimationState != null)
        //    {
        //        if (skeleton.Skeleton == null)
        //            skeleton.Initialize(true);

        //        if (!skeleton.Skeleton.Data.ContainAnim(animationToSet))
        //        {
        //            Debug.Log("No animation " + animationToSet);
        //            return;
        //        }

        //        skeleton.startingAnimation = animationToSet;
        //        skeleton.AnimationState.ClearTrack(0);
        //        trackEntry = skeleton.AnimationState.SetAnimation(0, animationToSet, isLoop);

        //        if (OnEvent != null) trackEntry.Event += OnEvent;
        //        if (OnComplete != null) trackEntry.Complete += OnComplete;
        //    }
        //}

        public static bool ContainSkin(this SkeletonData skeletonData, string pSkinName)
        {
            foreach (var s in skeletonData.Skins)
                if (s.Name == pSkinName)
                    return true;
            return false;
        }

        public static bool ContainAnim(this SkeletonData skeletonData, string pAnim)
        {
            foreach (var anim in skeletonData.Animations)
                if (anim.Name == pAnim)
                    return true;
            return false;
        }
    }
}
#endif