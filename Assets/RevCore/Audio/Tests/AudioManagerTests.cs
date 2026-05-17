using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    public class AudioManagerTests
    {
        [Test]
        public void OnDestroy_DestroyedInstance_ClearsSingleton()
        {
            var go = new GameObject("AudioManager");
            var manager = go.AddComponent<AudioManager>();
            // EditMode Test Runner does not consistently fire MonoBehaviour lifecycle callbacks
            // (Awake / OnDestroy) on AddComponent + DestroyImmediate, so we invoke them by
            // reflection to exercise the production paths directly.
            typeof(AudioManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);

            Assert.AreSame(manager, AudioManager.Instance);

            typeof(AudioManager).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            UnityEngine.Object.DestroyImmediate(go);

            Assert.IsNull(AudioManager.Instance);
        }
    }
}
