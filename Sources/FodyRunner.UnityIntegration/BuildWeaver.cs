namespace Malimbe.FodyRunner.UnityIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    internal sealed class BuildWeaver : IPostprocessBuildWithReport
    {
        public int callbackOrder =>
            0;

        public void OnPostprocessBuild(BuildReport buildReport)
        {
            const string managedLibraryRoleName = "ManagedLibrary";
            List<string> managedLibraryFilePaths = buildReport.files
                .Where(file => string.Equals(file.role, managedLibraryRoleName, StringComparison.OrdinalIgnoreCase))
                .Select(file => file.path)
                .ToList();
            if (managedLibraryFilePaths.Count == 0)
            {
                Debug.LogWarning(
                    $"The build didn't create any files of role '{managedLibraryRoleName}'. No weaving will be done.");
                return;
            }

            IEnumerable<string> dependentManagedLibraryFilePaths = buildReport.files.Where(
                    file => string.Equals(file.role, "DependentManagedLibrary", StringComparison.OrdinalIgnoreCase))
                .Select(file => file.path);
            IEnumerable<string> managedEngineApiFilePaths = buildReport.files
                .Where(file => string.Equals(file.role, "ManagedEngineAPI", StringComparison.OrdinalIgnoreCase))
                .Select(file => file.path);
            List<string> potentialReferences = managedLibraryFilePaths.Concat(dependentManagedLibraryFilePaths)
                .Concat(managedEngineApiFilePaths)
                .ToList();
            List<string> scriptingDefineSymbols = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(buildReport.summary.platformGroup)
                .Split(
                    new[]
                    {
                        ';'
                    },
                    StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            bool isDebugBuild = buildReport.summary.options.HasFlag(BuildOptions.Development);

            try
            {
                Runner runner = new Runner(new Logger());

                foreach (string managedLibraryFilePath in managedLibraryFilePaths)
                {
                    runner.RunAsync(
                            WeaverPathsHelper.SearchPaths,
                            managedLibraryFilePath,
                            potentialReferences.Except(
                                new[]
                                {
                                    managedLibraryFilePath
                                }),
                            scriptingDefineSymbols,
                            WeaverPathsHelper.SearchPaths,
                            isDebugBuild,
                            CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
