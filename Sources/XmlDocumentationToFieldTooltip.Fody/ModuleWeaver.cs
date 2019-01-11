namespace Malimbe.XmlDocumentationToFieldTooltip.Fody
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::Fody;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private static readonly Regex _documentedIdentifierRegex = new Regex(
            @"<\s*summary\s*>(?'summary'(?:\s*\/{3}.*)*)<\s*\/\s*summary\s*>(?:\s*\/{3}.*|\s|\[[\p{L}\p{M}\p{N}\p{P}\p{S}\p{Z}\p{C}]*?\])*[\s\w<>.\[\]?]*\s(?'identifier'\w+)(?:\((?'methodParameters'.*)\))?",
            RegexOptions.Compiled);
        private static readonly Regex _xmlTagBodyCleanUpRegex = new Regex(
            @"<\s*\w*\b.*?""(?'reference'.*)""\s*\/\s*>",
            RegexOptions.Compiled);

        private TypeDefinition _attributeDefinition;
        private MethodReference _attributeConstructorReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();

            List<Regex> namespaceFilters = FindNamespaceFilters().ToList();
            IEnumerable<TypeDefinition> typeDefinitions = ModuleDefinition.Types.Where(
                definition =>
                    definition.HasFields
                    && (namespaceFilters.Count == 0
                        || namespaceFilters.Any(regex => regex.IsMatch(definition.Namespace))));
            foreach (TypeDefinition typeDefinition in typeDefinitions)
            {
                List<FieldDefinition> fieldDefinitions = FindFieldsToAnnotate(typeDefinition).ToList();
                if (fieldDefinitions.Count == 0)
                {
                    continue;
                }

                IReadOnlyDictionary<string, string> summariesByIdentifierName = ParseSourceFileXmlDocumentation(typeDefinition);
                if (summariesByIdentifierName.Count == 0)
                {
                    continue;
                }

                foreach (FieldDefinition fieldDefinition in fieldDefinitions)
                {
                    if (summariesByIdentifierName.TryGetValue(fieldDefinition.Name, out string summary))
                    {
                        AnnotateField(fieldDefinition, summary);
                    }
                }
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "UnityEngine";
            yield return "netstandard";
            yield return "mscorlib";
        }

        private void FindReferences()
        {
            string typeName = Config?.Attribute("FullAttributeName")?.Value ?? "UnityEngine.TooltipAttribute";
            TypeDefinition typeDefinition = FindType(typeName);
            if (typeDefinition == null)
            {
                throw new WeavingException($"No attribute with the name '{typeName}' was found.");
            }

            MethodDefinition constructorMethodInfo = typeDefinition.Methods.SingleOrDefault(
                definition => definition.IsConstructor
                    && definition.Parameters.SingleOrDefault()?.ParameterType.FullName
                    == TypeSystem.StringReference.FullName);
            if (constructorMethodInfo == null)
            {
                throw new WeavingException(
                    $"The attribute '{typeName}' doesn't offer a constructor that takes"
                    + $" a '{TypeSystem.StringReference.FullName}' and nothing else.");
            }

            _attributeDefinition = ModuleDefinition.ImportReference(typeDefinition).Resolve();
            _attributeConstructorReference = ModuleDefinition.ImportReference(constructorMethodInfo);
        }

        private IEnumerable<Regex> FindNamespaceFilters() =>
            Config?.Elements("NamespaceFilter")
                .Select(xElement => xElement.Value)
                .Select(filter => new Regex(filter, RegexOptions.Compiled))
                .ToList()
            ?? Enumerable.Empty<Regex>();

        private static IEnumerable<FieldDefinition> FindFieldsToAnnotate(TypeDefinition typeDefinition) =>
            typeDefinition.Fields.Where(
                definition =>
                    definition.IsPublic
                    || definition.CustomAttributes.Any(
                        attribute => attribute.AttributeType.FullName == "UnityEngine.SerializeField"));

        private IReadOnlyDictionary<string, string> ParseSourceFileXmlDocumentation(TypeDefinition typeDefinition)
        {
            SequencePoint sequencePoint = typeDefinition.Methods.Select(definition => definition.DebugInformation)
                .SelectMany(information => information.SequencePoints)
                .FirstOrDefault();
            if (sequencePoint == null)
            {
                return new Dictionary<string, string>();
            }

            string documentUrl = sequencePoint.Document.Url;
            string sourceCode = File.ReadAllText(documentUrl);
            Dictionary<string, string> summariesByIdentifierName = new Dictionary<string, string>();

            foreach (Match match in _documentedIdentifierRegex.Matches(sourceCode))
            {
                if (match.Groups["methodParameters"].Success)
                {
                    continue;
                }

                string identifierName = match.Groups["identifier"].Value;
                if (summariesByIdentifierName.ContainsKey(identifierName))
                {
                    LogError(
                        $"There are at least two identifiers called '{identifierName}' in '{typeDefinition.FullName}'."
                        + " Only a single identifier name per source code file is currently supported. The first occurrence of"
                        + $" the identifier name will be annotated using the attribute '{_attributeDefinition.FullName}'.");
                    continue;
                }

                string body = match.Groups["summary"].Value.Trim(' ', '/', '\r', '\n');
                string summary = _xmlTagBodyCleanUpRegex.Replace(body, "${reference}");
                summariesByIdentifierName[identifierName] = summary;
            }

            return summariesByIdentifierName;
        }

        private void AnnotateField(FieldDefinition fieldDefinition, string summary)
        {
            CustomAttribute existingAttribute = fieldDefinition.CustomAttributes.SingleOrDefault(
                attribute => attribute.AttributeType.FullName == _attributeDefinition.FullName);
            bool didAttributeAlreadyExist = existingAttribute != null;
            if (didAttributeAlreadyExist)
            {
                LogWarning(
                    $"The field '{fieldDefinition.FullName}' is documented using XML documentation comments"
                    + $" but already uses the attribute '{_attributeDefinition.FullName}'."
                    + " The attribute is replaced.");
                fieldDefinition.CustomAttributes.Remove(existingAttribute);
            }

            CustomAttribute newAttribute = new CustomAttribute(_attributeConstructorReference)
            {
                ConstructorArguments =
                {
                    new CustomAttributeArgument(TypeSystem.StringReference, summary)
                }
            };
            fieldDefinition.CustomAttributes.Add(newAttribute);

            LogInfo(
                $"{(didAttributeAlreadyExist ? "Updated" : "Added")} the attribute"
                + $" '{_attributeDefinition.FullName}' for the field '{fieldDefinition.FullName}'"
                + " using the field's XML documentation comments.");
        }
    }
}
