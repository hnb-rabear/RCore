using System;
using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// Conditionally shows a serialized field based on a sibling member's value. The condition
	/// member can be a <see cref="bool"/> field, property, or parameterless method on the same
	/// object; when it returns <c>true</c> the field is rendered, otherwise hidden.
	/// </summary>
	/// <remarks>
	/// Currently <see cref="AttributeUsage"/> sets <c>AllowMultiple = false</c>. Phase 4 will lift this
	/// to support AND/OR composition. When the condition member cannot be resolved, the field renders
	/// (fail-open) — a deliberate behavioral choice pinned by characterization tests.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class ShowIfAttribute : PropertyAttribute
	{
		/// <summary>Name of the bool field/property/method on the owning object that gates visibility.</summary>
		public string ConditionMemberName { get; private set; }

		/// <summary>Creates the attribute keyed on <paramref name="conditionMemberName"/>.</summary>
		public ShowIfAttribute(string conditionMemberName)
		{
			ConditionMemberName = conditionMemberName;
		}
	}
}
