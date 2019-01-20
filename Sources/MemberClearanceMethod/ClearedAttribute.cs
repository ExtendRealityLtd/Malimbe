namespace Malimbe.MemberClearanceMethod
{
    using System;

    /// <summary>
    /// Indicates that the member is cleared by a method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ClearedAttribute : Attribute
    {
    }
}
