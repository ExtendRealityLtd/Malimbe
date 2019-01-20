namespace Malimbe.PropertySetterMethod.Fody
{
    using System.Collections.Generic;
    using System.Linq;
    using global::Fody;
    using Malimbe.Shared;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;

    // ReSharper disable once UnusedMember.Global
    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private static readonly string _fullAttributeName = typeof(SetsPropertyAttribute).FullName;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            IEnumerable<MethodDefinition> methodDefinitions =
                ModuleDefinition.Types.SelectMany(definition => definition.Methods);
            foreach (MethodDefinition methodDefinition in methodDefinitions)
            {
                if (!FindAndRemoveAttribute(methodDefinition, out CustomAttribute attribute)
                    || !FindProperty(methodDefinition, attribute, out PropertyDefinition propertyDefinition))
                {
                    continue;
                }

                if (propertyDefinition.GetMethod == null)
                {
                    LogError(
                        $"The method '{methodDefinition.FullName}' is annotated to be a setter for the"
                        + $" property '{propertyDefinition.FullName}' but the property has no getter.");
                    continue;
                }

                if (propertyDefinition.SetMethod == null)
                {
                    LogError(
                        $"The method '{methodDefinition.FullName}' is annotated to be a setter for the"
                        + $" property '{propertyDefinition.FullName}' but the property has no setter.");
                    continue;
                }

                InsertSetMethodCallIntoPropertySetter(propertyDefinition, methodDefinition);
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }

        private bool FindAndRemoveAttribute(IMemberDefinition methodDefinition, out CustomAttribute foundAttribute)
        {
            foundAttribute = methodDefinition.CustomAttributes.SingleOrDefault(
                attribute => attribute.AttributeType.FullName == _fullAttributeName);
            if (foundAttribute == null)
            {
                return false;
            }

            methodDefinition.CustomAttributes.Remove(foundAttribute);
            LogInfo($"Removed the attribute '{_fullAttributeName}' from the method '{methodDefinition.FullName}'.");
            return true;
        }

        private bool FindProperty(
            MethodDefinition methodDefinition,
            ICustomAttribute attribute,
            out PropertyDefinition propertyDefinition)
        {
            string propertyName = (string)attribute.ConstructorArguments.Single().Value;

            if (methodDefinition.ReturnType.FullName == TypeSystem.VoidReference.FullName
                || methodDefinition.Parameters?.Count != 2
                || methodDefinition.Parameters.Any(
                    definition => definition.ParameterType.FullName != methodDefinition.ReturnType.FullName))
            {
                LogError(
                    $"The method '{methodDefinition.FullName}' is annotated to be a setter for the"
                    + $" property '{propertyName}' but the method signature doesn't match."
                    + $" The expected signature is 'T {methodDefinition.Name}(T, T)' where 'T' is the property type.");
                propertyDefinition = null;
                return false;
            }

            propertyDefinition =
                methodDefinition.DeclaringType.Properties?.SingleOrDefault(
                    definition => definition.Name == propertyName);
            if (propertyDefinition == null)
            {
                LogError(
                    $"The method '{methodDefinition.FullName}' is annotated to be a setter for the"
                    + $" property '{propertyName}' but the property doesn't exist.");
                return false;
            }

            string expectedTypeFullName = methodDefinition.ReturnType.FullName;
            if (propertyDefinition.PropertyType.FullName == expectedTypeFullName)
            {
                return true;
            }

            LogError(
                $"The method '{methodDefinition.FullName}' is annotated to be a setter for the"
                + $" property '{propertyDefinition.FullName}' but the property's type doesn't"
                + $" match the method's. The expected type is '{expectedTypeFullName}'.");
            return false;
        }

        private void InsertSetMethodCallIntoPropertySetter(
            PropertyDefinition propertyDefinition,
            MethodReference setMethodReference)
        {
            ParameterDefinition parameterDefinition = propertyDefinition.SetMethod.Parameters.Single();
            Collection<Instruction> instructions = propertyDefinition.SetMethod.Body.Instructions;
            int index = -1;

            // value = this.setMethod(this.property, value);

            // Load this (for setMethod call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Load this (for getter call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Call getter
            instructions.Insert(
                ++index,
                Instruction.Create(OpCodes.Callvirt, propertyDefinition.GetMethod.GetGeneric()));
            // Load value
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_1));
            // Call setMethod
            instructions.Insert(++index, Instruction.Create(OpCodes.Callvirt, setMethodReference.GetGeneric()));
            // Store into value
            instructions.Insert(++index, Instruction.Create(OpCodes.Starg_S, parameterDefinition));

            LogInfo(
                $"Prefixed the setter of the property '{propertyDefinition.FullName}' with"
                + $" a call to the method '{setMethodReference.FullName}'.");
        }
    }
}
