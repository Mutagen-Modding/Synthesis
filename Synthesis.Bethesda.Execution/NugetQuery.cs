using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public static class NugetQuery
    {
        public static async Task<IEnumerable<(string Package, string Requested, string Resolved, string Latest)>> Query(string projectPath, bool queryServer)
        {
            // Run restore first
            {
                using var restore = ProcessWrapper.Start(
                    new ProcessStartInfo("dotnet", $"restore \"{projectPath}\""));
                await restore.Start();
            }

            bool on = false;
            List<string> lines = new List<string>();
            List<string> errors = new List<string>();
            using var process = ProcessWrapper.Start(
                new ProcessStartInfo("dotnet", $"list \"{projectPath}\" package{(queryServer ? " --outdated" : null)}"));
            using var outSub = process.Output.Subscribe(s =>
            {
                if (s.Contains("Top-level Package"))
                {
                    on = true;
                    return;
                }
                if (!on) return;
                lines.Add(s);
            });
            using var errSub = process.Error.Subscribe(s => errors.Add(s));
            var result = await process.Start();
            if (errors.Count > 0)
            {
                throw new ArgumentException($"Error while retrieving nuget listings: \n{string.Join("\n", errors)}");
            }

            var ret = new List<(string Package, string Requested, string Resolved, string Latest)>();
            foreach (var line in lines)
            {
                var startIndex = line.IndexOf("> ");
                if (startIndex == -1) continue;
                var split = line.Substring(startIndex + 2).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                ret.Add((split[0], split[1], split[2], split[3]));
            }
            return ret;
        }

        public static async Task<(string? MutagenVersion, string? SynthesisVersion)> QueryVersions(string projectPath, bool current)
        {
            string? mutagenVersion = null, synthesisVersion = null;
            var queries = await Query(projectPath, !current);
            foreach (var item in queries)
            {
                if (item.Package.StartsWith("Mutagen.Bethesda")
                    && !item.Package.EndsWith("Synthesis"))
                {
                    mutagenVersion = current ? item.Resolved : item.Latest;
                }
                if (item.Package.Equals("Mutagen.Bethesda.Synthesis"))
                {
                    synthesisVersion = current ? item.Resolved : item.Latest;
                }
            }
            return (mutagenVersion, synthesisVersion);
        }
    }
}
