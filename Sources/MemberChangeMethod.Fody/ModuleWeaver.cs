namespace Malimbe.MemberChangeMethod.Fody
{
    using System;
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
            List<Instruction> storeInstructions = instructions.Where(
                    instruction => instruction.OpCode == OpCodes.Stfld
                        && (instruction.Operand as FieldReference)?.FullName == backingField.FullName)
                .ToList();

            foreach (Instruction storeInstruction in storeInstructions)
            {
                Instruction targetInstruction;
                int instructionIndex;
                bool needsPlayingCheck = _isCompilingForEditor;
                bool needsActiveAndEnabledCheck =
                    methodReference.DeclaringType.Resolve().IsSubclassOf(_behaviourReference);
                bool needsInstanceCheck =
                    methodReference.DeclaringType.FullName != propertyDefinition.DeclaringType.FullName;

                /*
                 if (Application.isPlaying)                         // Only if compiling for Editor
                 {
                     if (this.isActiveAndEnabled)                   // Only if in a Behaviour
                     {
                         if (this is methodReference.DeclaringType) // Only if the property is defined in a superclass
                         {
                             this.Method();
                         }
                     }
                 }
                 */

                bool IsPlayingCheck(Instruction instruction) =>
                    instruction.OpCode == OpCodes.Call
                    && (instruction.Operand as MethodReference)?.FullName
                    == _isApplicationPlayingGetterReference.FullName;

                bool IsActiveAndEnabledCheck(Instruction instruction) =>
                    instruction.OpCode == OpCodes.Call
                    && (instruction.Operand as MethodReference)?.FullName
                    == _isActiveAndEnabledGetterReference.FullName;

                bool IsInstanceCheck(Instruction instruction) =>
                    instruction.OpCode == OpCodes.Isinst
                    && (instruction.Operand as TypeReference)?.FullName == methodReference.DeclaringType.FullName;

                if (attribute.AttributeType.FullName == _fullBeforeChangeAttributeName)
                {
                    targetInstruction = storeInstruction.Previous.Previous;
                    instructionIndex = instructions.IndexOf(targetInstruction) - 1;

                    Instruction testInstruction = targetInstruction.Previous;

                    void TryFindExistingCheck(ref bool needsCheck, Func<Instruction, bool> predicate)
                    {
                        if (!needsCheck)
                        {
                            return;
                        }

                        while (testInstruction != null)
                        {
                            if (predicate(testInstruction))
                            {
                                needsCheck = false;
                                return;
                            }

                            testInstruction = testInstruction.Previous;
                        }

                        while (testInstruction != null
                            && (testInstruction.OpCode == OpCodes.Brfalse
                                || testInstruction.OpCode == OpCodes.Brfalse_S))
                        {
                            testInstruction = testInstruction.Next;
                        }

                        if (testInstruction != null)
                        {
                            instructionIndex = instructions.IndexOf(testInstruction) - 1;
                        }
                    }

                    TryFindExistingCheck(ref needsInstanceCheck, IsInstanceCheck);
                    TryFindExistingCheck(ref needsActiveAndEnabledCheck, IsActiveAndEnabledCheck);
                    TryFindExistingCheck(ref needsPlayingCheck, IsPlayingCheck);
                }
                else
                {
                    targetInstruction = storeInstruction.Next;
                    instructionIndex = instructions.IndexOf(targetInstruction) - 1;

                    Instruction testInstruction = storeInstruction.Next;

                    void TryFindExistingCheck(ref bool needsCheck, Func<Instruction, bool> predicate)
                    {
                        if (!needsCheck)
                        {
                            return;
                        }

                        while (testInstruction != null)
                        {
                            if (predicate(testInstruction))
                            {
                                needsCheck = false;
                                instructionIndex = instructions.IndexOf(testInstruction) + 1;
                                return;
                            }

                            testInstruction = testInstruction.Next;
                        }
                    }

                    TryFindExistingCheck(ref needsPlayingCheck, IsPlayingCheck);
                    TryFindExistingCheck(ref needsActiveAndEnabledCheck, IsActiveAndEnabledCheck);
                    TryFindExistingCheck(ref needsInstanceCheck, IsInstanceCheck);
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

                Instruction nopInstruction = null;
                if (needsInstanceCheck)
                {
                    // if (this is setMethodReference.DeclaringType) { ...
                    nopInstruction = Instruction.Create(OpCodes.Nop);

                    // Load this (for instance check)
                    instructions.Insert(++instructionIndex, Instruction.Create(OpCodes.Ldarg_0));
                    // Check if instance of that type
                    instructions.Insert(
                        ++instructionIndex,
                        Instruction.Create(OpCodes.Isinst, methodReference.DeclaringType));
                    // Jump if false
                    instructions.Insert(++instructionIndex, Instruction.Create(OpCodes.Brfalse, nopInstruction));
                }

                // Load this (for method call)
                instructions.Insert(++instructionIndex, Instruction.Create(OpCodes.Ldarg_0));
                // Call method
                instructions.Insert(
                    ++instructionIndex,
                    Instruction.Create(OpCodes.Callvirt, methodReference.CreateGenericMethodIfNeeded()));

                if (nopInstruction != null)
                {
                    // ... }

                    // Nop to jump to (see above)
                    instructions.Insert(++instructionIndex, nopInstruction);
                }

                methodBody.OptimizeMacros();

                LogInfo(
                    $"Inserted a call to the method '{methodReference.FullName}' into"
                    + $" the setter of the property '{propertyDefinition.FullName}'.");
            }
        }
    }
}
