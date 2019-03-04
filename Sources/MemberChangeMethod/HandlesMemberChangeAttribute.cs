namespace Malimbe.MemberChangeMethod
{
    using System;

    /// <summary>
    /// Indicates that the method is called in response to the change of a data member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class HandlesMemberChangeAttribute : Attribute
    {
        /// <summary>
        /// The name of the data member that changes to will result in this method being called.
        /// </summary>
        /// <remarks>
        /// The data member needs to be declared in the same type this attribute is used in.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once NotAccessedField.Global
        public readonly string DataMemberName;

        /// <summary>
        /// Indicates that the method is called in response to the change of a data member.
        /// </summary>
        /// <param name="memberName">The name of the data member that changes to will result in this method being called.</param>
        public HandlesMemberChangeAttribute(string memberName) =>
            DataMemberName = memberName;
    }
}
