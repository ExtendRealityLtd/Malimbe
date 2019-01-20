namespace Malimbe.PropertyValidationMethod
{
    using System;

    /// <summary>
    /// Indicates that changes to the property are validated by a method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ValidatedAttribute : Attribute
    {
    }
}
