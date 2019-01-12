namespace Malimbe.Shared
{
    using System;
    using System.Collections.Generic;
    using Mono.Cecil;

    public sealed class FieldReferenceComparer : IEqualityComparer<FieldReference>
    {
        public static readonly FieldReferenceComparer Instance = new FieldReferenceComparer();

        public bool Equals(FieldReference x, FieldReference y) =>
            string.Equals(x?.FullName, y?.FullName, StringComparison.Ordinal);

        public int GetHashCode(FieldReference obj) =>
            obj.FullName.GetHashCode();
    }
}
