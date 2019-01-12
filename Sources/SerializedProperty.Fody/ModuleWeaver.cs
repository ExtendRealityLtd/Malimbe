namespace Malimbe.SerializedProperty.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Fody;
    using Malimbe.Shared;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;

    public sealed class ModuleWeaver : BaseModuleWeaver
    {
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

                FieldReference backingFieldReference = propertyDefinition.GetBackingField();
                if (backingFieldReference == null)
                {
                    LogError(
                        $"No backing field for the property '{propertyDefinition.FullName}' was found."
                        + " A field is assumed to be backing the property if the property getter loads"
                        + " the field and the setter stores into that same field.");
                    continue;
                }

                LogInfo(
                    $"Assuming the field '{backingFieldReference.FullName}' is the backing field for"
                    + $" the property '{propertyDefinition.FullName}'.");

                ConfigureBackingField(backingFieldReference, propertyDefinition, attribute);
                InsertSetMethodCallIntoPropertySetter(propertyDefinition, backingFieldReference);
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "UnityEngine";
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

        private void ConfigureBackingField(
            FieldReference backingFieldReference,
            PropertyDefinition propertyDefinition,
            ICustomAttribute attribute)
        {
            FieldDefinition backingFieldDefinition =
                backingFieldReference as FieldDefinition ?? backingFieldReference.Resolve();
            Collection<CustomAttribute> customAttributes = backingFieldDefinition.CustomAttributes;

            if (customAttributes.All(
                customAttribute => customAttribute.Constructor != _serializeFieldAttributeConstructorReference))
            {
                customAttributes.Add(new CustomAttribute(_serializeFieldAttributeConstructorReference));
                LogInfo(
                    $"Added the attribute '{_serializeFieldAttributeConstructorReference.DeclaringType.FullName}'"
                    + $" to the field '{backingFieldReference.FullName}'.");
            }

            string propertyName = propertyDefinition.Name;
            if (backingFieldReference.Name == $"<{propertyDefinition.Name}>k__BackingField")
            {
                string previousFieldName = backingFieldReference.FullName;
                char firstNameCharacter = char.IsUpper(propertyName, 0)
                    ? char.ToLowerInvariant(propertyName[0])
                    : char.ToUpperInvariant(propertyName[0]);
                string newFieldName = $"{firstNameCharacter}{propertyName.Substring(1)}";

                if (backingFieldDefinition != backingFieldReference)
                {
                    IEnumerable<FieldReference> otherFieldReferences = new[]
                        {
                            propertyDefinition.GetMethod, propertyDefinition.SetMethod
                        }.SelectMany(definition => definition.Body?.Instructions)
                        .Select(instruction => instruction.Operand as FieldReference)
                        .Where(reference => reference?.FullName == previousFieldName);
                    foreach (FieldReference otherFieldReference in otherFieldReferences)
                    {
                        otherFieldReference.Name = newFieldName;
                    }
                }

                backingFieldDefinition.Name = newFieldName;
                LogInfo($"Changed the name of the field '{previousFieldName}' to '{newFieldName}'.");
            }

            bool isFieldVisibleInInspector = (bool)attribute.ConstructorArguments.Single().Value;
            if (isFieldVisibleInInspector)
            {
                return;
            }

            customAttributes.Add(new CustomAttribute(_hideInInspectorAttributeConstructorReference));
            LogInfo(
                $"Added the attribute '{_hideInInspectorAttributeConstructorReference.DeclaringType.FullName}'"
                + $" to the field '{backingFieldReference.FullName}'.");
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
