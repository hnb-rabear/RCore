using UnityEngine;

namespace RCore.Inspector
{
	public class AutoFillAttribute : PropertyAttribute
	{
		public string Path { get; private set; }

		public AutoFillAttribute(string path = "")
		{
			Path = path;
		}
	}
}