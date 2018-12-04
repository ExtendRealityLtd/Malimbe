namespace Malimbe.FieldToProperty.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Fody;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;
    using Mono.Collections.Generic;

    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private static readonly string _attributeName = typeof(BacksPropertyAttribute).FullName;

        private MethodReference _serializeFieldAttributeConstructorReference;
        private MethodReference _compilerGeneratedAttributeConstructorReference;
        private MethodReference _reflectionMethodFromHandleMethodReference;
        private MethodReference _expressionPropertyMethodReference;
        private TypeReference _reflectionMethodInfoTypeReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            List<TypeDefinition> typeDefinitions = ModuleDefinition.Types.Where(
                    definition => !definition.IsInterface && !definition.IsEnum && !definition.IsValueType)
                .ToList();
            IEnumerable<FieldDefinition> fieldDefinitions = typeDefinitions.SelectMany(definition => definition.Fields)
                .Where(definition => RemoveAttribute(definition) && ShouldWeaveField(definition));
            List<MethodDefinition> methodDefinitions = typeDefinitions.SelectMany(definition => definition.Methods)
                .Where(definition => !definition.IsAbstract && definition.HasBody)
                .ToList();

            foreach (FieldDefinition fieldDefinition in fieldDefinitions)
            {
                HideField(fieldDefinition);
                PropertyDefinition propertyDefinition = CreateProperty(fieldDefinition);
                if (propertyDefinition != null)
                {
                    ReplaceFieldUsage(fieldDefinition, propertyDefinition, methodDefinitions);
                }
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
            _compilerGeneratedAttributeConstructorReference =
                FindTypeConstructorReference("System.Runtime.CompilerServices.CompilerGeneratedAttribute");

            _reflectionMethodFromHandleMethodReference = ModuleDefinition.ImportReference(
                FindType("System.Reflection.MethodBase")
                    .Methods.First(definition => definition.Name == "GetMethodFromHandle"));
            _expressionPropertyMethodReference = ModuleDefinition.ImportReference(
                FindType("System.Linq.Expressions.Expression")
                    .Methods.First(
                        definition =>
                            definition.Name == "Property"
                            && definition.Parameters.Last().ParameterType.Name == "MethodInfo"));
            _reflectionMethodInfoTypeReference =
                ModuleDefinition.ImportReference(FindType("System.Reflection.MethodInfo"));
        }

        private bool RemoveAttribute(FieldDefinition fieldDefinition)
        {
            List<CustomAttribute> attributes = fieldDefinition.CustomAttributes.Where(
                    attribute => attribute.AttributeType.FullName == _attributeName)
                .ToList();
            foreach (CustomAttribute attribute in attributes)
            {
                fieldDefinition.CustomAttributes.Remove(attribute);
            }

            if (attributes.Capacity > 0)
            {
                LogInfo($"Removed attributes on '{fieldDefinition.FullName}'.");
            }

            return attributes.Count > 0;
        }

        private bool ShouldWeaveField(FieldDefinition fieldDefinition)
        {
            if (fieldDefinition.IsStatic)
            {
                LogError(
                    $"The field '{fieldDefinition.FullName}' is marked to be processed but is static."
                    + " Only instance fields are supported.");
                return false;
            }

            if (!fieldDefinition.IsPublic)
            {
                LogError(
                    $"The field '{fieldDefinition.FullName}' is marked to be processed but isn't public."
                    + " Only public fields are supported.");
                return false;
            }

            if (fieldDefinition.DeclaringType.HasGenericParameters)
            {
                LogError(
                    $"The field '{fieldDefinition.FullName}' is marked to be processed but is generic."
                    + " Only non-generic fields are currently supported.");
                return false;
            }

            return true;
        }

        private void HideField(FieldDefinition fieldDefinition)
        {
            fieldDefinition.IsPublic = false;
            fieldDefinition.IsPrivate = true;
            fieldDefinition.CustomAttributes.Add(new CustomAttribute(_serializeFieldAttributeConstructorReference));
        }

        private PropertyDefinition CreateProperty(FieldDefinition fieldDefinition)
        {
            char firstNameCharacter = char.IsUpper(fieldDefinition.Name, 0)
                ? char.ToLowerInvariant(fieldDefinition.Name[0])
                : char.ToUpperInvariant(fieldDefinition.Name[0]);
            PropertyDefinition propertyDefinition = new PropertyDefinition(
                $"{firstNameCharacter}{fieldDefinition.Name.Substring(1)}",
                PropertyAttributes.None,
                fieldDefinition.FieldType);
            if (fieldDefinition.DeclaringType.Properties.Any(
                definition => definition.FullName == propertyDefinition.FullName))
            {
                LogError(
                    $"The property '{propertyDefinition.FullName}' already exists."
                    + $" The field '{fieldDefinition.FullName}' will not be processed.");
                return null;
            }

            propertyDefinition.CustomAttributes.Add(
                new CustomAttribute(_compilerGeneratedAttributeConstructorReference));
            fieldDefinition.DeclaringType.Properties.Add(propertyDefinition);

            AddPropertyGetter(propertyDefinition, fieldDefinition);
            AddPropertySetter(propertyDefinition, fieldDefinition);

            LogInfo(
                $"Created a property '{propertyDefinition.FullName}'"
                + $" for the field '{fieldDefinition.FullName}'.");

            return propertyDefinition;
        }

        private void AddPropertyGetter(PropertyDefinition propertyDefinition, FieldReference fieldReference)
        {
            MethodDefinition getterDefinition = new MethodDefinition(
                $"get_{propertyDefinition.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyDefinition.PropertyType)
            {
                SemanticsAttributes = MethodSemanticsAttributes.Getter
            };
            if (propertyDefinition.DeclaringType.Methods.Any(
                definition => definition.FullName == getterDefinition.FullName))
            {
                LogError($"The getter method '{getterDefinition.FullName}' already exists and won't be processed.");
                return;
            }

            getterDefinition.CustomAttributes.Add(
                new CustomAttribute(_compilerGeneratedAttributeConstructorReference));
            propertyDefinition.DeclaringType.Methods.Add(getterDefinition);

            Collection<Instruction> instructions = getterDefinition.Body.Instructions;

            // return this.field;

            // Load this
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            // Load field
            instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldReference));
            // Return
            instructions.Add(Instruction.Create(OpCodes.Ret));

            propertyDefinition.GetMethod = getterDefinition;
        }

        private void AddPropertySetter(PropertyDefinition propertyDefinition, FieldDefinition fieldDefinition)
        {
            if (fieldDefinition.IsInitOnly)
            {
                return;
            }

            MethodDefinition setterDefinition = new MethodDefinition(
                $"set_{propertyDefinition.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                TypeSystem.VoidReference)
            {
                SemanticsAttributes = MethodSemanticsAttributes.Setter
            };
            if (propertyDefinition.DeclaringType.Methods.Any(
                definition => definition.FullName == setterDefinition.FullName))
            {
                LogError($"The setter method '{setterDefinition.FullName}' already exists and won't be processed.");
                return;
            }

            setterDefinition.CustomAttributes.Add(
                new CustomAttribute(_compilerGeneratedAttributeConstructorReference));
            setterDefinition.Parameters.Add(
                new ParameterDefinition("value", ParameterAttributes.None, propertyDefinition.PropertyType));
            propertyDefinition.DeclaringType.Methods.Add(setterDefinition);

            Collection<Instruction> instructions = setterDefinition.Body.Instructions;
            MethodDefinition setMethodDefinition = FindSetMethodDefinition(propertyDefinition);
            if (setMethodDefinition != null)
            {
                LogInfo(
                    $"Inserted a call to the method '{setMethodDefinition.FullName}' into"
                    + $" the setter of property '{propertyDefinition.FullName}'.");

                // this.field = this.setMethod(this.field, value);

                // Load this (for field store)
                instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                // Load this (for method call)
                instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                // Load this (for field load)
                instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                // Load field
                instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
                // Load value
                instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                // Call setMethod
                instructions.Add(Instruction.Create(OpCodes.Callvirt, setMethodDefinition));
                // Store into field
                instructions.Add(Instruction.Create(OpCodes.Stfld, fieldDefinition));
            }
            else
            {
                // this.field = value;

                // Load this (for field store)
                instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                // Load value
                instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                // Store into field
                instructions.Add(Instruction.Create(OpCodes.Stfld, fieldDefinition));
            }

            // Return
            instructions.Add(Instruction.Create(OpCodes.Ret));

            propertyDefinition.SetMethod = setterDefinition;
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
                        + " generated property by name but the signature doesn't match."
                        + $" The expected signature is '{expectedDefinition.FullName}'.");
                }
            }

            return foundDefinition;
        }

        private void ReplaceFieldUsage(
            FieldDefinition fieldDefinition,
            PropertyDefinition propertyDefinition,
            IEnumerable<MethodDefinition> methodDefinitions)
        {
            foreach (MethodDefinition methodDefinition in methodDefinitions)
            {
                MethodBody body = methodDefinition.Body;
                body.SimplifyMacros();

                foreach (Instruction instruction in body.Instructions)
                {
                    if (!(instruction.Operand is FieldDefinition operandFieldDefinition)
                        || operandFieldDefinition != fieldDefinition)
                    {
                        continue;
                    }

                    // Load value of field that is on evaluation stack
                    if (instruction.OpCode == OpCodes.Ldfld)
                    {
                        // Call getter
                        instruction.OpCode = OpCodes.Callvirt;
                        instruction.Operand = propertyDefinition.GetMethod;
                    }
                    // Find address of field that is on evaluation stack
                    else if (instruction.OpCode == OpCodes.Ldflda)
                    {
                        Instruction instruction1 = instruction.Next;
                        if ((instruction1.OpCode == OpCodes.Call || instruction1.OpCode == OpCodes.Calli)
                            && instruction1.Operand is MethodReference methodReference
                            && methodReference.Parameters.Any(
                                definition => definition.IsOut || definition.ParameterType.IsByReference))
                        {
                            LogError(
                                $"The method '{methodDefinition.FullName}' uses the field '{fieldDefinition.FullName}'"
                                + " as a 'ref' or 'out' parameter. Only direct usages of fields are currently supported.");
                            continue;
                        }

                        body.InitLocals = true;
                        VariableDefinition variableDefinition =
                            new VariableDefinition(propertyDefinition.PropertyType);
                        body.Variables.Add(variableDefinition);

                        // Call getter
                        instruction.OpCode = OpCodes.Callvirt;
                        instruction.Operand = propertyDefinition.GetMethod;

                        int index = body.Instructions.IndexOf(instruction);
                        // Pop value from evaluation stack and store into variable
                        body.Instructions.Insert(++index, Instruction.Create(OpCodes.Stloc, variableDefinition));
                        // Load variable's address onto evaluation stack
                        body.Instructions.Insert(++index, Instruction.Create(OpCodes.Ldloca, variableDefinition));
                    }
                    // Push metadata token's runtime representation onto evaluation stack
                    else if (instruction.OpCode == OpCodes.Ldtoken)
                    {
                        // Push getter token's runtime representation
                        instruction.Operand = propertyDefinition.GetMethod;

                        Instruction nextInstruction = instruction.Next;
                        Instruction afterNextInstruction = nextInstruction?.Next;

                        if (nextInstruction == null
                            || afterNextInstruction == null
                            || nextInstruction.OpCode != OpCodes.Call
                            || afterNextInstruction.OpCode != OpCodes.Call
                            || !(nextInstruction.Operand is MethodReference nextMethodReference)
                            || !(afterNextInstruction.Operand is MethodReference afterNextMethodReference)
                            || nextMethodReference.FullName
                            != "System.Reflection.FieldInfo System.Reflection.FieldInfo::GetFieldFromHandle(System.RuntimeFieldHandle)"
                            || afterNextMethodReference.FullName
                            != "System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression::Field(System.Linq.Expressions.Expression,System.Reflection.FieldInfo)"
                        )
                        {
                            continue;
                        }

                        // Call GetMethodFromHandle to get a handle to the getter
                        nextInstruction.Operand = _reflectionMethodFromHandleMethodReference;
                        // Call Property expression to call getter
                        afterNextInstruction.Operand = _expressionPropertyMethodReference;

                        // Cast expression result to MethodInfo
                        int index = body.Instructions.IndexOf(afterNextInstruction);
                        body.Instructions.Insert(
                            index,
                            Instruction.Create(OpCodes.Castclass, _reflectionMethodInfoTypeReference));
                    }
                    // Store value on evaluation stack into field
                    else if (instruction.OpCode == OpCodes.Stfld
                        && !fieldDefinition.IsInitOnly
                        /*
                         * Setting the field in the declaring type's constructor is needed because the type isn't yet
                         * fully initialized.
                         */
                        && (!methodDefinition.IsConstructor
                            || propertyDefinition.DeclaringType != methodDefinition.DeclaringType))
                    {
                        // Call setter
                        instruction.OpCode = OpCodes.Callvirt;
                        instruction.Operand = propertyDefinition.SetMethod;
                    }
                }

                body.OptimizeMacros();
            }

            LogInfo($"Replaced any field usage (except for constructors) of the field '{fieldDefinition.FullName}'.");
        }
    }
}
