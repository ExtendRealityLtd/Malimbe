namespace Malimbe.FodyRunner.UnityIntegration
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class WeaverPathsHelper
    {
        public static readonly string[] SearchPaths;
        private static readonly string _projectPath;

        static WeaverPathsHelper()
        {
            _projectPath = Directory.GetParent(Application.dataPath).FullName;
            SearchPaths = new[]
            {
                _projectPath
            };
        }

        public static string AddProjectPathRootIfNeeded(string path) =>
            Path.IsPathRooted(path) ? path : Path.Combine(_projectPath, path);
    }
}
