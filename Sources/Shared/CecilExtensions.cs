namespace Malimbe.CecilExtensions
{
    using Mono.Cecil;

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
    }
}
