namespace Malimbe.FodyRunner.UnityIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JetBrains.Annotations;
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
            List<string> searchPaths = WeaverPathsHelper.GetSearchPaths().ToList();

            for (int index = 0; index < assemblies.Count; index++)
            {
                Assembly assembly = assemblies[index];
                try
                {
                    EditorUtility.DisplayProgressBar(
                        nameof(Malimbe),
                        $"Weaving '{assembly.name}'.",
                        (float)index / assemblies.Count);
                }
                catch
                {
                    // ignored
                }

                WeaveAssembly(assembly, searchPaths);
            }

            try
            {
                EditorUtility.ClearProgressBar();
            }
            catch
            {
                // ignored
            }
        }

        private static void OnCompilationFinished(string path, CompilerMessage[] messages)
        {
            Assembly foundAssembly = GetAllAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.outputPath, path, StringComparison.Ordinal));
            if (foundAssembly == null)
            {
                return;
            }

            List<string> searchPaths = WeaverPathsHelper.GetSearchPaths().ToList();
            WeaveAssembly(foundAssembly, searchPaths);
        }

        [NotNull]
        private static IEnumerable<Assembly> GetAllAssemblies() =>
            CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Concat(CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                .GroupBy(assembly => assembly.outputPath)
                .Select(grouping => grouping.First());

        private static void WeaveAssembly(Assembly assembly, ICollection<string> searchPaths)
        {
            try
            {
                string assemblyPath = WeaverPathsHelper.AddProjectPathRootIfNeeded(assembly.outputPath);
                IEnumerable<string> references =
                    assembly.allReferences.Select(WeaverPathsHelper.AddProjectPathRootIfNeeded);
                _runner.RunAsync(searchPaths, assemblyPath, references, assembly.defines.ToList(), searchPaths, true)
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
