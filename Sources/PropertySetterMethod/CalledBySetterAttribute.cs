namespace Malimbe.PropertySetterMethod
{
    using System;

    /// <summary>
    /// Indicates that the method is called inside the setter of a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CalledBySetterAttribute : Attribute
    {
        /// <summary>
        /// The name of the property this method will be called from.
        /// </summary>
        /// <remarks>
        /// The property needs to be declared in the same type this attribute is used in and needs both a getter and setter of any accessibility.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once NotAccessedField.Global
        public readonly string PropertyName;

        /// <summary>
        /// Indicates that the method is called inside the setter of a property.
        /// </summary>
        /// <param name="propertyName">The name of the property this method will be called from.</param>
        public CalledBySetterAttribute(string propertyName) =>
            PropertyName = propertyName;
    }
}
