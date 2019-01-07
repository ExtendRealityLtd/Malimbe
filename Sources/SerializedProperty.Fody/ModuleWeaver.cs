namespace Malimbe.SerializedProperty.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Fody;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;

    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private sealed class FieldDefinitionComparer : IEqualityComparer<FieldDefinition>
        {
            public static readonly FieldDefinitionComparer Instance = new FieldDefinitionComparer();

            public bool Equals(FieldDefinition x, FieldDefinition y) =>
                string.Equals(x?.FullName, y?.FullName, StringComparison.Ordinal);

            public int GetHashCode(FieldDefinition obj) =>
                obj.FullName.GetHashCode();
        }

        private static readonly string _fullAttributeName = typeof(SerializedPropertyAttribute).FullName;

        private MethodReference _serializeFieldAttributeConstructorReference;
        private MethodReference _hideInInspectorAttributeConstructorReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            IEnumerable<PropertyDefinition> propertyDefinitions =
                ModuleDefinition.Types.SelectMany(definition => definition.Properties);
            foreach (PropertyDefinition propertyDefinition in propertyDefinitions)
            {
                if (!FindAndRemoveAttribute(propertyDefinition, out CustomAttribute attribute))
                {
                    continue;
                }

                if (propertyDefinition.GetMethod == null)
                {
                    LogError(
                        $"The property '{propertyDefinition.FullName}' is marked to be"
                        + " serializable but has no getter.");
                    continue;
                }

                if (propertyDefinition.SetMethod == null)
                {
                    LogError(
                        $"The property '{propertyDefinition.FullName}' is marked to be"
                        + " serializable but has no setter.");
                    continue;
                }

                FieldDefinition backingFieldDefinition = GetBackingField(propertyDefinition);
                if (backingFieldDefinition == null)
                {
                    LogError(
                        $"No backing field for the property '{propertyDefinition.FullName}' was found."
                        + " A field is assumed to be backing the property if the property getter loads"
                        + " the field and the setter stores into that same field.");
                    continue;
                }

                LogInfo(
                    $"Assuming the field '{backingFieldDefinition.FullName}' is the backing field for"
                    + $" the property '{propertyDefinition.FullName}'.");

                ConfigureBackingField(backingFieldDefinition, propertyDefinition, attribute);
                InsertSetMethodCallIntoPropertySetter(propertyDefinition, backingFieldDefinition);
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "UnityEngine";

            yield return "System.Runtime";
            yield return "netstandard.dll";
            yield return "mscorlib.dll";
        }

        private void FindReferences()
        {
            MethodReference FindTypeConstructorReference(string fullTypeName) =>
                ModuleDefinition.ImportReference(
                    FindType(fullTypeName).Methods.First(definition => definition.IsConstructor));

            _serializeFieldAttributeConstructorReference = FindTypeConstructorReference("UnityEngine.SerializeField");
            _hideInInspectorAttributeConstructorReference =
                FindTypeConstructorReference("UnityEngine.HideInInspector");
        }

        private bool FindAndRemoveAttribute(IMemberDefinition propertyDefinition, out CustomAttribute foundAttribute)
        {
            foundAttribute = propertyDefinition.CustomAttributes.SingleOrDefault(
                attribute => attribute.AttributeType.FullName == _fullAttributeName);
            if (foundAttribute == null)
            {
                return false;
            }

            propertyDefinition.CustomAttributes.Remove(foundAttribute);
            LogInfo(
                $"Removed the attribute '{_fullAttributeName}' from"
                + $" the property '{propertyDefinition.FullName}'.");
            return true;
        }

        private static FieldDefinition GetBackingField(PropertyDefinition propertyDefinition)
        {
            IEnumerable<FieldDefinition> getFieldDefinitions = propertyDefinition.GetMethod.Body?.Instructions
                ?.Where(instruction => instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld)
                .Select(instruction => (FieldDefinition)instruction.Operand);
            IEnumerable<FieldDefinition> setFieldDefinitions = propertyDefinition.SetMethod.Body?.Instructions
                ?.Where(instruction => instruction.OpCode == OpCodes.Stsfld || instruction.OpCode == OpCodes.Stfld)
                .Select(instruction => (FieldDefinition)instruction.Operand);

            return getFieldDefinitions?.Intersect(
                    setFieldDefinitions ?? Enumerable.Empty<FieldDefinition>(),
                    FieldDefinitionComparer.Instance)
                .FirstOrDefault();
        }

        private void ConfigureBackingField(
            FieldDefinition backingFieldDefinition,
            MemberReference propertyReference,
            ICustomAttribute attribute)
        {
            if (backingFieldDefinition.CustomAttributes.All(
                customAttribute => customAttribute.Constructor != _serializeFieldAttributeConstructorReference))
            {
                backingFieldDefinition.CustomAttributes.Add(
                    new CustomAttribute(_serializeFieldAttributeConstructorReference));
                LogInfo(
                    $"Added the attribute '{_serializeFieldAttributeConstructorReference.DeclaringType.FullName}'"
                    + $" to the field '{backingFieldDefinition.FullName}'.");
            }

            if (backingFieldDefinition.Name.IndexOf("BackingField", StringComparison.OrdinalIgnoreCase) != -1)
            {
                string previousFieldName = backingFieldDefinition.FullName;
                string propertyName = propertyReference.Name;
                char firstNameCharacter = char.IsUpper(propertyName, 0)
                    ? char.ToLowerInvariant(propertyName[0])
                    : char.ToUpperInvariant(propertyName[0]);

                backingFieldDefinition.Name = $"{firstNameCharacter}{propertyName.Substring(1)}";
                LogInfo($"Changed the name of the field '{previousFieldName}' to '{backingFieldDefinition.Name}'.");
            }

            bool hidesFieldInInspector = (bool)attribute.ConstructorArguments.Single().Value;
            if (!hidesFieldInInspector)
            {
                return;
            }

            backingFieldDefinition.CustomAttributes.Add(
                new CustomAttribute(_hideInInspectorAttributeConstructorReference));
            LogInfo(
                $"Added the attribute '{_hideInInspectorAttributeConstructorReference.DeclaringType.FullName}'"
                + $" to the field '{backingFieldDefinition.FullName}'.");
        }

        private void InsertSetMethodCallIntoPropertySetter(
            PropertyDefinition propertyDefinition,
            FieldReference backingFieldReference)
        {
            MethodDefinition setMethodDefinition = FindSetMethodDefinition(propertyDefinition);
            if (setMethodDefinition == null)
            {
                return;
            }

            ParameterDefinition parameterDefinition = propertyDefinition.SetMethod.Parameters.Single();
            Collection<Instruction> instructions = propertyDefinition.SetMethod.Body.Instructions;
            int index = -1;

            // value = this.setMethod(this.field, value);

            // Load this (for method call)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Load this (for field load)
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
            // Load field
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldfld, backingFieldReference));
            // Load value
            instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_1));
            // Call setMethod
            instructions.Insert(++index, Instruction.Create(OpCodes.Callvirt, setMethodDefinition));
            // Store into value
            instructions.Insert(++index, Instruction.Create(OpCodes.Starg_S, parameterDefinition));

            LogInfo(
                $"Prefixed the setter of the property '{propertyDefinition.FullName}' with"
                + $" a call to the method '{setMethodDefinition.FullName}'.");
        }

        private MethodDefinition FindSetMethodDefinition(PropertyDefinition propertyDefinition)
        {
            MethodDefinition expectedDefinition = new MethodDefinition(
                $"Set{propertyDefinition.Name}",
                0,
                propertyDefinition.PropertyType)
            {
                DeclaringType = propertyDefinition.DeclaringType
            };
            expectedDefinition.Parameters.Add(new ParameterDefinition(propertyDefinition.PropertyType));
            expectedDefinition.Parameters.Add(new ParameterDefinition(propertyDefinition.PropertyType));

            IEnumerable<MethodDefinition> potentialDefinitions = propertyDefinition.DeclaringType.Methods.Where(
                definition => string.Equals(
                    definition.Name,
                    expectedDefinition.Name,
                    StringComparison.OrdinalIgnoreCase));
            MethodDefinition foundDefinition = null;

            foreach (MethodDefinition potentialDefinition in potentialDefinitions)
            {
                if (potentialDefinition.ReturnType == expectedDefinition.ReturnType
                    && potentialDefinition.Parameters.Count == expectedDefinition.Parameters.Count
                    && potentialDefinition.Parameters.Where(
                            (definition, index) =>
                                definition.ParameterType == expectedDefinition.Parameters[index].ParameterType)
                        .Any())
                {
                    foundDefinition = potentialDefinition;
                }
                else
                {
                    LogWarning(
                        $"The method '{potentialDefinition.FullName}' matches the expected setter method for the"
                        + " property by name but the signature doesn't match."
                        + $" The expected signature is '{expectedDefinition.FullName}'.");
                }
            }

            return foundDefinition;
        }
    }
}
