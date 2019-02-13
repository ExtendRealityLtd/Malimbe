namespace Malimbe.PropertySerializationAttribute.Fody
{
    using System.Collections.Generic;
    using System.Linq;
    using global::Fody;
    using Malimbe.Shared;
    using Mono.Cecil;
    using Mono.Collections.Generic;

    // ReSharper disable once UnusedMember.Global
    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private static readonly string _fullAttributeName = typeof(SerializedAttribute).FullName;

        private MethodReference _serializeFieldAttributeConstructorReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            IEnumerable<PropertyDefinition> propertyDefinitions =
                ModuleDefinition.Types.SelectMany(definition => definition.Properties);
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (PropertyDefinition propertyDefinition in propertyDefinitions)
            {
                if (!FindAndRemoveAttribute(propertyDefinition))
                {
                    continue;
                }

                FieldReference backingFieldReference = propertyDefinition.FindBackingField();
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

                ConfigureBackingField(backingFieldReference, propertyDefinition);
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "UnityEngine";
        }

        private void FindReferences() =>
            _serializeFieldAttributeConstructorReference = ModuleDefinition.ImportReference(
                FindType("UnityEngine.SerializeField").Methods.First(definition => definition.IsConstructor));

        private bool FindAndRemoveAttribute(IMemberDefinition propertyDefinition)
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
            return true;
        }

        private void ConfigureBackingField(FieldReference backingFieldReference, PropertyDefinition propertyDefinition)
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
            if (backingFieldReference.Name != $"<{propertyDefinition.Name}>k__BackingField")
            {
                return;
            }

            string previousFieldName = backingFieldReference.FullName;
            char firstNameCharacter = char.IsUpper(propertyName, 0)
                ? char.ToLowerInvariant(propertyName[0])
                : char.ToUpperInvariant(propertyName[0]);
            string newFieldName = $"{firstNameCharacter}{propertyName.Substring(1)}";

            if (backingFieldDefinition != backingFieldReference)
            {
                IEnumerable<FieldReference> otherFieldReferences = propertyDefinition.DeclaringType.Methods
                    .SelectMany(definition => definition.Body?.Instructions)
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
    }
}
