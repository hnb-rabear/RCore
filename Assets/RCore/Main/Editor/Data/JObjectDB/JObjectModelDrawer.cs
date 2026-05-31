using System;
using RCore.Data.JObject;
using UnityEditor;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// Property drawer for <see cref="JObjectModel{T}"/> fields. Adds a "Create" button that instantiates
	/// a new model asset directly from the inspector. See <see cref="JObjectCreateAssetDrawer"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(JObjectModel<>), true)]
	public class JObjectModelDrawer : JObjectCreateAssetDrawer
	{
		protected override Type GetAssetType()
		{
			// When the field is declared as the open generic, instantiate its type argument; otherwise the field type itself.
			return fieldInfo.FieldType.IsGenericType
				? fieldInfo.FieldType.GetGenericArguments()[0]
				: fieldInfo.FieldType;
		}
	}
}
