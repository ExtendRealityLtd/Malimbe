namespace Malimbe.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public static class CecilExtensions
    {
        public static MethodReference GetGeneric(this MethodReference methodReference)
        {
            if (!methodReference.DeclaringType.HasGenericParameters)
            {
                return methodReference;
            }

            GenericInstanceType declaringType = new GenericInstanceType(methodReference.DeclaringType);
            foreach (GenericParameter parameter in methodReference.DeclaringType.GenericParameters)
            {
                declaringType.GenericArguments.Add(parameter);
            }

            MethodReference genericMethodReference = new MethodReference(
                methodReference.Name,
                methodReference.MethodReturnType.ReturnType,
                declaringType);
            foreach (ParameterDefinition parameterDefinition in methodReference.Parameters)
            {
                genericMethodReference.Parameters.Add(parameterDefinition);
            }

            genericMethodReference.HasThis = methodReference.HasThis;
            return genericMethodReference;
        }

        public static MethodDefinition GetBaseMethod(this MethodDefinition methodDefinition)
        {
            TypeDefinition baseTypeDefinition = methodDefinition.DeclaringType.BaseType?.Resolve();
            while (baseTypeDefinition != null)
            {
                MethodDefinition matchingMethodDefinition =
                    MetadataResolver.GetMethod(baseTypeDefinition.Methods, methodDefinition);
                if (matchingMethodDefinition != null)
                {
                    return matchingMethodDefinition;
                }

                baseTypeDefinition = baseTypeDefinition.BaseType?.Resolve();
            }

            return methodDefinition;
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
