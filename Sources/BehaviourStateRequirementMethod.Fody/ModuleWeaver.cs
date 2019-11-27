namespace Malimbe.BehaviourStateRequirementMethod.Fody
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
        private static readonly string _fullAttributeName = typeof(RequiresBehaviourStateAttribute).FullName;

        private TypeReference _behaviourTypeReference;
        private MethodReference _getGameObjectMethodReference;
        private MethodReference _getActiveSelfMethodReference;
        private MethodReference _getActiveInHierarchyMethodReference;
        private MethodReference _getIsActiveAndEnabledMethodReference;
        private MethodReference _getEnabledMethodReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            IEnumerable<MethodDefinition> methodDefinitions =
                ModuleDefinition.GetTypes().SelectMany(definition => definition.Methods);
            foreach (MethodDefinition methodDefinition in methodDefinitions)
            {
                if (!FindAndRemoveAttribute(methodDefinition, out CustomAttribute attribute))
                {
                    continue;
                }

                GameObjectActivity gameObjectActivity = (GameObjectActivity)attribute.ConstructorArguments[0].Value;
                bool behaviourNeedsToBeEnabled = (bool)attribute.ConstructorArguments[1].Value;

                if (gameObjectActivity == GameObjectActivity.None && !behaviourNeedsToBeEnabled)
                {
                    LogWarning(
                        $"The method '{methodDefinition.FullName}' is annotated to require a Behaviour state"
                        + " but the attribute constructor arguments result in no action being taken.");
                    continue;
                }

                MethodBody body = methodDefinition.Body;
                Collection<Instruction> instructions = body.Instructions;
                if (instructions.Count == 0)
                {
                    LogWarning(
                        $"The method '{methodDefinition.FullName}' is annotated to require a Behaviour state"
                        + " but the method has no instructions in its body and thus no action is being taken.");
                    continue;
                }

                body.SimplifyMacros();
                InsertInstructions(
                    body,
                    methodDefinition,
                    instructions,
                    gameObjectActivity,
                    behaviourNeedsToBeEnabled);
                body.OptimizeMacros();
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "UnityEngine";
        }

        private void FindReferences()
        {
            MethodReference ImportPropertyGetter(TypeDefinition typeDefinition, string propertyName) =>
                ModuleDefinition.ImportReference(
                    typeDefinition.Properties.Single(definition => definition.Name == propertyName).GetMethod);

            TypeDefinition behaviourTypeDefinition = FindType("UnityEngine.Behaviour");
            TypeDefinition gameObjectTypeDefinition = FindType("UnityEngine.GameObject");

            _behaviourTypeReference = ModuleDefinition.ImportReference(behaviourTypeDefinition);
            _getGameObjectMethodReference = ImportPropertyGetter(FindType("UnityEngine.Component"), "gameObject");
            _getActiveSelfMethodReference = ImportPropertyGetter(gameObjectTypeDefinition, "activeSelf");
            _getActiveInHierarchyMethodReference = ImportPropertyGetter(gameObjectTypeDefinition, "activeInHierarchy");
            _getIsActiveAndEnabledMethodReference =
                ImportPropertyGetter(behaviourTypeDefinition, "isActiveAndEnabled");
            _getEnabledMethodReference = ImportPropertyGetter(behaviourTypeDefinition, "enabled");
        }

        private bool FindAndRemoveAttribute(MethodDefinition methodDefinition, out CustomAttribute foundAttribute)
        {
            foundAttribute = methodDefinition.CustomAttributes.SingleOrDefault(
                attribute => attribute.AttributeType.FullName == _fullAttributeName);
            if (foundAttribute == null)
            {
                return false;
            }

            methodDefinition.CustomAttributes.Remove(foundAttribute);
            LogInfo($"Removed the attribute '{_fullAttributeName}' from the method '{methodDefinition.FullName}'.");

            if (methodDefinition.DeclaringType.IsSubclassOf(_behaviourTypeReference))
            {
                return true;
            }

            LogError(
                $"The method '{methodDefinition.FullName}' is annotated to require a Behaviour state"
                + $" but the declaring type doesn't derive from '{_behaviourTypeReference.FullName}'.");
            return false;
        }

        private void InsertInstructions(
            MethodBody body,
            MethodReference methodDefinition,
            IList<Instruction> instructions,
            GameObjectActivity gameObjectActivity,
            bool behaviourNeedsToBeEnabled)
        {
            Instruction earlyReturnInstruction;

            if (methodDefinition.ReturnType.FullName != TypeSystem.VoidReference.FullName)
            {
                // Create new variable to return a value
                VariableDefinition variableDefinition = new VariableDefinition(methodDefinition.ReturnType);
                body.Variables.Add(variableDefinition);
                // Set variable to default value
                body.InitLocals = true;

                // Load variable
                Instruction loadInstruction = Instruction.Create(OpCodes.Ldloc, variableDefinition);
                instructions.Add(loadInstruction);
                // Return
                instructions.Add(Instruction.Create(OpCodes.Ret));

                earlyReturnInstruction = loadInstruction;
            }
            else
            {
                earlyReturnInstruction = instructions.Last(instruction => instruction.OpCode == OpCodes.Ret);
            }

            int index = -1;

            if (gameObjectActivity != GameObjectActivity.None)
            {
                // Load this (for gameObject getter call)
                instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
                // Call gameObject getter
                instructions.Insert(++index, Instruction.Create(OpCodes.Callvirt, _getGameObjectMethodReference));

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (gameObjectActivity)
                {
                    case GameObjectActivity.Self:
                        // Call activeSelf getter
                        instructions.Insert(
                            ++index,
                            Instruction.Create(OpCodes.Callvirt, _getActiveSelfMethodReference));
                        break;
                    case GameObjectActivity.InHierarchy:
                        // Call activeInHierarchy getter
                        instructions.Insert(
                            ++index,
                            Instruction.Create(OpCodes.Callvirt, _getActiveInHierarchyMethodReference));
                        break;
                }

                AddEarlyReturnInstruction(instructions, ref index, earlyReturnInstruction);

                if (behaviourNeedsToBeEnabled)
                {
                    // Load this (for enabled getter call)
                    instructions.Insert(++index, Instruction.Create(OpCodes.Ldarg_0));
                    // Call enabled getter
                    instructions.Insert(++index, Instruction.Create(OpCodes.Callvirt, _getEnabledMethodReference));

                    AddEarlyReturnInstruction(instructions, ref index, earlyReturnInstruction);
                }
            }

            LogInfo($"Added (an) early return(s) to the method '{methodDefinition.FullName}'.");
        }

        private static void AddEarlyReturnInstruction(
            IList<Instruction> instructions,
            ref int index,
            Instruction earlyReturnInstruction) =>
            // Return early if false
            instructions.Insert(++index, Instruction.Create(OpCodes.Brfalse, earlyReturnInstruction));
    }
}
