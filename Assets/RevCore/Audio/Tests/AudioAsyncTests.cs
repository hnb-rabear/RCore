using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
	public class AudioAsyncTests
	{
		private sealed class TestAudioManager : BaseAudioManager
		{
			public void InvokeStartForTest()
			{
				Start();
			}
		}

		private GameObject m_go;
		private TestAudioManager m_manager;

		[SetUp]
		public void SetUp()
		{
			m_go = new GameObject("AudioAsyncTests_Manager");
			m_manager = m_go.AddComponent<TestAudioManager>();
			m_manager.InvokeStartForTest();
		}

		[TearDown]
		public void TearDown()
		{
			if (m_go != null)
				Object.DestroyImmediate(m_go);
		}

		[Test]
		public async UniTaskVoid FadeMusicAsync_zero_duration_completes_immediately()
		{
			var task = m_manager.FadeMusicAsync(0.5f, 0);
			await task;
			Assert.AreEqual(0.5f, m_manager.MusicVolume, 0.001f);
		}

		[Test]
		public void FadeMusicAsync_null_manager_returns_faulted()
		{
			BaseAudioManager nullManager = null;
			var task = nullManager.FadeMusicAsync(0.5f, 1f);
			Assert.AreEqual(UniTaskStatus.Faulted, task.Status);
		}

		[Test]
		public void FadeMusicAsync_pre_cancelled_token_returns_cancelled()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			var task = m_manager.FadeMusicAsync(0.5f, 1f, cts.Token);
			Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
		}

		[Test]
		public async UniTaskVoid FadeOutMusicAsync_zero_duration_stops_music_immediately()
		{
			m_manager.SetMusicVolume(0.8f, 0);
			var task = m_manager.FadeOutMusicAsync(0);
			await task;
			Assert.AreEqual(0f, m_manager.MusicVolume, 0.001f);
		}

		[Test]
		public void FadeOutMusicAsync_null_manager_returns_faulted()
		{
			BaseAudioManager nullManager = null;
			var task = nullManager.FadeOutMusicAsync(1f);
			Assert.AreEqual(UniTaskStatus.Faulted, task.Status);
		}

		[Test]
		public void FadeOutMusicAsync_pre_cancelled_token_returns_cancelled()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			var task = m_manager.FadeOutMusicAsync(1f, cts.Token);
			Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
		}
	}
}
