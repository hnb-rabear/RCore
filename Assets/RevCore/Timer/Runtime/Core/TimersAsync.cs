using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace RevCore
{
	/// <inheritdoc cref="Timers"/>
	public static partial class Timers
	{
		/// <summary>
		/// Awaitable equivalent of <see cref="WaitForSeconds(float, Action, bool, int)"/>. Returns
		/// when the wall-clock delay has elapsed on the active <see cref="Scheduler"/>. Cancellation
		/// via <paramref name="cancellationToken"/> cancels the underlying timer handle.
		/// </summary>
		/// <param name="seconds">Delay in seconds. Non-positive completes synchronously.</param>
		/// <param name="unscaledTime">When <c>true</c>, advances by unscaled delta time.</param>
		/// <param name="cancellationToken">Cancels the wait and the underlying timer.</param>
		public static UniTask DelayAsync(float seconds, bool unscaledTime = false, CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			var handle = s_scheduler.WaitForSeconds(seconds, () => tcs.TrySetResult(), unscaledTime);

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					handle.Cancel();
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}

		/// <summary>
		/// Awaitable equivalent of <see cref="WaitForCondition(ConditionalDelegate, Action, int)"/>.
		/// Returns the first scheduler Tick on which <paramref name="predicate"/> evaluates to <c>true</c>.
		/// </summary>
		/// <param name="predicate">Polled on every Tick. Keep it cheap. Exceptions surface through the returned <see cref="UniTask"/>.</param>
		/// <param name="cancellationToken">Cancels the wait.</param>
		public static UniTask WaitForConditionAsync(Func<bool> predicate, CancellationToken cancellationToken = default)
		{
			if (predicate == null)
				return UniTask.FromException(new ArgumentNullException(nameof(predicate)));
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			ConditionalDelegate cd = () =>
			{
				try
				{
					return predicate();
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
					return true;
				}
			};

			var handle = s_scheduler.WaitForCondition(cd, () => tcs.TrySetResult());

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					handle.Cancel();
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}

		/// <summary>
		/// Awaitable that returns after <paramref name="frameCount"/> scheduler Ticks. One Tick equals
		/// one Update on the driver MonoBehaviour, so this is approximately one Unity frame per count.
		/// </summary>
		/// <param name="frameCount">Number of Ticks to wait. Non-positive returns synchronously.</param>
		/// <param name="cancellationToken">Cancels the wait.</param>
		public static UniTask WaitForFramesAsync(int frameCount, CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);
			if (frameCount <= 0)
				return UniTask.CompletedTask;

			var tcs = new UniTaskCompletionSource();
			int remaining = frameCount;
			ConditionalDelegate cd = () => --remaining <= 0;
			var handle = s_scheduler.WaitForCondition(cd, () => tcs.TrySetResult());

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					handle.Cancel();
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}
	}
}
