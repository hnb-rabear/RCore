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
            typeof(AudioManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);

            Assert.AreSame(manager, AudioManager.Instance);

            UnityEngine.Object.DestroyImmediate(go);

            Assert.IsNull(AudioManager.Instance);
        }
    }
}
