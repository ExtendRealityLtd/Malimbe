namespace Malimbe.ClearPropertyMethod.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::Fody;
    using Malimbe.Shared;
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

            string clearMethodNamePrefix = Config?.Attribute("MethodNamePrefix")?.Value ?? "Clear";
            List<Regex> namespaceFilters = FindNamespaceFilters().ToList();
            IEnumerable<PropertyDefinition> propertyDefinitions = ModuleDefinition.Types
                .Where(
                    definition => namespaceFilters.Count == 0
                        || namespaceFilters.Any(regex => regex.IsMatch(definition.Namespace)))
                .SelectMany(definition => definition.Properties)
                .Where(
                    definition => !definition.PropertyType.IsPrimitive
                        && !definition.PropertyType.IsValueType
                        && definition.SetMethod != null);

            foreach (PropertyDefinition propertyDefinition in propertyDefinitions)
            {
                string methodName = clearMethodNamePrefix
                    + char.ToUpperInvariant(propertyDefinition.Name[0])
                    + propertyDefinition.Name.Substring(1);
                MethodDefinition existingMethodDefinition = FindClearMethod(propertyDefinition, methodName);
                if (existingMethodDefinition != null)
                {
                    LogInfo(
                        $"The clear method '{existingMethodDefinition.FullName}' already exists."
                        + $" A setter call to clear the property '{propertyDefinition.FullName}' will not be inserted.");
                    continue;
                }

                MethodDefinition clearMethodDefinition = CreateClearMethod(propertyDefinition, methodName);
                ClearPropertyInMethod(propertyDefinition, clearMethodDefinition);
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

        private IEnumerable<Regex> FindNamespaceFilters() =>
            Config?.Elements("NamespaceFilter")
                .Select(xElement => xElement.Value)
                .Select(filter => new Regex(filter, RegexOptions.Compiled))
                .ToList()
            ?? Enumerable.Empty<Regex>();

        private MethodDefinition FindClearMethod(IMemberDefinition propertyDefinition, string methodName) =>
            propertyDefinition.DeclaringType.Methods.FirstOrDefault(
                definition => string.Equals(definition.Name, methodName, StringComparison.OrdinalIgnoreCase)
                    && definition.ReturnType.FullName == TypeSystem.VoidReference.FullName
                    && !definition.HasParameters);

        private MethodDefinition CreateClearMethod(IMemberDefinition propertyDefinition, string methodName)
        {
            MethodDefinition newMethodDefinition = new MethodDefinition(
                methodName,
                MethodAttributes.Public | MethodAttributes.HideBySig,
                TypeSystem.VoidReference)
            {
                IsPublic = true
            };
            newMethodDefinition.CustomAttributes.Add(
                new CustomAttribute(_compilerGeneratedAttributeConstructorReference));
            propertyDefinition.DeclaringType.Methods.Add(newMethodDefinition);

            // Return
            newMethodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            return newMethodDefinition;
        }

        private void ClearPropertyInMethod(PropertyDefinition propertyDefinition, MethodDefinition methodDefinition)
        {
            Collection<Instruction> instructions = methodDefinition.Body.Instructions;
            int index = instructions.Count - 2;

            // this.Property = null;

            // Load this (for setter call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Load null (for setter call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldnull));
            // Call setter
            instructions.Insert(
                ++index,
                Instruction.Create(OpCodes.Callvirt, propertyDefinition.SetMethod.GetGeneric()));

            LogInfo(
                $"Inserted a setter call to clear the property '{propertyDefinition.FullName}'"
                + $" into the method '{methodDefinition.FullName}'.");
        }
    }
}
