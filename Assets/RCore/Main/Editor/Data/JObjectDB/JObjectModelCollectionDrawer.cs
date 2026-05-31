using System;
using RCore.Data.JObject;
using UnityEditor;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// Property drawer for <see cref="JObjectModelCollection"/> fields. Adds a "Create" button that
	/// instantiates a new collection asset directly from the inspector. See <see cref="JObjectCreateAssetDrawer"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(JObjectModelCollection), true)]
	public class JObjectModelCollectionDrawer : JObjectCreateAssetDrawer
	{
		protected override Type GetAssetType() => fieldInfo.FieldType;
	}
}
