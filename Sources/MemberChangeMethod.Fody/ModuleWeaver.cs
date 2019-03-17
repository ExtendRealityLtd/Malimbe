namespace Malimbe.MemberChangeMethod.Fody
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
        private static readonly string _fullBaseAttributeName = typeof(HandlesMemberChangeAttribute).FullName;
        private static readonly string _fullBeforeChangeAttributeName = typeof(CalledBeforeChangeOfAttribute).FullName;

        private MethodReference _isApplicationPlayingGetterReference;
        private MethodReference _isActiveAndEnabledGetterReference;
        private TypeReference _behaviourReference;
        private bool _isCompilingForEditor;

        public override bool ShouldCleanReference =>
            // InspectorEditor needs this assembly.
            false;

        public override void Execute()
        {
            FindReferences();

            IEnumerable<MethodDefinition> methodDefinitions =
                ModuleDefinition.GetTypes().SelectMany(definition => definition.Methods);
            foreach (MethodDefinition methodDefinition in methodDefinitions)
            {
                foreach (CustomAttribute attribute in FindAttributes(methodDefinition))
                {
                    if (!FindProperty(methodDefinition, attribute, out PropertyDefinition propertyDefinition))
                    {
                        continue;
                    }

                    if (propertyDefinition.SetMethod == null)
                    {
                        LogError(
                            $"The method '{methodDefinition.FullName}' is annotated to be called by the setter of the"
                            + $" property '{propertyDefinition.FullName}' but the property has no setter.");
                        continue;
                    }

                    ChangePropertySetterCallsToFieldStore(propertyDefinition, methodDefinition);
                    InsertSetMethodCallIntoPropertySetter(propertyDefinition, methodDefinition, attribute);
                }
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "UnityEngine";
        }

        private void FindReferences()
        {
            MethodReference ImportPropertyGetter(string typeName, string propertyName) =>
                ModuleDefinition.ImportReference(
                    FindType(typeName).Properties.Single(definition => definition.Name == propertyName).GetMethod);

            _isApplicationPlayingGetterReference = ImportPropertyGetter("UnityEngine.Application", "isPlaying");
            _isActiveAndEnabledGetterReference = ImportPropertyGetter("UnityEngine.Behaviour", "isActiveAndEnabled");
            _behaviourReference = ModuleDefinition.ImportReference(FindType("UnityEngine.Behaviour"));
            _isCompilingForEditor = DefineConstants.Contains("UNITY_EDITOR");
        }

        private static IEnumerable<CustomAttribute> FindAttributes(ICustomAttributeProvider methodDefinition) =>
            methodDefinition.CustomAttributes.Where(
                    attribute => attribute.AttributeType.Resolve().BaseType.FullName == _fullBaseAttributeName)
                .ToList();

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

            if (methodDefinition.ReturnType.FullName == TypeSystem.VoidReference.FullName
                && methodDefinition.Parameters?.Count == 0)
            {
                return true;
            }

            LogError(
                $"The method '{methodDefinition.FullName}' is annotated to be called by the setter of the"
                + $" property '{propertyName}' but the method signature doesn't match. The expected signature is"
                + $" 'void {methodDefinition.Name}()'.");
            propertyDefinition = null;

            return false;
        }

        private void ChangePropertySetterCallsToFieldStore(
            PropertyDefinition propertyDefinition,
            MethodDefinition methodDefinition)
        {
            MethodBody methodBody = methodDefinition.Body;
            Collection<Instruction> instructions = methodBody.Instructions;

            IEnumerable<Instruction> setterCallInstructions = instructions.Where(
                instruction =>
                    (instruction.OpCode == OpCodes.Call
                        || instruction.OpCode == OpCodes.Calli
                        || instruction.OpCode == OpCodes.Callvirt)
                    && instruction.Operand is MethodReference reference
                    && reference.FullName == propertyDefinition.SetMethod.FullName);
            FieldReference backingField = propertyDefinition.FindBackingField();

            foreach (Instruction setterCallInstruction in setterCallInstructions)
            {
                setterCallInstruction.OpCode = OpCodes.Stfld;
                setterCallInstruction.Operand = backingField;

                LogInfo(
                    $"Changed the property setter call in '{methodDefinition.FullName}' to set the backing"
                    + $" field '{backingField.FullName}' instead to prevent a potential infinite loop.");
            }

            methodBody.OptimizeMacros();
        }

        private void InsertSetMethodCallIntoPropertySetter(
            PropertyDefinition propertyDefinition,
            MethodReference methodReference,
            ICustomAttribute attribute)
        {
            MethodBody methodBody = propertyDefinition.SetMethod.Body;
            Collection<Instruction> instructions = methodBody.Instructions;

            FieldReference backingField = propertyDefinition.FindBackingField();
            Instruction storeInstruction = instructions.First(
                instruction => instruction.OpCode == OpCodes.Stfld
                    && (instruction.Operand as FieldReference)?.FullName == backingField.FullName);

            Instruction targetInstruction;
            int instructionIndex;
            bool needsPlayingCheck = _isCompilingForEditor;
            bool needsActiveAndEnabledCheck =
                methodReference.DeclaringType.Resolve().IsSubclassOf(_behaviourReference);

            /*
             if (Application.isPlaying)       // Only if compiling for Editor
             {
                 if (this.isActiveAndEnabled) // Only if in a Behaviour
                 {
                     this.Method();
                 }
             }
             */

            if (attribute.AttributeType.FullName == _fullBeforeChangeAttributeName)
            {
                targetInstruction = storeInstruction.Previous.Previous;
                instructionIndex = instructions.IndexOf(targetInstruction) - 1;

                Instruction testInstruction = targetInstruction.Previous;

                void TryFindExistingCheck(ref bool needsCheck, MemberReference checkGetterReference)
                {
                    if (!needsCheck)
                    {
                        return;
                    }

                    while (testInstruction != null)
                    {
                        if (testInstruction.OpCode == OpCodes.Call
                            && (testInstruction.Operand as MethodReference)?.FullName == checkGetterReference.FullName)
                        {
                            needsCheck = false;
                            return;
                        }

                        testInstruction = testInstruction.Previous;
                    }

                    while (testInstruction != null
                        && (testInstruction.OpCode == OpCodes.Brfalse || testInstruction.OpCode == OpCodes.Brfalse_S))
                    {
                        testInstruction = testInstruction.Next;
                    }

                    if (testInstruction != null)
                    {
                        instructionIndex = instructions.IndexOf(testInstruction) - 1;
                    }
                }

                TryFindExistingCheck(ref needsActiveAndEnabledCheck, _isActiveAndEnabledGetterReference);
                TryFindExistingCheck(ref needsPlayingCheck, _isApplicationPlayingGetterReference);
            }
            else
            {
                targetInstruction = storeInstruction.Next;
                instructionIndex = instructions.IndexOf(targetInstruction) - 1;

                Instruction testInstruction = storeInstruction.Next;

                void TryFindExistingCheck(ref bool needsCheck, MemberReference checkGetterReference)
                {
                    if (!needsCheck)
                    {
                        return;
                    }

                    while (testInstruction != null)
                    {
                        if (testInstruction.OpCode == OpCodes.Call
                            && (testInstruction.Operand as MethodReference)?.FullName == checkGetterReference.FullName)
                        {
                            needsCheck = false;
                            instructionIndex = instructions.IndexOf(testInstruction) + 1;
                            return;
                        }

                        testInstruction = testInstruction.Next;
                    }
                }

                TryFindExistingCheck(ref needsPlayingCheck, _isApplicationPlayingGetterReference);
                TryFindExistingCheck(ref needsActiveAndEnabledCheck, _isActiveAndEnabledGetterReference);
            }

            if (needsPlayingCheck)
            {
                // Call Application.isPlaying getter
                instructions.Insert(
                    ++instructionIndex,
                    Instruction.Create(OpCodes.Call, _isApplicationPlayingGetterReference));
                // Bail out if false
                instructions.Insert(++instructionIndex, Instruction.Create(OpCodes.Brfalse, targetInstruction));
            }

            if (needsActiveAndEnabledCheck)
            {
                // Load this (for getter call)
                instructions.Insert(++instructionIndex, Instruction.Create(OpCodes.Ldarg_0));
                // Call Behaviour.isActiveAndEnabled getter
                instructions.Insert(
                    ++instructionIndex,
                    Instruction.Create(OpCodes.Call, _isActiveAndEnabledGetterReference));
                // Bail out if false
                instructions.Insert(++instructionIndex, Instruction.Create(OpCodes.Brfalse, targetInstruction));
            }

            // Load this (for method call)
            instructions.Insert(++instructionIndex, Instruction.Create(OpCodes.Ldarg_0));
            // Call method
            instructions.Insert(
                ++instructionIndex,
                Instruction.Create(OpCodes.Callvirt, methodReference.CreateGenericMethodIfNeeded()));

            methodBody.OptimizeMacros();

            LogInfo(
                $"Inserted a call to the method '{methodReference.FullName}' into"
                + $" the setter of the property '{propertyDefinition.FullName}'.");
        }
    }
}
