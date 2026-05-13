using UnityEngine;

namespace RevCore
{
	public sealed class AutoFillAttribute : PropertyAttribute
	{
		public string Path { get; private set; }

		public AutoFillAttribute(string path = "")
		{
			Path = path;
		}
	}
}
