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
        private readonly LogForwarder _logForwarder;
        private readonly List<WeaverEntry> _weaverEntries = new List<WeaverEntry>();

        public Runner(ILogger logger) =>
            _logForwarder = new LogForwarder(logger);

        public void Configure(
            IEnumerable<string> configurationSearchPaths,
            IReadOnlyCollection<string> weaverSearchPaths)
        {
            const string assemblyNameSuffix = ".Fody.dll";

            IReadOnlyList<XDocument> documents = configurationSearchPaths.SelectMany(
                    path => Directory.GetFiles(path, "FodyWeavers.xml", SearchOption.AllDirectories))
                .Select(XDocument.Load)
                .ToList();
            IReadOnlyList<XElement> runnerElements = documents.SelectMany(
                    document => document.Root?.Elements(typeof(Runner).Namespace))
                .ToList();
            IEnumerable<XElement> weaverElements = documents
                .SelectMany(document => document.Element("Weavers")?.Elements())
                .Except(runnerElements);
            IReadOnlyDictionary<string, string> weaverAssembliesPathsByName = weaverSearchPaths
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

            _logForwarder.SetLogLevelFromConfiguration(runnerElements);
            _weaverEntries.Clear();

            foreach (XElement weaverElement in weaverElements)
            {
                string assemblyName = weaverElement.Name.LocalName;
                if (!weaverAssembliesPathsByName.TryGetValue(assemblyName, out string assemblyPath))
                {
                    _logForwarder.LogError(
                        $"A configuration lists '{assemblyName}' but the assembly file wasn't found"
                        + $" in the search paths '{string.Join(", ", weaverSearchPaths)}'.");
                    continue;
                }

                WeaverEntry existingEntry = _weaverEntries.FirstOrDefault(entry => entry.AssemblyName == assemblyName);
                int index = _weaverEntries.Count;
                if (existingEntry != null)
                {
                    _logForwarder.LogWarning(
                        $"There are multiple configurations for '{assemblyName}'. The configuration read last is used.");
                    index = _weaverEntries.IndexOf(existingEntry);
                    _weaverEntries.Remove(existingEntry);
                }

                WeaverEntry weaverEntry = new WeaverEntry
                {
                    Element = weaverElement.ToString(SaveOptions.OmitDuplicateNamespaces),
                    AssemblyName = assemblyName,
                    AssemblyPath = assemblyPath,
                    TypeName = "ModuleWeaver"
                };
                _weaverEntries.Insert(index, weaverEntry);
            }
        }

        public Task<bool> RunAsync(
            string assemblyFilePath,
            IEnumerable<string> references,
            List<string> defineConstants,
            bool isDebugBuild,
            CancellationToken cancellationToken = default)
        {
            if (_weaverEntries == null)
            {
                throw new NotConfiguredException();
            }

            return Task.Run(
                () =>
                {
                    if (!File.Exists(assemblyFilePath))
                    {
                        _logForwarder.LogInfo(
                            $"Not processing assembly '{assemblyFilePath}' because the file doesn't exist.");
                        return false;
                    }

                    if (IsAssemblyProcessed(assemblyFilePath))
                    {
                        _logForwarder.LogInfo(
                            $"Not processing assembly '{assemblyFilePath}' because it has already been processed.");
                        return false;
                    }

                    InnerWeaver innerWeaver = new InnerWeaver
                    {
                        AssemblyFilePath = assemblyFilePath,
                        References = string.Join(";", references),
                        ReferenceCopyLocalPaths = new List<string>(),
                        DefineConstants = defineConstants,
                        Logger = _logForwarder,
                        Weavers = _weaverEntries,
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
