using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RCore.Common;

namespace RCore.Inspector
{
    [CustomPropertyDrawer(typeof(StringStringDictionary))]
    [CustomPropertyDrawer(typeof(ObjectColorDictionary))]
    [CustomPropertyDrawer(typeof(ObjectObjectDictionary))]
    public class SerializableDictionaryDrawer : SerializableDictionaryPropertyDrawer { }
}