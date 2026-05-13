using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    public class AudioCollectionTests
    {
        private AudioCollection CreateCollection()
        {
            var col = ScriptableObject.CreateInstance<AudioCollection>();
            col.sfxClips = new AudioClip[3];
            col.musicClips = new AudioClip[2];
            for (int i = 0; i < col.sfxClips.Length; i++)
            {
                col.sfxClips[i] = AudioClip.Create($"sfx_{i}", 44100, 1, 44100, false);
                col.sfxClips[i].name = $"sfx_{i}";
            }
            for (int i = 0; i < col.musicClips.Length; i++)
            {
                col.musicClips[i] = AudioClip.Create($"music_{i}", 44100, 1, 44100, false);
                col.musicClips[i].name = $"music_{i}";
            }
            return col;
        }

        [Test]
        public void GetSFXClip_ByIndex_ReturnsCorrectClip()
        {
            var col = CreateCollection();
            Assert.AreEqual("sfx_1", col.GetSFXClip(1).name);
        }

        [Test]
        public void GetSFXClip_ByIndex_OutOfRange_ReturnsNull()
        {
            var col = CreateCollection();
            Assert.IsNull(col.GetSFXClip(999));
            Assert.IsNull(col.GetSFXClip(-1));
        }

        [Test]
        public void GetSFXClip_ByName_CaseInsensitive()
        {
            var col = CreateCollection();
            Assert.IsNotNull(col.GetSFXClip("SFX_0"));
            Assert.IsNotNull(col.GetSFXClip("sfx_0"));
        }

        [Test]
        public void GetSFXClip_ByName_ReturnsIndex()
        {
            var col = CreateCollection();
            col.GetSFXClip("sfx_2", out int idx);
            Assert.AreEqual(2, idx);
        }

        [Test]
        public void GetSFXClip_ByName_NotFound_ReturnsMinusOne()
        {
            var col = CreateCollection();
            col.GetSFXClip("nonexistent", out int idx);
            Assert.AreEqual(-1, idx);
        }

        [Test]
        public void GetMusicClip_ByIndex_ReturnsCorrectClip()
        {
            var col = CreateCollection();
            Assert.AreEqual("music_0", col.GetMusicClip(0).name);
        }

        [Test]
        public void GetMusicClip_ByName_Works()
        {
            var col = CreateCollection();
            Assert.IsNotNull(col.GetMusicClip("music_1"));
        }

        [Test]
        public void GetMusicClip_ByName_ReturnsIndex()
        {
            var col = CreateCollection();
            int idx = -1;
            col.GetMusicClip("music_1", ref idx);
            Assert.AreEqual(1, idx);
        }

        [Test]
        public void GetSFXNames_ReturnsAllNames()
        {
            var col = CreateCollection();
            var names = col.GetSFXNames();
            Assert.AreEqual(3, names.Length);
            Assert.AreEqual("sfx_0", names[0]);
            Assert.AreEqual("sfx_2", names[2]);
        }
    }
}
