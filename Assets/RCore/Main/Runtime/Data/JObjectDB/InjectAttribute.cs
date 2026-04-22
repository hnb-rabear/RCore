/**
 * Author HNB-RaBear - 2024
 **/

using System;

namespace RCore.Data.JObject
{
	/// <summary>
	/// Marks a field for automatic dependency injection by <see cref="JObjectModelCollection"/>.
	/// At runtime during <c>InjectDependencies()</c>, the field will be automatically resolved
	/// from registered models or their underlying data objects.
	/// Supports interfaces and concrete types, enabling models to depend on abstractions
	/// rather than concrete model references.
	/// </summary>
	/// <example>
	/// <code>
	/// public class RewardModel : JObjectModel&lt;RewardData&gt;
	/// {
	///     [Inject] private IPlayerModelInventory m_playerInventory;
	///     [Inject] private IPlayerModelProgress m_playerProgress;
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Field)]
	public class InjectAttribute : Attribute { }
}
