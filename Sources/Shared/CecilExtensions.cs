namespace Malimbe.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public static class CecilExtensions
    {
        public static MethodReference GetGeneric(this MethodReference reference)
        {
            if (!reference.DeclaringType.HasGenericParameters)
            {
                return reference;
            }

            GenericInstanceType declaringType = new GenericInstanceType(reference.DeclaringType);
            foreach (GenericParameter parameter in reference.DeclaringType.GenericParameters)
            {
                declaringType.GenericArguments.Add(parameter);
            }

            MethodReference methodReference = new MethodReference(
                reference.Name,
                reference.MethodReturnType.ReturnType,
                declaringType);
            foreach (ParameterDefinition parameterDefinition in reference.Parameters)
            {
                methodReference.Parameters.Add(parameterDefinition);
            }

            methodReference.HasThis = reference.HasThis;
            return methodReference;
        }

        public static FieldReference GetBackingField(this PropertyDefinition propertyDefinition)
        {
            IEnumerable<FieldReference> getFieldReferences = propertyDefinition.GetMethod?.Body?.Instructions
                ?.Where(instruction => instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld)
                .Select(instruction => (FieldReference)instruction.Operand);
            IEnumerable<FieldReference> setFieldReferences = propertyDefinition.SetMethod?.Body?.Instructions
                ?.Where(instruction => instruction.OpCode == OpCodes.Stsfld || instruction.OpCode == OpCodes.Stfld)
                .Select(instruction => (FieldReference)instruction.Operand);

            return getFieldReferences?.Intersect(
                    setFieldReferences ?? Enumerable.Empty<FieldReference>(),
                    FieldReferenceComparer.Instance)
                .FirstOrDefault();
        }
    }
}
