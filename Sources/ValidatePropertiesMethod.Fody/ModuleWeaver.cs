namespace Malimbe.ValidatePropertiesMethod.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::Fody;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;

    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private MethodReference _compilerGeneratedAttributeConstructorReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            string validationMethodName = Config?.Attribute("MethodName")?.Value ?? "OnValidate";
            List<Regex> namespaceFilters = FindNamespaceFilters().ToList();

            IEnumerable<PropertyDefinition> propertyDefinitions = ModuleDefinition.Types
                .Where(
                    definition => namespaceFilters.Count == 0
                        || namespaceFilters.Any(regex => regex.IsMatch(definition.Namespace)))
                .SelectMany(definition => definition.Properties)
                .Where(definition => definition.SetMethod != null);
            List<MethodDefinition> existingMethodDefinitions = FindValidationMethods(validationMethodName).ToList();

            foreach (PropertyDefinition propertyDefinition in propertyDefinitions)
            {
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
            yield return "netstandard.dll";
            yield return "mscorlib.dll";
        }

        private void FindReferences() =>
            _compilerGeneratedAttributeConstructorReference = ModuleDefinition.ImportReference(
                FindType("System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                    .Methods.First(definition => definition.IsConstructor));

        private IEnumerable<Regex> FindNamespaceFilters() =>
            Config?.Elements("NamespaceFilter")
                .Select(xElement => xElement.Value)
                .Select(filter => new Regex(filter, RegexOptions.Compiled))
                .ToList()
            ?? Enumerable.Empty<Regex>();

        private IEnumerable<MethodDefinition> FindValidationMethods(string validationMethodName) =>
            ModuleDefinition.Types
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
            int index = instructions.Count - 2;

            // Property = Property;

            // Load this (for setter call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Load this (for getter call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Call getter
            instructions.Insert(++index, Instruction.Create(OpCodes.Callvirt, propertyDefinition.GetMethod));
            // Call setter
            instructions.Insert(++index, Instruction.Create(OpCodes.Callvirt, propertyDefinition.SetMethod));

            LogInfo(
                $"Inserted a property setter call of '{propertyDefinition.FullName}'"
                + $" into the method '{methodDefinition.FullName}'.");
        }

        private MethodDefinition OverrideBaseMethodIfNeeded(MethodDefinition methodDefinition)
        {
            MethodDefinition baseMethodDefinition = FindBaseMethod(methodDefinition);
            if (baseMethodDefinition == methodDefinition)
            {
                return null;
            }

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
                && instructions[index].Operand == baseMethodDefinition)
            {
                LogInfo(
                    $"No base call was inserted into the method '{methodDefinition.FullName}'"
                    + " because there already is such a call at the start of the method.");
                return baseMethodDefinition;
            }

            // base.MethodName();

            // Load this (for base method call)
            instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
            // Call base method
            instructions.Insert(
                1,
                Instruction.Create(
                    OpCodes.Call,
                    methodDefinition.DeclaringType.Module.ImportReference(baseMethodDefinition)));

            LogInfo($"Inserted a base call into the method '{methodDefinition.FullName}'.");
            return baseMethodDefinition;
        }

        private static MethodDefinition FindBaseMethod(MethodDefinition methodDefinition)
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
    }
}
