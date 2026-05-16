namespace RevCore
{
	/// <summary>
	/// Marker interface for messages flowing through <see cref="IEventBus"/>. Implementations are
	/// typically immutable <c>struct</c>s carrying the event payload. The marker constrains the
	/// generic methods of <see cref="IEventBus"/> so unrelated types cannot be published by accident.
	/// </summary>
	public interface IEvent { }
}
