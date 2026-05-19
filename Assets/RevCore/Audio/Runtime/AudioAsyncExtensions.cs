using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace RevCore
{
	/// <summary>
	/// UniTask-based awaitable extensions over <see cref="BaseAudioManager"/>. Each method bridges
	/// the existing callback-style API to <see cref="UniTask"/>; cancellation kills the in-flight
	/// fade by re-invoking <see cref="BaseAudioManager.SetMusicVolume"/> with the current volume
	/// and a zero fade duration, which triggers the DOTween tweener's <c>Kill</c> at the top of
	/// that method. Without the <c>DOTWEEN</c> define, cancellation marks the task cancelled but
	/// the underlying coroutine fade continues to run until it finishes naturally.
	/// </summary>
	public static class AudioAsyncExtensions
	{
		/// <summary>
		/// Awaitable equivalent of <see cref="BaseAudioManager.SetMusicVolume(float, float, Action)"/>.
		/// Returns when the fade completes; cancellation snap-stops the fade at the current volume.
		/// Snap-stop is reliable only when the <c>DOTWEEN</c> define is active; without it the
		/// underlying coroutine continues after cancellation.
		/// </summary>
		/// <param name="manager">The audio manager.</param>
		/// <param name="targetVolume">Final music volume.</param>
		/// <param name="duration">Fade duration in seconds. Zero or less snaps and returns immediately.</param>
		/// <param name="cancellationToken">Cancels the fade and returns the task as cancelled.</param>
		public static UniTask FadeMusicAsync(this BaseAudioManager manager, float targetVolume, float duration, CancellationToken cancellationToken = default)
		{
			if (manager == null)
				return UniTask.FromException(new ArgumentNullException(nameof(manager)));
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			var reg = default(CancellationTokenRegistration);
			bool completed = false;
			// Unity is single-threaded: synchronous completion sets completed=true before the guard below; async completion fires next frame, after reg is assigned.
			manager.SetMusicVolume(targetVolume, duration, () =>
			{
				completed = true;
				reg.Dispose();
				tcs.TrySetResult();
			});

			if (cancellationToken.CanBeCanceled && !completed)
			{
				reg = cancellationToken.Register(() =>
				{
					reg.Dispose();
					manager.SetMusicVolume(manager.MusicVolume, 0);
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}

		/// <summary>
		/// Awaitable fade-to-zero followed by <see cref="BaseAudioManager.StopMusic"/>. Returns when
		/// the fade completes and the music source has stopped. Cancellation snap-stops the fade at
		/// the current volume; the music source is NOT stopped on cancellation. Snap-stop is reliable
		/// only when the <c>DOTWEEN</c> define is active; without it the underlying coroutine
		/// continues after cancellation.
		/// </summary>
		/// <param name="manager">The audio manager.</param>
		/// <param name="duration">Fade-out duration in seconds.</param>
		/// <param name="cancellationToken">Cancels the fade. The music source is NOT stopped on cancellation.</param>
		public static UniTask FadeOutMusicAsync(this BaseAudioManager manager, float duration, CancellationToken cancellationToken = default)
		{
			if (manager == null)
				return UniTask.FromException(new ArgumentNullException(nameof(manager)));
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			var reg = default(CancellationTokenRegistration);
			bool completed = false;
			// Unity is single-threaded: synchronous completion sets completed=true before the guard below; async completion fires next frame, after reg is assigned.
			manager.StopMusic(duration, () =>
			{
				completed = true;
				reg.Dispose();
				tcs.TrySetResult();
			});

			if (cancellationToken.CanBeCanceled && !completed)
			{
				reg = cancellationToken.Register(() =>
				{
					reg.Dispose();
					manager.SetMusicVolume(manager.MusicVolume, 0);
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}
	}
}
