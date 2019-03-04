namespace Malimbe.MemberChangeMethod
{
    /// <summary>
    /// Indicates that the method is called after a data member is changed.
    /// </summary>
    public sealed class CalledAfterChangeOfAttribute : HandlesMemberChangeAttribute
    {
        /// <summary>
        /// Indicates that the method is called after a data member is changed.
        /// </summary>
        /// <param name="memberName">The name of the data member that changes to will result in this method being called.</param>
        public CalledAfterChangeOfAttribute(string memberName) : base(memberName)
        {
        }
    }
}
