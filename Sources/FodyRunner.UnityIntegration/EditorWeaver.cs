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
        [InitializeOnLoadMethod]
        private static void OnEditorInitialization()
        {
            void OnDelayCall()
            {
                // ReSharper disable once DelegateSubtraction
                EditorApplication.delayCall -= OnDelayCall;

                CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
                WeaveAllAssemblies();
            }

            EditorApplication.delayCall += OnDelayCall;
        }

        [MenuItem("Tools/" + nameof(Malimbe) + "/Weave All Assemblies")]
        private static void ManuallyWeaveAllAssemblies()
        {
            WeaveAllAssemblies();
            Debug.Log("Weaving finished.");
        }

        private static void WeaveAllAssemblies()
        {
            IReadOnlyList<Assembly> assemblies = GetAllAssemblies().ToList();
            IReadOnlyCollection<string> searchPaths = WeaverPathsHelper.GetSearchPaths().ToList();
            Runner runner = new Runner(new Logger());
            runner.Configure(searchPaths, searchPaths);

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

                WeaveAssembly(assembly, runner);
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

            IReadOnlyCollection<string> searchPaths = WeaverPathsHelper.GetSearchPaths().ToList();
            Runner runner = new Runner(new Logger());
            runner.Configure(searchPaths, searchPaths);

            WeaveAssembly(foundAssembly, runner);
        }

        [NotNull]
        private static IEnumerable<Assembly> GetAllAssemblies() =>
            CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Concat(CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                .GroupBy(assembly => assembly.outputPath)
                .Select(grouping => grouping.First());

        private static void WeaveAssembly(Assembly assembly, Runner runner)
        {
            try
            {
                string assemblyPath = WeaverPathsHelper.AddProjectPathRootIfNeeded(assembly.outputPath);
                IEnumerable<string> references =
                    assembly.allReferences.Select(WeaverPathsHelper.AddProjectPathRootIfNeeded);
                runner.RunAsync(assemblyPath, references, assembly.defines.ToList(), true).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
