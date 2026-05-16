using System;

namespace RevCore
{
    /// <summary>
    /// Marks a field on a <see cref="JObjectModel{T}"/> as a dependency to be filled by
    /// <see cref="JObjectModelCollection.InjectDependencies"/>. The field's type is resolved
    /// against the collection's registered models and assigned by reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InjectAttribute : Attribute { }
}
