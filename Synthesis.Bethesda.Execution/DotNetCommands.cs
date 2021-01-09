using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public static class DotNetCommands

    {
        public static async Task<IEnumerable<(string Package, string Requested, string Resolved, string Latest)>> NugetListingQuery(string projectPath, bool outdated, bool includePrerelease, CancellationToken cancel)
        {
            // Run restore first
            {
                using var restore = ProcessWrapper.Create(
                    new ProcessStartInfo("dotnet", $"restore \"{projectPath}\""),
                    cancel: cancel);
                await restore.Run();
            }

            bool on = false;
            List<string> lines = new List<string>();
            List<string> errors = new List<string>();
            using var process = ProcessWrapper.Create(
                new ProcessStartInfo("dotnet", $"list \"{projectPath}\" package{(outdated ? " --outdated" : null)}{(includePrerelease ? " --include-prerelease" : null)}"),
                cancel: cancel);
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

        public static async Task<(string? MutagenVersion, string? SynthesisVersion)> QuerySynthesisVersions(string projectPath, bool current, bool includePrerelease, CancellationToken cancel)
        {
            string? mutagenVersion = null, synthesisVersion = null;
            var queries = await NugetListingQuery(projectPath, outdated: !current, includePrerelease: includePrerelease, cancel: cancel);
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

        public static async Task<Version> DotNetSdkVersion(CancellationToken cancel)
        {
            using var proc = ProcessWrapper.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", "--version"),
                cancel: cancel);
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

        public static async Task<GetResponse<string>> GetExecutablePath(string projectPath, CancellationToken cancel)
        {
            // Hacky way to locate executable, but running a build and extracting the path its logs spit out
            // Tried using Buildalyzer, but it has a lot of bad side effects like clearing build outputs when
            // locating information like this.
            using var proc = ProcessWrapper.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", $"build \"{projectPath}\""),
                cancel: cancel);
            List<string> outs = new List<string>();
            using var outp = proc.Output.Subscribe(o => outs.Add(o));
            List<string> errs = new List<string>();
            using var errp = proc.Error.Subscribe(o => errs.Add(o));
            var result = await proc.Run();
            if (errs.Count > 0)
            {
                throw new ArgumentException($"{string.Join("\n", errs)}");
            }
            int index = outs.IndexOf("Build succeeded.");
            if (index == -1 || index < 2)
            {
                return GetResponse<string>.Fail("Could not locate target executable.");
            }
            var line = outs[index - 2];
            const string delimiter = " -> ";
            index = line.IndexOf(delimiter);
            if (index == -1)
            {
                return GetResponse<string>.Fail("Could not locate target executable.");
            }
            return GetResponse<string>.Succeed(line.Substring(index + delimiter.Length).Trim());
        }
    }
}
