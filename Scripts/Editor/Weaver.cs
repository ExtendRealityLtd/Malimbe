namespace Malimbe
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;

    public static class Weaver
    {
        public enum LogLevel
        {
            Nothing,
            Summarized,
            Detailed
        }

        private const string didWeaveAlreadyKey = "VRTK.Weaver.DidWeaveAlready";
        private const int processTimeout = 10000;

        // TODO: Make this an accessible setting by adding some UI for it somewhere.
        private const LogLevel logLevel = LogLevel.Nothing;

        private static readonly string _dataPath = Directory.GetParent(Application.dataPath).FullName;

        [InitializeOnLoadMethod]
        private static void OnEditorInitialization()
        {
            if (!SessionState.GetBool(didWeaveAlreadyKey, false))
            {
                SessionState.SetBool(didWeaveAlreadyKey, true);
                Weave();
            }

            AssemblyReloadEvents.beforeAssemblyReload += Weave;
        }

        private static void Weave()
        {
            string executablePath, configurationPath, weaversAssemblyPath;
            FindFiles(out executablePath, out configurationPath, out weaversAssemblyPath);

            if (logLevel != LogLevel.Nothing)
            {
                UnityEngine.Debug.Log("Starting to weave.");
            }

            EditorApplication.LockReloadAssemblies();

            try
            {
                foreach (Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
                {
                    string assemblyPath = Path.IsPathRooted(assembly.outputPath)
                        ? assembly.outputPath
                        : Path.Combine(_dataPath, assembly.outputPath);
                    string referencePaths = string.Join(
                        ";",
                        assembly.allReferences.Select(
                            path => Path.IsPathRooted(path) ? path : Path.Combine(_dataPath, path)));

                    if (logLevel != LogLevel.Nothing)
                    {
                        UnityEngine.Debug.Log($"Weaving '{assemblyPath}'.");
                    }

                    using (Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = executablePath,
                            Arguments = $"-f \"{configurationPath}\" -w \"{weaversAssemblyPath}\" -t \"{assemblyPath}\" -r \"{referencePaths}\"",
                            WorkingDirectory = _dataPath,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    })
                    {
                        process.OutputDataReceived += OnProcessOutputDataReceived;
                        process.Start();
                        process.BeginOutputReadLine();
                        if (process.WaitForExit(processTimeout))
                        {
                            continue;
                        }

                        process.Kill();
                        process.OutputDataReceived -= OnProcessOutputDataReceived;
                        UnityEngine.Debug.LogError($"Unable to weave '{assemblyPath}'. The process timed out.");
                    }
                }
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                if (logLevel != LogLevel.Nothing)
                {
                    UnityEngine.Debug.Log("Done weaving.");
                }
            }
        }

        private static void FindFiles(
            out string executablePath,
            out string configurationPath,
            out string weaversAssemblyPath)
        {
            executablePath = FindAbsoluteAssetPath("Fody.StandAlone");
            configurationPath = FindAbsoluteAssetPath("FodyWeavers");
            weaversAssemblyPath = Path.GetDirectoryName(FindAbsoluteAssetPath("*.Fody"));
        }

        private static string FindAbsoluteAssetPath(string filter)
        {
            string guid = AssetDatabase.FindAssets(filter).FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                throw new FileNotFoundException($"Can't find file for filter '{filter}' in the AssetDatabase.");
            }

            return Path.Combine(_dataPath, AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (logLevel == LogLevel.Detailed && eventArgs?.Data != null)
            {
                UnityEngine.Debug.Log(eventArgs.Data);
            }
        }
    }
}
