namespace RevCore
{
	public interface ITimerHandle
	{
		int Id { get; }
		bool IsRunning { get; }
		bool IsCompleted { get; }
		bool IsCancelled { get; }
		float Elapsed { get; }
		float Duration { get; }
		float Remaining { get; }
		void Cancel();
	}
}
