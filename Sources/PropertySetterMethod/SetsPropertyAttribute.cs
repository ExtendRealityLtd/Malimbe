namespace Malimbe.PropertySetterMethod
{
    using System;

    /// <summary>
    /// Indicates that the method acts as a setter for a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SetsPropertyAttribute : Attribute
    {
        /// <summary>
        /// The name of the property this methods acts as a setter for.
        /// </summary>
        /// <remarks>
        /// The property needs to be declared in the same type this attribute is used in and needs both a getter and setter of any accessibility.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once NotAccessedField.Global
        public readonly string PropertyName;

        /// <summary>
        /// Indicates that the method acts as a setter for a property.
        /// </summary>
        /// <param name="propertyName">The name of the property this methods acts as a setter for.</param>
        public SetsPropertyAttribute(string propertyName) =>
            PropertyName = propertyName;
    }
}
