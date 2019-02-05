namespace Malimbe.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public static class CecilExtensions
    {
        public static TypeReference CreateGenericType(
            this TypeReference typeReference,
            params TypeReference[] genericArgumentTypes)
        {
            if (typeReference.GenericParameters.Count != genericArgumentTypes.Length)
            {
                throw new ArgumentException(
                    "The number of type arguments doesn't match the number of generic parameters.",
                    nameof(genericArgumentTypes));
            }

            GenericInstanceType genericInstanceType = new GenericInstanceType(typeReference);
            foreach (TypeReference genericArgumentType in genericArgumentTypes)
            {
                genericInstanceType.GenericArguments.Add(genericArgumentType);
            }

            return genericInstanceType;
        }

        public static bool IsSubclassOf(this TypeDefinition typeDefinition, TypeReference superTypeReference)
        {
            TypeDefinition baseTypeDefinition = typeDefinition.BaseType?.Resolve();
            while (baseTypeDefinition != null && baseTypeDefinition.FullName != superTypeReference.FullName)
            {
                baseTypeDefinition = baseTypeDefinition.BaseType?.Resolve();
            }

            return baseTypeDefinition?.FullName == superTypeReference.FullName;
        }

        public static MethodReference CreateGenericMethod(
            this MethodReference methodReference,
            params TypeReference[] genericArgumentTypes)
        {
            MethodReference genericMethodReference = new MethodReference(
                methodReference.Name,
                methodReference.ReturnType,
                methodReference.DeclaringType.CreateGenericType(genericArgumentTypes))
            {
                HasThis = methodReference.HasThis,
                ExplicitThis = methodReference.ExplicitThis,
                CallingConvention = methodReference.CallingConvention
            };

            foreach (ParameterDefinition parameterDefinition in methodReference.Parameters)
            {
                genericMethodReference.Parameters.Add(new ParameterDefinition(parameterDefinition.ParameterType));
            }

            foreach (GenericParameter genericParameter in methodReference.GenericParameters)
            {
                genericMethodReference.GenericParameters.Add(
                    new GenericParameter(genericParameter.Name, genericMethodReference));
            }

            return genericMethodReference;
        }

        public static MethodReference CreateGenericMethodIfNeeded(this MethodReference methodReference) =>
            methodReference.DeclaringType.HasGenericParameters
                ? methodReference.CreateGenericMethod(
                    methodReference.DeclaringType.GenericParameters.Select(parameter => parameter.GetElementType())
                        .ToArray())
                : methodReference;

        public static MethodReference FindBaseMethod(this MethodDefinition methodDefinition)
        {
            TypeReference baseTypeReference = methodDefinition.DeclaringType.BaseType;
            while (baseTypeReference != null)
            {
                TypeDefinition baseTypeDefinition = baseTypeReference.Resolve();
                MethodDefinition matchingMethodDefinition =
                    MetadataResolver.GetMethod(baseTypeDefinition.Methods, methodDefinition);
                if (matchingMethodDefinition != null)
                {
                    if (!baseTypeReference.IsGenericInstance)
                    {
                        return matchingMethodDefinition;
                    }

                    GenericInstanceType genericInstanceType = (GenericInstanceType)baseTypeReference;
                    return methodDefinition.Module.ImportReference(matchingMethodDefinition)
                        .CreateGenericMethod(genericInstanceType.GenericArguments.ToArray());
                }

                baseTypeReference = baseTypeDefinition.BaseType;
            }

            return methodDefinition;
        }

        public static FieldReference FindBackingField(this PropertyDefinition propertyDefinition)
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
