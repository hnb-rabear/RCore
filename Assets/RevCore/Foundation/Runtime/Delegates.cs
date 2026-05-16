namespace RevCore
{
	/// <summary>Parameterless action delegate. Equivalent to <see cref="System.Action"/>; kept for legacy serialized references.</summary>
	public delegate void VoidDelegate();

	/// <summary>Action delegate taking a single <see cref="int"/>.</summary>
	public delegate void IntDelegate(int value);

	/// <summary>Action delegate taking a single <see cref="bool"/>.</summary>
	public delegate void BoolDelegate(bool value);

	/// <summary>Action delegate taking a single <see cref="float"/>.</summary>
	public delegate void FloatDelegate(float value);

	/// <summary>Predicate-style delegate returning <c>true</c> when a condition is met. Used by <see cref="TimerScheduler.WaitForCondition"/>.</summary>
	public delegate bool ConditionalDelegate();
}
