using System;
using RCore.Data.JObject;
using UnityEditor;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// Property drawer for <see cref="JObjectDataCollection"/> fields. Adds a "Create" button that
	/// instantiates a new collection asset directly from the inspector. See <see cref="JObjectCreateAssetDrawer"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(JObjectDataCollection), true)]
	public class JObjectDataCollectionDrawer : JObjectCreateAssetDrawer
	{
		protected override Type GetAssetType() => fieldInfo.FieldType;
	}
}
