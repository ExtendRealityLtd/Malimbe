namespace Malimbe.SerializedProperty
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SerializedPropertyAttribute : Attribute
    {
        public readonly bool IsFieldVisibleInInspector;

        public SerializedPropertyAttribute(bool isFieldVisibleInInspector = true) =>
            IsFieldVisibleInInspector = isFieldVisibleInInspector;
    }
}
