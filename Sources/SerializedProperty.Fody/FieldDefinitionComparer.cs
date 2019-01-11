namespace Malimbe.SerializedProperty.Fody
{
    using System;
    using System.Collections.Generic;
    using Mono.Cecil;

    internal sealed class FieldDefinitionComparer : IEqualityComparer<FieldDefinition>
    {
        public static readonly FieldDefinitionComparer Instance = new FieldDefinitionComparer();

        public bool Equals(FieldDefinition x, FieldDefinition y) =>
            string.Equals(x?.FullName, y?.FullName, StringComparison.Ordinal);

        public int GetHashCode(FieldDefinition obj) =>
            obj.FullName.GetHashCode();
    }
}
