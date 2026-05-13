using System;
using UnityEngine;

namespace RevCore
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class ShowIfAttribute : PropertyAttribute
	{
		public string ConditionMemberName { get; private set; }

		public ShowIfAttribute(string conditionMemberName)
		{
			ConditionMemberName = conditionMemberName;
		}
	}
}
