using Buildalyzer;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Synthesis.Bethesda.GUI
{
    public static class Utility
    {
        public static IEnumerable<string> AvailableProjectSubpaths(string solutionPath)
        {
            if (!File.Exists(solutionPath)) return Enumerable.Empty<string>();
            try
            {
                var manager = new AnalyzerManager(solutionPath);
                return manager.Projects.Keys.Select(projPath => projPath.TrimStart($"{Path.GetDirectoryName(solutionPath)}\\"!));
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }

        public static async void NavigateToPath(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error navigating to path: {path}");
            }
        }
    }
}
