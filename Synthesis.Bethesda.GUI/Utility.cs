using Buildalyzer;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    }
}
