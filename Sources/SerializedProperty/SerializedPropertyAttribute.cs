namespace Malimbe.SerializedProperty
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SerializedPropertyAttribute : Attribute
    {
        public readonly bool HidesFieldInInspector;

        public SerializedPropertyAttribute(bool hideFieldInInspector = false) =>
            HidesFieldInInspector = hideFieldInInspector;
    }
}
