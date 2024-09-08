using System;
using UnityEngine;

namespace RCore.Common
{
#if UNITY_EDITOR
    [Serializable]
#endif
    public class FPSCounter : IUpdate
    {
        public int fps;

        private float mTimeEslap;
        private int mCountFrame;

        public int id { get; set; }
        public bool updated { get; set; }

        public void Update(float pUnscaledDeltaTime)
        {
            updated = false;
            mTimeEslap += pUnscaledDeltaTime;
            mCountFrame++;

            if (mTimeEslap >= 1)
            {
                fps = Mathf.RoundToInt(mCountFrame * 1f / mTimeEslap);

                mTimeEslap = 0;
                mCountFrame = 0;
                updated = true;
            }
        }
    }
}
