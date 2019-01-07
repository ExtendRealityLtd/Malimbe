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
        private static void OnEditorInitialization()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
            WeaveAllAssemblies();
        }

        [MenuItem("Tools/" + nameof(Malimbe) + "/Weave All Assemblies")]
        private static void ManuallyWeaveAllAssemblies()
        {
            WeaveAllAssemblies();
            Debug.Log("Weaving finished.");
        }

        private static void WeaveAllAssemblies()
        {
            List<Assembly> assemblies = GetAllAssemblies().ToList();
            for (int index = 0; index < assemblies.Count; index++)
            {
                Assembly assembly = assemblies[index];
                EditorUtility.DisplayProgressBar(
                    nameof(Malimbe),
                    $"Weaving '{assembly.name}'.",
                    (float)index / assemblies.Count);
                WeaveAssembly(assembly);
            }

            EditorUtility.ClearProgressBar();
        }

        private static void OnCompilationFinished(string path, CompilerMessage[] messages)
        {
            Assembly foundAssembly = GetAllAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.outputPath, path, StringComparison.Ordinal));
            if (foundAssembly != null)
            {
                WeaveAssembly(foundAssembly);
            }
        }

        private static IEnumerable<Assembly> GetAllAssemblies() =>
            CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Concat(CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                .GroupBy(assembly => assembly.outputPath)
                .Select(grouping => grouping.First());

        private static void WeaveAssembly(Assembly assembly)
        {
            try
            {
                string assemblyPath = WeaverPathsHelper.AddProjectPathRootIfNeeded(assembly.outputPath);
                IEnumerable<string> references =
                    assembly.allReferences.Select(WeaverPathsHelper.AddProjectPathRootIfNeeded);
                _runner.RunAsync(
                        WeaverPathsHelper.SearchPaths,
                        assemblyPath,
                        references,
                        assembly.defines.ToList(),
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
