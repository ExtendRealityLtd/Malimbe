namespace Malimbe.FodyRunner.UnityIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;

    internal static class EditorWeaver
    {
        private static readonly Runner _runner = new Runner(new Logger());

        [InitializeOnLoadMethod]
        private static void OnEditorInitialization() =>
            CompilationPipeline.assemblyCompilationFinished += (path, messages) => WeaveAssembly(path);

        private static void WeaveAssembly(string assemblyFilePath)
        {
            Assembly foundAssembly = CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Concat(CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                .FirstOrDefault(
                    assembly => string.Equals(assembly.outputPath, assemblyFilePath, StringComparison.Ordinal));
            if (foundAssembly == null)
            {
                return;
            }

            try
            {
                string assemblyPath = WeaverPathsHelper.AddProjectPathRootIfNeeded(foundAssembly.outputPath);
                IEnumerable<string> references =
                    foundAssembly.allReferences.Select(WeaverPathsHelper.AddProjectPathRootIfNeeded);
                _runner.RunAsync(
                        WeaverPathsHelper.SearchPaths,
                        assemblyPath,
                        references,
                        foundAssembly.defines.ToList(),
                        WeaverPathsHelper.SearchPaths,
                        true)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
