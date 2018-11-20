namespace Malimbe.FodyRunner.UnityIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;
    using Assembly = UnityEditor.Compilation.Assembly;

    internal static class EditorWeaver
    {
        private static readonly string _lastWeaveTimeTicksKey =
            $"{typeof(EditorWeaver).FullName}.{nameof(_lastWeaveTimeTicksKey)}";
        private static CancellationTokenSource _cancellationTokenSource;

        [InitializeOnLoadMethod]
        private static void OnEditorInitialization()
        {
            WeaveEditorAssemblies();

            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                long lastWriteTimeTicks = File.GetLastWriteTimeUtc(typeof(EditorWeaver).Assembly.Location).Ticks;
                if (long.Parse(SessionState.GetString(_lastWeaveTimeTicksKey, "-1")) <= lastWriteTimeTicks)
                {
                    /*
                     * The assembly this type is in changed which means Unity will reload this assembly and call this method again.
                     * Thus we shouldn't weave with the previous assembly that is executing at this point.
                     */
                    return;
                }

                WeaveEditorAssemblies();
            };

            ExecuteSynchronizationContextIfNeeded();
        }

        private static void WeaveEditorAssemblies()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            SessionState.SetString(_lastWeaveTimeTicksKey, DateTime.UtcNow.Ticks.ToString());
            EditorApplication.LockReloadAssemblies();
            Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            if (assemblies.Length == 0)
            {
                EditorApplication.UnlockReloadAssemblies();
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            Task.Run(
                    async () =>
                    {
                        try
                        {
                            Runner runner = new Runner(new Logger());

                            foreach (Assembly assembly in assemblies)
                            {
                                if (_cancellationTokenSource.IsCancellationRequested)
                                {
                                    break;
                                }

                                string assemblyPath =
                                    WeaverPathsHelper.AddProjectPathRootIfNeeded(assembly.outputPath);
                                IEnumerable<string> references =
                                    assembly.allReferences.Select(WeaverPathsHelper.AddProjectPathRootIfNeeded);

                                await runner.RunAsync(
                                        WeaverPathsHelper.SearchPaths,
                                        assemblyPath,
                                        references,
                                        assembly.defines.ToList(),
                                        WeaverPathsHelper.SearchPaths,
                                        true,
                                        cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }
                    },
                    cancellationToken)
                .ContinueWith(
                    _ => EditorApplication.UnlockReloadAssemblies(),
                    CancellationToken.None,
                    TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static void ExecuteSynchronizationContextIfNeeded()
        {
            // Task continuations are not run in the Editor before Unity 2018.2.0.
            string[] versionParts = Application.unityVersion.Split('.');
            if (versionParts.Length == 0
                || !Version.TryParse(string.Join(".", versionParts, 0, versionParts.Length - 1), out Version version))
            {
                Debug.LogWarning(
                    $"Unable to parse the Unity version from the string '{Application.unityVersion}' reported by Unity."
                    + " A workaround for task continuations not working in the Editor before 2018.2.0 won't be applied.");
                return;
            }

            if (version < new Version(2018, 2))
            {
                EditorApplication.update += () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        return;
                    }

                    SynchronizationContext context = SynchronizationContext.Current;
                    context.GetType()
                        .GetMethod("Exec", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.Invoke(context, null);
                };
            }
        }
    }
}
