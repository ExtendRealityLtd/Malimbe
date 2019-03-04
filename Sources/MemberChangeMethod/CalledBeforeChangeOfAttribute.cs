namespace Malimbe.MemberChangeMethod
{
    /// <summary>
    /// Indicates that the method is called before a data member is changed.
    /// </summary>
    public sealed class CalledBeforeChangeOfAttribute : HandlesMemberChangeAttribute
    {
        /// <summary>
        /// Indicates that the method is called before a data member is changed.
        /// </summary>
        /// <param name="memberName">The name of the data member that changes to will result in this method being called.</param>
        public CalledBeforeChangeOfAttribute(string memberName) : base(memberName)
        {
        }
    }
}
