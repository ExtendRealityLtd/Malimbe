namespace Malimbe.PropertyValidationMethod.Fody
{
    using System;
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
        private static readonly string _fullAttributeName = typeof(ValidatedAttribute).FullName;
        private MethodReference _compilerGeneratedAttributeConstructorReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            string validationMethodName = Config?.Attribute("MethodName")?.Value ?? "OnValidate";

            IEnumerable<PropertyDefinition> propertyDefinitions =
                ModuleDefinition.GetTypes().SelectMany(definition => definition.Properties);
            List<MethodDefinition> existingMethodDefinitions = FindValidationMethods(validationMethodName).ToList();

            foreach (PropertyDefinition propertyDefinition in propertyDefinitions)
            {
                if (!FindAndRemoveAttribute(propertyDefinition))
                {
                    continue;
                }

                MethodDefinition methodDefinition =
                    FindOrCreateValidationMethod(propertyDefinition, validationMethodName);
                SetPropertyInMethod(propertyDefinition, methodDefinition);

                MethodDefinition baseMethodDefinition = OverrideBaseMethodIfNeeded(methodDefinition);
                existingMethodDefinitions.Remove(baseMethodDefinition);
            }

            while (existingMethodDefinitions.Count > 0)
            {
                int index = existingMethodDefinitions.Count - 1;
                MethodDefinition existingMethodDefinition = existingMethodDefinitions[index];
                existingMethodDefinitions.RemoveAt(index);

                MethodDefinition baseMethodDefinition = OverrideBaseMethodIfNeeded(existingMethodDefinition);
                existingMethodDefinitions.Remove(baseMethodDefinition);
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "System.Runtime";
            yield return "netstandard";
            yield return "mscorlib";
        }

        private void FindReferences() =>
            _compilerGeneratedAttributeConstructorReference = ModuleDefinition.ImportReference(
                FindType("System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                    .Methods.First(definition => definition.IsConstructor));

        private bool FindAndRemoveAttribute(PropertyDefinition propertyDefinition)
        {
            CustomAttribute foundAttribute = propertyDefinition.CustomAttributes.SingleOrDefault(
                attribute => attribute.AttributeType.FullName == _fullAttributeName);
            if (foundAttribute == null)
            {
                return false;
            }

            propertyDefinition.CustomAttributes.Remove(foundAttribute);
            LogInfo(
                $"Removed the attribute '{_fullAttributeName}' from"
                + $" the property '{propertyDefinition.FullName}'.");

            if (propertyDefinition.GetMethod == null)
            {
                LogError(
                    $"The property '{propertyDefinition.FullName}' is annotated"
                    + " to be validated but has no getter.");
                return false;
            }

            // ReSharper disable once InvertIf
            if (propertyDefinition.SetMethod == null)
            {
                LogError(
                    $"The property '{propertyDefinition.FullName}' is annotated"
                    + " to be validated but has no setter.");
                return false;
            }

            return true;
        }

        private IEnumerable<MethodDefinition> FindValidationMethods(string validationMethodName) =>
            ModuleDefinition.GetTypes()
                .Where(
                    definition => !definition.IsInterface
                        && !definition.IsEnum
                        && !definition.IsValueType
                        && definition.HasMethods)
                .SelectMany(definition => definition.Methods)
                .Where(
                    definition =>
                        string.Equals(definition.Name, validationMethodName, StringComparison.OrdinalIgnoreCase)
                        && definition.ReturnType.FullName == TypeSystem.VoidReference.FullName
                        && !definition.HasParameters);

        private MethodDefinition FindOrCreateValidationMethod(
            IMemberDefinition propertyDefinition,
            string validationMethodName)
        {
            TypeReference returnTypeReference = TypeSystem.VoidReference;
            MethodDefinition existingMethodDefinition = propertyDefinition.DeclaringType.Methods.FirstOrDefault(
                definition => string.Equals(definition.Name, validationMethodName, StringComparison.OrdinalIgnoreCase)
                    && definition.ReturnType.FullName == returnTypeReference.FullName
                    && !definition.HasParameters);
            if (existingMethodDefinition != null)
            {
                return existingMethodDefinition;
            }

            MethodDefinition newMethodDefinition = new MethodDefinition(
                validationMethodName,
                MethodAttributes.Private | MethodAttributes.HideBySig,
                returnTypeReference);
            newMethodDefinition.CustomAttributes.Add(
                new CustomAttribute(_compilerGeneratedAttributeConstructorReference));
            propertyDefinition.DeclaringType.Methods.Add(newMethodDefinition);

            // Return
            newMethodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            return newMethodDefinition;
        }

        private void SetPropertyInMethod(PropertyDefinition propertyDefinition, MethodDefinition methodDefinition)
        {
            Collection<Instruction> instructions = methodDefinition.Body.Instructions;
            int index = instructions.IndexOf(instructions.First(instruction => instruction.OpCode == OpCodes.Ret)) - 1;

            // Property = Property;

            // Load this (for setter call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Load this (for getter call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Call getter
            instructions.Insert(
                ++index,
                Instruction.Create(OpCodes.Callvirt, propertyDefinition.GetMethod.CreateGenericMethodIfNeeded()));
            // Call setter
            instructions.Insert(
                ++index,
                Instruction.Create(OpCodes.Callvirt, propertyDefinition.SetMethod.CreateGenericMethodIfNeeded()));

            LogInfo(
                $"Inserted a property setter call of '{propertyDefinition.FullName}'"
                + $" into the method '{methodDefinition.FullName}'.");
        }

        private MethodDefinition OverrideBaseMethodIfNeeded(MethodDefinition methodDefinition)
        {
            MethodReference baseMethodReference = methodDefinition.FindBaseMethod();
            if (baseMethodReference == methodDefinition)
            {
                return null;
            }

            MethodDefinition baseMethodDefinition = baseMethodReference.Resolve();
            if (!baseMethodDefinition.IsFamily)
            {
                baseMethodDefinition.IsFamily = true;
            }

            if (!baseMethodDefinition.IsVirtual && !baseMethodDefinition.IsNewSlot)
            {
                baseMethodDefinition.IsNewSlot = true;
            }

            baseMethodDefinition.IsFinal = false;
            baseMethodDefinition.IsVirtual = true;
            baseMethodDefinition.IsHideBySig = true;

            methodDefinition.IsPrivate = baseMethodDefinition.IsPrivate;
            methodDefinition.IsFamily = baseMethodDefinition.IsFamily;
            methodDefinition.IsFamilyAndAssembly = baseMethodDefinition.IsFamilyAndAssembly;

            methodDefinition.IsFinal = false;
            methodDefinition.IsVirtual = true;
            methodDefinition.IsNewSlot = false;
            methodDefinition.IsReuseSlot = true;
            methodDefinition.IsHideBySig = true;

            LogInfo(
                $"Changed the method '{methodDefinition.FullName}' to override"
                + $" the base method '{baseMethodDefinition.FullName}'.");

            Collection<Instruction> instructions = methodDefinition.Body.Instructions;
            int index = instructions.TakeWhile(instruction => instruction.OpCode == OpCodes.Nop).Count() + 1;
            if (index < instructions.Count
                && (instructions[index].OpCode == OpCodes.Callvirt
                    || instructions[index].OpCode == OpCodes.Call
                    || instructions[index].OpCode == OpCodes.Calli)
                && (instructions[index].Operand as MethodReference)?.FullName == baseMethodReference.FullName)
            {
                LogInfo(
                    $"No base call was inserted into the method '{methodDefinition.FullName}'"
                    + " because there already is such a call at the start of the method.");
                return baseMethodDefinition;
            }

            index = -1;

            // base.MethodName();

            // Load this (for base method call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Call base method
            instructions.Insert(++index, Instruction.Create(OpCodes.Call, baseMethodReference));

            LogInfo($"Inserted a base call into the method '{methodDefinition.FullName}'.");
            return baseMethodDefinition;
        }
    }
}
