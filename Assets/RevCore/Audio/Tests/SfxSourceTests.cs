using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RevCore.Tests
{
    public class SfxSourceTests
    {
        private static void InvokeInit(SfxSource source)
        {
            typeof(SfxSource)
                .GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(source, null);
        }

        [Test]
        public void Init_NullClipArray_DoesNotThrow()
        {
            var go = new GameObject("SfxSource");
            try
            {
                var source = go.AddComponent<SfxSource>();
                source.mClips = null;

                LogAssert.Expect(LogType.Error, "AudioManager instance not found. SfxSource cannot function.");
                Assert.DoesNotThrow(() => InvokeInit(source));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlaySFX_NoAudioManager_DoesNotThrow()
        {
            var go = new GameObject("SfxSource");
            try
            {
                var source = go.AddComponent<SfxSource>();
                source.mClips = new[] { "click" };

                LogAssert.Expect(LogType.Error, "AudioManager instance not found. SfxSource cannot function.");
                LogAssert.Expect(LogType.Warning, "SfxSource is not initialized yet.");
                Assert.DoesNotThrow(() => source.PlaySFX());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
