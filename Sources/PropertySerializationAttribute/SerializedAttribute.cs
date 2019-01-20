namespace Malimbe.PropertySerializationAttribute
{
    using System;

    /// <summary>
    /// Indicates that the property's backing field is serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SerializedAttribute : Attribute
    {
    }
}
