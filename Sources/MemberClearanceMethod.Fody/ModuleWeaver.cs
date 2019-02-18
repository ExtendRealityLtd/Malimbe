namespace Malimbe.MemberClearanceMethod.Fody
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
        private static readonly string _fullAttributeName = typeof(ClearedAttribute).FullName;
        private MethodReference _compilerGeneratedAttributeConstructorReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            string clearMethodNamePrefix = Config?.Attribute("MethodNamePrefix")?.Value ?? "Clear";
            IEnumerable<PropertyDefinition> propertyDefinitions =
                ModuleDefinition.GetTypes().SelectMany(definition => definition.Properties);
            IEnumerable<FieldDefinition> fieldDefinitions =
                ModuleDefinition.GetTypes().SelectMany(definition => definition.Fields);
            IEnumerable<IMemberDefinition> memberDefinitions =
                propertyDefinitions.Concat<IMemberDefinition>(fieldDefinitions);

            foreach (IMemberDefinition memberDefinition in memberDefinitions)
            {
                if (!FindAndRemoveAttribute(memberDefinition))
                {
                    continue;
                }

                TypeReference typeReference = null;
                switch (memberDefinition)
                {
                    case PropertyDefinition propertyDefinition:
                        if (propertyDefinition.SetMethod == null)
                        {
                            LogError(
                                $"The property '{propertyDefinition.FullName}' is annotated"
                                + " to be cleared but has no setter.");
                        }
                        else
                        {
                            typeReference = propertyDefinition.PropertyType;
                        }

                        break;
                    case FieldDefinition fieldDefinition:
                        typeReference = fieldDefinition.FieldType;
                        break;
                }

                if (typeReference == null)
                {
                    continue;
                }

                if (typeReference.IsPrimitive || typeReference.IsValueType)
                {
                    LogError(
                        $"The member '{memberDefinition.FullName}' is annotated to be cleared"
                        + $" but its type '{typeReference.FullName}' isn't of reference type.");
                    continue;
                }

                string methodName = clearMethodNamePrefix
                    + char.ToUpperInvariant(memberDefinition.Name[0])
                    + memberDefinition.Name.Substring(1);
                MethodDefinition clearMethodDefinition = FindClearMethod(memberDefinition, methodName);
                if (clearMethodDefinition != null)
                {
                    LogInfo(
                        $"The clear method '{clearMethodDefinition.FullName}' already exists. A setter call"
                        + $" to clear the member '{memberDefinition.FullName}' will be inserted nonetheless."
                        + " The clearing will be done at the end of the existing method.");
                }
                else
                {
                    clearMethodDefinition = CreateClearMethod(memberDefinition, methodName);
                }

                switch (memberDefinition)
                {
                    case PropertyDefinition propertyDefinition:
                        ClearPropertyInMethod(propertyDefinition, clearMethodDefinition);
                        break;
                    case FieldDefinition fieldDefinition:
                        ClearFieldInMethod(fieldDefinition, clearMethodDefinition);
                        break;
                }
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

        private bool FindAndRemoveAttribute(IMemberDefinition memberDefinition)
        {
            CustomAttribute foundAttribute = memberDefinition.CustomAttributes.SingleOrDefault(
                attribute => attribute.AttributeType.FullName == _fullAttributeName);
            if (foundAttribute == null)
            {
                return false;
            }

            memberDefinition.CustomAttributes.Remove(foundAttribute);
            LogInfo($"Removed the attribute '{_fullAttributeName}' from the member '{memberDefinition.FullName}'.");
            return true;
        }

        private MethodDefinition FindClearMethod(IMemberDefinition memberDefinition, string methodName) =>
            memberDefinition.DeclaringType.Methods.FirstOrDefault(
                definition => string.Equals(definition.Name, methodName, StringComparison.OrdinalIgnoreCase)
                    && definition.ReturnType.FullName == TypeSystem.VoidReference.FullName
                    && !definition.HasParameters);

        private MethodDefinition CreateClearMethod(IMemberDefinition memberDefinition, string methodName)
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
            memberDefinition.DeclaringType.Methods.Add(newMethodDefinition);

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
                Instruction.Create(OpCodes.Callvirt, propertyDefinition.SetMethod.CreateGenericMethodIfNeeded()));

            LogInfo(
                $"Inserted a setter call to clear the property '{propertyDefinition.FullName}'"
                + $" into the method '{methodDefinition.FullName}'.");
        }

        private void ClearFieldInMethod(FieldReference fieldReference, MethodDefinition methodDefinition)
        {
            Collection<Instruction> instructions = methodDefinition.Body.Instructions;
            int index = instructions.Count - 2;

            // this.field = null;

            // Load this (for field storage)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Load null (for field storage)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldnull));
            // Store into field
            instructions.Insert(++index, Instruction.Create(OpCodes.Stfld, fieldReference));

            LogInfo(
                $"Inserted a setter call to clear the field '{fieldReference.FullName}'"
                + $" into the method '{methodDefinition.FullName}'.");
        }
    }
}
