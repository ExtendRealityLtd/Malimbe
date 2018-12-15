namespace Malimbe.FodyRunner.UnityIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class WeaverPathsHelper
    {
        public static readonly List<string> SearchPaths;
        private static readonly string _projectPath;

        static WeaverPathsHelper()
        {
            _projectPath = Directory.GetParent(Application.dataPath).FullName;

            ListRequest listRequest = Client.List(true);
            while (listRequest.Status == StatusCode.InProgress)
            {
                Thread.Sleep(100);
            }

            SearchPaths = listRequest.Result.Select(info => info.resolvedPath)
                .ToList()
                .Concat(
                    new[]
                    {
                        _projectPath
                    })
                .ToList();
        }

        public static string AddProjectPathRootIfNeeded(string path) =>
            Path.IsPathRooted(path) ? path : Path.Combine(_projectPath, path);
    }
}
