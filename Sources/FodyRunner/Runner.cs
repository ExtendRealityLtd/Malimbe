namespace Malimbe.FodyRunner
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Mono.Cecil;

    public sealed class Runner
    {
        private static readonly ReaderParameters _processedCheckReaderParameters = new ReaderParameters
        {
            ReadWrite = false,
            ReadingMode = ReadingMode.Deferred,
            ReadSymbols = false
        };
        private static readonly string _ignoredConfigurationElementLocalName = typeof(Runner).Namespace;
        private readonly LogForwarder _logForwarder;

        public Runner(ILogger logger) =>
            _logForwarder = new LogForwarder(logger);

        public Task<bool> RunAsync(
            IEnumerable<string> configurationSearchPaths,
            string assemblyFilePath,
            IEnumerable<string> references,
            List<string> defineConstants,
            ICollection<string> weaverSearchPaths,
            bool isDebugBuild,
            CancellationToken cancellationToken = default) =>
            Task.Run(
                () =>
                {
                    if (IsAssemblyProcessed(assemblyFilePath))
                    {
                        _logForwarder.LogInfo(
                            $"Not processing assembly '{assemblyFilePath}' because it has already been processed.");
                        return false;
                    }

                    IEnumerable<string> configurationFilePaths = configurationSearchPaths.SelectMany(
                        path => Directory.GetFiles(path, "FodyWeavers.xml", SearchOption.AllDirectories));
                    List<WeaverEntry> weaverEntries = CreateWeaverEntries(configurationFilePaths, weaverSearchPaths);
                    InnerWeaver innerWeaver = new InnerWeaver
                    {
                        AssemblyFilePath = assemblyFilePath,
                        References = string.Join(";", references),
                        ReferenceCopyLocalPaths = new List<string>(),
                        DefineConstants = defineConstants,
                        Logger = _logForwarder,
                        Weavers = weaverEntries,
                        DebugSymbols = isDebugBuild ? DebugSymbolsType.External : DebugSymbolsType.None
                    };
                    CancellationTokenRegistration cancellationTokenRegistration =
                        // ReSharper disable once AccessToDisposedClosure
                        cancellationToken.Register(() => innerWeaver.Cancel());

                    try
                    {
                        innerWeaver.Execute();
                    }
                    finally
                    {
                        cancellationTokenRegistration.Dispose();
                        innerWeaver.Dispose();
                    }

                    return true;
                },
                cancellationToken);

        private List<WeaverEntry> CreateWeaverEntries(
            IEnumerable<string> configurationFilePaths,
            ICollection<string> weaverSearchPaths)
        {
            const string assemblyNameSuffix = ".Fody.dll";

            Dictionary<string, string> weaverAssembliesPathsByName = weaverSearchPaths
                .SelectMany(path => Directory.GetFiles(path, $"*{assemblyNameSuffix}", SearchOption.AllDirectories))
                .GroupBy(
                    path =>
                    {
                        string fileName = Path.GetFileName(path);
                        return fileName?.Remove(
                            fileName.Length - assemblyNameSuffix.Length,
                            assemblyNameSuffix.Length);
                    })
                .ToDictionary(grouping => grouping.Key, grouping => grouping.FirstOrDefault());

            List<WeaverEntry> weaverEntries = new List<WeaverEntry>();
            foreach (string configurationFilePath in configurationFilePaths)
            {
                XDocument document = XDocument.Load(configurationFilePath);
                IEnumerable<XElement> elements = document.Element("Weavers")?.Elements();
                if (elements == null)
                {
                    continue;
                }

                _logForwarder.SetLogLevelFromConfiguration(document);

                foreach (XElement element in elements)
                {
                    string assemblyName = element.Name.LocalName;
                    if (assemblyName == _ignoredConfigurationElementLocalName)
                    {
                        continue;
                    }

                    if (!weaverAssembliesPathsByName.TryGetValue(assemblyName, out string assemblyPath))
                    {
                        _logForwarder.LogError(
                            $"A configuration lists '{assemblyName}' but the assembly file wasn't found"
                            + $" in the search paths '{string.Join(", ", weaverSearchPaths)}'.");
                        continue;
                    }

                    WeaverEntry existingEntry = weaverEntries.FirstOrDefault(
                        entry => entry.AssemblyName == assemblyName);
                    int index = weaverEntries.Count;
                    if (existingEntry != null)
                    {
                        _logForwarder.LogWarning(
                            $"There are multiple configurations for '{assemblyName}'. The configuration read last is used.");
                        index = weaverEntries.IndexOf(existingEntry);
                        weaverEntries.Remove(existingEntry);
                    }

                    WeaverEntry weaverEntry = new WeaverEntry
                    {
                        Element = element.ToString(SaveOptions.OmitDuplicateNamespaces),
                        AssemblyName = assemblyName,
                        AssemblyPath = assemblyPath,
                        TypeName = "ModuleWeaver"
                    };
                    weaverEntries.Insert(index, weaverEntry);
                }
            }

            return weaverEntries;
        }

        private static bool IsAssemblyProcessed(string assemblyFilePath)
        {
            using (ModuleDefinition moduleDefinition = ModuleDefinition.ReadModule(
                assemblyFilePath,
                _processedCheckReaderParameters))
            {
                return moduleDefinition.Types.Any(typeDefinition => typeDefinition.Name == "ProcessedByFody");
            }
        }
    }
}
