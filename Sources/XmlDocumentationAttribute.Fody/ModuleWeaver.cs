namespace Malimbe.XmlDocumentationAttribute.Fody
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::Fody;
    using Malimbe.Shared;
    using Mono.Cecil;

    // ReSharper disable once UnusedMember.Global
    public sealed class ModuleWeaver : BaseModuleWeaver
    {
        private const string IdentifierReplacementFormatElementName = "IdentifierReplacementFormat";
        private const string DefaultIdentifierReplacementFormat = "{0}";

        private static readonly string _fullAttributeName = typeof(DocumentedByXmlAttribute).FullName;
        private static readonly Regex _documentedIdentifierRegex = new Regex(
            @"<\s*summary\s*>(?'summary'(?:\s*\/{3}.*)*)<\s*\/\s*summary\s*>(?:\s*\/{3}.*|\s|\[[\p{L}\p{M}\p{N}\p{P}\p{S}\p{Z}\p{C}]*?\])*[\s\w<>.\[\]?]*\s(?'identifier'\w+)(?:\((?'methodParameters'.*)\))?",
            RegexOptions.Compiled);
        private static readonly Regex _xmlTagBodyCleanUpRegex = new Regex(
            @"<\s*\w*\b.*?""(?'identifier'.*)""\s*\/\s*>",
            RegexOptions.Compiled);

        private TypeDefinition _attributeDefinition;
        private MethodReference _attributeConstructorReference;

        public override bool ShouldCleanReference =>
            true;

        public override void Execute()
        {
            FindReferences();
            string identifierReplacementFormat = GetIdentifierReplacementFormat();

            IEnumerable<TypeDefinition> typeDefinitions = ModuleDefinition.Types.Where(
                definition => definition.HasFields);
            foreach (TypeDefinition typeDefinition in typeDefinitions)
            {
                List<FieldDefinition> fieldDefinitions = typeDefinition.Fields.Where(FindAndRemoveAttribute).ToList();
                if (fieldDefinitions.Count == 0)
                {
                    continue;
                }

                IReadOnlyDictionary<string, List<string>> summariesByIdentifierName =
                    ParseSourceFileXmlDocumentation(typeDefinition, identifierReplacementFormat);
                foreach (FieldDefinition fieldDefinition in fieldDefinitions)
                {
                    if (!summariesByIdentifierName.TryGetValue(fieldDefinition.Name, out List<string> summaries))
                    {
                        PropertyDefinition propertyDefinition =
                            fieldDefinition.DeclaringType.Properties?.FirstOrDefault(
                                definition => definition.FindBackingField()?.Name == fieldDefinition.Name);
                        if (propertyDefinition != null)
                        {
                            summariesByIdentifierName.TryGetValue(propertyDefinition.Name, out summaries);
                        }
                    }

                    if (summaries == null)
                    {
                        LogError(
                            $"The field '{fieldDefinition.FullName}' is annotated to be documented by XML"
                            + " documentation comments but none were found.");
                        continue;
                    }

                    if (summaries.Count > 1)
                    {
                        LogError(
                            $"There are at least two identifiers called '{fieldDefinition.Name}' in '{typeDefinition.FullName}'."
                            + " Only a single identifier name per source code file is currently supported. The first occurrence of"
                            + $" the identifier name will be annotated using the attribute '{_attributeDefinition.FullName}'.");
                    }

                    AnnotateField(fieldDefinition, summaries.First());
                }
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "UnityEngine";
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

        private string GetIdentifierReplacementFormat()
        {
            string identifierReplacementFormat = Config.Attribute(IdentifierReplacementFormatElementName)?.Value;
            if (string.IsNullOrWhiteSpace(identifierReplacementFormat))
            {
                LogInfo(
                    $"No '{IdentifierReplacementFormatElementName}' element is specified in the configuration."
                    + $" Falling back to use '{DefaultIdentifierReplacementFormat}'.");
                identifierReplacementFormat = DefaultIdentifierReplacementFormat;
            }
            else if (!identifierReplacementFormat.Contains("{0}"))
            {
                LogError(
                    $"The '{IdentifierReplacementFormatElementName}' configuration element doesn't specify a format"
                    + $" placeholder '{{0}}'. Falling back to use '{DefaultIdentifierReplacementFormat}'.");
                identifierReplacementFormat = DefaultIdentifierReplacementFormat;
            }

            return identifierReplacementFormat;
        }

        private bool FindAndRemoveAttribute(IMemberDefinition fieldDefinition)
        {
            CustomAttribute foundAttribute = fieldDefinition.CustomAttributes.SingleOrDefault(
                attribute => attribute.AttributeType.FullName == _fullAttributeName);
            if (foundAttribute == null)
            {
                return false;
            }

            fieldDefinition.CustomAttributes.Remove(foundAttribute);
            LogInfo($"Removed the attribute '{_fullAttributeName}' from the field '{fieldDefinition.FullName}'.");
            return true;
        }

        private static IReadOnlyDictionary<string, List<string>> ParseSourceFileXmlDocumentation(
            TypeDefinition typeDefinition,
            string identifierReplacementFormat)
        {
            string GetDocumentFilePath(TypeDefinition definition) =>
                definition.Methods.Select(methodDefinition => methodDefinition.DebugInformation)
                    .SelectMany(information => information.SequencePoints)
                    .FirstOrDefault()
                    ?.Document.Url;

            string documentFilePath = GetDocumentFilePath(typeDefinition);
            if (!File.Exists(documentFilePath))
            {
                documentFilePath = typeDefinition.Module.Types.Select(
                        definition =>
                        {
                            string filePath = GetDocumentFilePath(definition);
                            if (filePath == null)
                            {
                                return (null, null);
                            }

                            string[] namespaceParts = definition.Namespace.Split('.');
                            DirectoryInfo directoryInfo = new FileInfo(filePath).Directory;

                            for (int index = namespaceParts.Length - 1; index >= 0; index--)
                            {
                                if (directoryInfo == null)
                                {
                                    return (null, null);
                                }

                                string namespacePart = namespaceParts[index];
                                if (!string.Equals(
                                    namespacePart,
                                    directoryInfo.Name,
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    return (directoryInfo.FullName, namespacePart);
                                }

                                directoryInfo = directoryInfo.Parent;
                            }

                            return (null, null);
                        })
                    .Where(tuple => tuple.FullName != null)
                    .Select(
                        tuple => Path.Combine(
                            tuple.FullName,
                            typeDefinition.Namespace.Replace($"{tuple.namespacePart}.", string.Empty)
                                .Replace('.', Path.DirectorySeparatorChar),
                            $"{typeDefinition.Name}.cs"))
                    .FirstOrDefault(File.Exists);
            }

            if (documentFilePath == null || !File.Exists(documentFilePath))
            {
                return new Dictionary<string, List<string>>();
            }

            string sourceCode = File.ReadAllText(documentFilePath);
            Dictionary<string, List<string>> summariesByIdentifierName = new Dictionary<string, List<string>>();

            foreach (Match match in _documentedIdentifierRegex.Matches(sourceCode))
            {
                if (match.Groups["methodParameters"].Success)
                {
                    continue;
                }

                string identifierName = match.Groups["identifier"].Value;
                if (!summariesByIdentifierName.TryGetValue(identifierName, out List<string> summaries))
                {
                    summaries = new List<string>();
                    summariesByIdentifierName[identifierName] = summaries;
                }

                string body = match.Groups["summary"].Value.Trim(' ', '/', '\r', '\n');
                string summary = _xmlTagBodyCleanUpRegex.Replace(
                    body,
                    string.Format(identifierReplacementFormat, "${identifier}"));
                summaries.Add(summary);
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
