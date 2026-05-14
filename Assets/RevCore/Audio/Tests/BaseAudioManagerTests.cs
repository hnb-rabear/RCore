using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    public class BaseAudioManagerTests
    {
        private sealed class TestAudioManager : BaseAudioManager
        {
            public void InvokeStartForTest()
            {
                Start();
            }
        }

        [Test]
        public void Start_RuntimeAddedManager_CreatesSourcesWithoutThrowing()
        {
            var go = new GameObject("AudioManager");
            try
            {
                var manager = go.AddComponent<TestAudioManager>();

                Assert.DoesNotThrow(manager.InvokeStartForTest);
                Assert.DoesNotThrow(() => manager.SetMasterVolume(0.5f));
                Assert.DoesNotThrow(() => manager.EnableMusic(false));
                Assert.DoesNotThrow(() => manager.EnableSFX(false));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectionBasedPlayback_MissingCollection_DoesNotThrow()
        {
            var go = new GameObject("AudioManager");
            try
            {
                var manager = go.AddComponent<TestAudioManager>();
                manager.InvokeStartForTest();

                Assert.DoesNotThrow(() => manager.PlayMusic("missing"));
                Assert.DoesNotThrow(() => manager.PlayMusicById(0));
                Assert.DoesNotThrow(() => manager.PlayMusicByIds(new[] { 0, 1 }));
                Assert.DoesNotThrow(() => manager.PlaySFX("missing"));
                Assert.DoesNotThrow(() => manager.PlaySFX(0));
                Assert.DoesNotThrow(() => manager.StopSFX(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StopMethods_RuntimeAddedManager_DoNotThrow()
        {
            var go = new GameObject("AudioManager");
            try
            {
                var manager = go.AddComponent<TestAudioManager>();
                manager.InvokeStartForTest();

                Assert.DoesNotThrow(() => manager.StopMusic());
                Assert.DoesNotThrow(() => manager.StopSFXs());
                Assert.DoesNotThrow(() => manager.StopSFX((AudioClip)null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
