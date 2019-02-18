namespace Malimbe.PropertySetterMethod.Fody
{
    using System.Collections.Generic;
    using System.Linq;
    using global::Fody;
    using Malimbe.Shared;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;
    using Mono.Collections.Generic;

    // ReSharper disable once UnusedMember.Global
    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private static readonly string _fullAttributeName = typeof(CalledBySetterAttribute).FullName;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            IEnumerable<MethodDefinition> methodDefinitions =
                ModuleDefinition.GetTypes().SelectMany(definition => definition.Methods);
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
                        $"The method '{methodDefinition.FullName}' is annotated to be called by the setter of the"
                        + $" property '{propertyDefinition.FullName}' but the property has no getter.");
                    continue;
                }

                if (propertyDefinition.SetMethod == null)
                {
                    LogError(
                        $"The method '{methodDefinition.FullName}' is annotated to be called by the setter of the"
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
            TypeDefinition typeDefinition = methodDefinition.DeclaringType;
            propertyDefinition = null;

            while (typeDefinition != null)
            {
                propertyDefinition =
                    typeDefinition.Properties?.SingleOrDefault(definition => definition.Name == propertyName);
                if (propertyDefinition != null)
                {
                    break;
                }

                typeDefinition = typeDefinition.BaseType.Resolve();
            }

            if (propertyDefinition == null)
            {
                LogError(
                    $"The method '{methodDefinition.FullName}' is annotated to be called by the setter of the"
                    + $" property '{propertyName}' but the property doesn't exist.");
                return false;
            }

            string expectedTypeFullName = propertyDefinition.PropertyType.FullName;
            if (methodDefinition.ReturnType.FullName == TypeSystem.VoidReference.FullName
                && methodDefinition.Parameters?.Count == 2
                && methodDefinition.Parameters[0].ParameterType.FullName == expectedTypeFullName
                && methodDefinition.Parameters[1].ParameterType.IsByReference
                && methodDefinition.Parameters[1].ParameterType.FullName.TrimEnd('&') == expectedTypeFullName)
            {
                return true;
            }

            LogError(
                $"The method '{methodDefinition.FullName}' is annotated to be called by the setter of the"
                + $" property '{propertyName}' but the method signature doesn't match. The expected signature is"
                + $" 'void {methodDefinition.Name}({expectedTypeFullName}, ref {expectedTypeFullName})'.");
            propertyDefinition = null;

            return false;
        }

        private void InsertSetMethodCallIntoPropertySetter(
            PropertyDefinition propertyDefinition,
            MethodReference setMethodReference)
        {
            MethodBody methodBody = propertyDefinition.SetMethod.Body;
            Collection<Instruction> instructions = methodBody.Instructions;
            int index = -1;

            methodBody.SimplifyMacros();

            // previousValue = this.property;
            MethodReference getMethodReference = propertyDefinition.GetMethod.CreateGenericMethodIfNeeded();
            VariableDefinition previousValueVariableDefinition = methodBody.Variables.FirstOrDefault(
                definition =>
                {
                    if (definition.VariableType.FullName != propertyDefinition.PropertyType.FullName)
                    {
                        return false;
                    }

                    List<Instruction> storeInstructions = instructions.Where(
                            instruction => instruction.OpCode == OpCodes.Stloc && instruction.Operand == definition)
                        .ToList();
                    if (storeInstructions.Count != 1)
                    {
                        return false;
                    }

                    Instruction storeInstruction = storeInstructions.Single();
                    return storeInstruction.Previous?.OpCode == OpCodes.Callvirt
                        && (storeInstruction.Previous.Operand as MethodReference)?.FullName
                        == getMethodReference.FullName
                        && storeInstruction.Previous.Previous?.OpCode == OpCodes.Ldarg;
                });

            if (previousValueVariableDefinition == null)
            {
                previousValueVariableDefinition = new VariableDefinition(propertyDefinition.PropertyType);
                methodBody.Variables.Add(previousValueVariableDefinition);

                // Load this (for getter call)
                instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
                // Call getter
                instructions.Insert(++index, Instruction.Create(OpCodes.Callvirt, getMethodReference));
                // Store into previousValue
                instructions.Insert(++index, Instruction.Create(OpCodes.Stloc, previousValueVariableDefinition));
            }

            List<Instruction> returnInstructions =
                instructions.Where(instruction => instruction.OpCode == OpCodes.Ret).ToList();
            foreach (Instruction returnInstruction in returnInstructions)
            {
                index = instructions.IndexOf(returnInstruction) - 1;

                Instruction nopInstruction = null;
                if (setMethodReference.DeclaringType.FullName != propertyDefinition.DeclaringType.FullName)
                {
                    // if (this is setMethodReference.DeclaringType) { ...
                    nopInstruction = Instruction.Create(OpCodes.Nop);

                    // Load this (for instance check)
                    instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
                    // Instance check
                    instructions.Insert(++index, Instruction.Create(OpCodes.Isinst, setMethodReference.DeclaringType));
                    // Jump if false
                    instructions.Insert(++index, Instruction.Create(OpCodes.Brfalse, nopInstruction));
                }

                // this.setMethod(previousValue, this.propertyBackingField);

                // Load this (for setMethod call)
                instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
                // Load previousValue
                instructions.Insert(++index, Instruction.Create(OpCodes.Ldloc, previousValueVariableDefinition));
                // Load this (for backing field get)
                instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
                // Load address of backing field
                instructions.Insert(
                    ++index,
                    Instruction.Create(OpCodes.Ldflda, propertyDefinition.FindBackingField()));
                // Call setMethod
                instructions.Insert(
                    ++index,
                    Instruction.Create(OpCodes.Callvirt, setMethodReference.CreateGenericMethodIfNeeded()));

                if (nopInstruction != null)
                {
                    // ... }

                    // Nop to jump to (see above)
                    instructions.Insert(++index, nopInstruction);
                }
            }

            methodBody.OptimizeMacros();

            LogInfo(
                $"Inserted a call to the method '{setMethodReference.FullName}' into"
                + $" the setter of the property '{propertyDefinition.FullName}'.");
        }
    }
}
