#if ADDRESSABLES
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Editor.Tool
{
	[CreateAssetMenu(fileName = "AddressableAssetsGroupsColorizerSettings", menuName = "RCore/Tool/AddressableAssetsGroupsColorizerSettings")]
	public class AddressableAssetsGroupsColorizerSettings : ScriptableObject
	{
		[Serializable]
		public class GroupColorRule
		{
			public string prefix;
			public Color color;
			public string label; // Optional: description of what this group represents
		}

		public List<GroupColorRule> rules = new()
		{
			new GroupColorRule() { prefix = "In", color = Color.green, label = "InstallTime" },
			new GroupColorRule() { prefix = "Fa", color = Color.blue, label = "FastFollow" },
			new GroupColorRule() { prefix = "On", color = Color.cyan, label = "OnDemand" },
			new GroupColorRule() { prefix = "Ex", color = Color.red, label = "Excluded" },
		};

	}
}
#endif