using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public static class DotNetQueries
    {
        public static async Task<IEnumerable<(string Package, string Requested, string Resolved, string Latest)>> NugetListingQuery(string projectPath, bool outdated, bool includePrerelease)
        {
            // Run restore first
            {
                using var restore = ProcessWrapper.Create(
                    new ProcessStartInfo("dotnet", $"restore \"{projectPath}\""));
                await restore.Run();
            }

            bool on = false;
            List<string> lines = new List<string>();
            List<string> errors = new List<string>();
            using var process = ProcessWrapper.Create(
                new ProcessStartInfo("dotnet", $"list \"{projectPath}\" package{(outdated ? " --outdated" : null)}{(includePrerelease ? " --include-prerelease" : null)}"));
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
            var result = await process.Run();
            if (errors.Count > 0)
            {
                throw new ArgumentException($"Error while retrieving nuget listings: \n{string.Join("\n", errors)}");
            }

            var ret = new List<(string Package, string Requested, string Resolved, string Latest)>();
            foreach (var line in lines)
            {
                if (!TryParseLibraryLine(
                    line, 
                    out var package,
                    out var requested, 
                    out var resolved, 
                    out var latest))
                {
                    continue;
                }
                ret.Add((package, requested, resolved, latest));
            }
            return ret;
        }

        public static bool TryParseLibraryLine(
            string line, 
            [MaybeNullWhen(false)] out string package,
            [MaybeNullWhen(false)] out string requested,
            [MaybeNullWhen(false)] out string resolved,
            [MaybeNullWhen(false)] out string latest)
        {
            var startIndex = line.IndexOf("> ");
            if (startIndex == -1)
            {
                package = default;
                requested = default;
                resolved = default;
                latest = default;
                return false;
            }
            var split = line
                .Substring(startIndex + 2)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .WithIndex()
                .Where(x => x.Index == 0 || x.Item != "(D)")
                .Select(x => x.Item)
                .ToArray();
            package = split[0];
            requested = split[1];
            resolved = split[2];
            latest = split[3];
            return true;
        }

        public static async Task<(string? MutagenVersion, string? SynthesisVersion)> QuerySynthesisVersions(string projectPath, bool current, bool includePrerelease)
        {
            string? mutagenVersion = null, synthesisVersion = null;
            var queries = await NugetListingQuery(projectPath, outdated: !current, includePrerelease: includePrerelease);
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

        public static async Task<Version> DotNetSdkVersion()
        {
            using var proc = ProcessWrapper.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", "--version"));
            List<string> outs = new List<string>();
            using var outp = proc.Output.Subscribe(o => outs.Add(o));
            List<string> errs = new List<string>();
            using var errp = proc.Error.Subscribe(o => errs.Add(o));
            var result = await proc.Run();
            if (errs.Count > 0)
            {
                throw new ArgumentException($"{string.Join("\n", errs)}");
            }
            if (outs.Count != 1)
            {
                throw new ArgumentException($"Unexpected messages:\n{string.Join("\n", outs)}");
            }
            if (!Version.TryParse(outs[0], out var v))
            {
                throw new ArgumentException($"Could not parse dotnet SDK version: {outs[0]}");
            }
            return v;
        }
    }
}
