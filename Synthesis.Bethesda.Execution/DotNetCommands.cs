using Noggog;
using Noggog.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public record DotNetVersion(string Version, bool Acceptable);

    public static class DotNetCommands
    {
        public const int MinVersion = 5;

        public static string GetBuildString(string args)
        {
            return $"build --runtime win-x64 {args} -c Release";
        }

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
            List<string> lines = new();
            List<string> errors = new();
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

        public static async Task<DotNetVersion> DotNetSdkVersion(CancellationToken cancel)
        {
            using var proc = ProcessWrapper.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", "--version"),
                cancel: cancel);
            List<string> outs = new();
            using var outp = proc.Output.Subscribe(o => outs.Add(o));
            List<string> errs = new();
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
            return GetDotNetVersion(outs[0]);
        }

        public static DotNetVersion GetDotNetVersion(ReadOnlySpan<char> str)
        {
            var orig = str;
            var indexOf = str.IndexOf('-');
            if (indexOf != -1)
            {
                str = str.Slice(0, indexOf);
            }
            if (Version.TryParse(str, out var vers)
                && vers.Major >= MinVersion)
            {
                return new DotNetVersion(orig.ToString(), true);
            }
            return new DotNetVersion(orig.ToString(), false);
        }
        
        public static async Task<GetResponse<string>> GetExecutablePath(string projectPath, CancellationToken cancel, Action<string>? log)
        {
            // Hacky way to locate executable, but running a build and extracting the path its logs spit out
            // Tried using Buildalyzer, but it has a lot of bad side effects like clearing build outputs when
            // locating information like this.
            using var proc = ProcessWrapper.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", GetBuildString($"\"{projectPath}\"")),
                cancel: cancel);
            log?.Invoke($"({proc.StartInfo.WorkingDirectory}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
            List<string> outs = new();
            using var outp = proc.Output.Subscribe(o => outs.Add(o));
            List<string> errs = new();
            using var errp = proc.Error.Subscribe(o => errs.Add(o));
            var result = await proc.Run();
            if (errs.Count > 0)
            {
                throw new ArgumentException($"{string.Join("\n", errs)}");
            }
            if (!TryGetExecutablePathFromOutput(outs, out var path))
            {
                log?.Invoke($"Could not locate target executable: {string.Join(Environment.NewLine, outs)}");
                return GetResponse<string>.Fail("Could not locate target executable.");
            }
            return GetResponse<string>.Succeed(path);
        }

        public static bool TryGetExecutablePathFromOutput(
            IEnumerable<string> lines, 
            [MaybeNullWhen(false)] out string output)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.EndsWith(".dll")) continue;
                const string delimiter = " -> ";
                var index = trimmed.IndexOf(delimiter, StringComparison.Ordinal);
                if (index == -1) continue;
                output = trimmed.Substring(index + delimiter.Length).Trim();
                return true;
            }
            output = null;
            return false;
        }

        public static async Task<ErrorResponse> Compile(string targetPath, CancellationToken cancel, Action<string>? log)
        {
            using var process = ProcessWrapper.Create(
                new ProcessStartInfo("dotnet", GetBuildString($"\"{Path.GetFileName(targetPath)}\""))
                {
                    WorkingDirectory = Path.GetDirectoryName(targetPath)!
                },
                cancel: cancel);
            log?.Invoke($"({process.StartInfo.WorkingDirectory}): {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            string? firstError = null;
            bool buildFailed = false;
            List<string> output = new();
            int totalLen = 0;
            process.Output.Subscribe(o =>
            {
                // ToDo
                // Refactor off looking for a string
                if (o.StartsWith("Build FAILED"))
                {
                    buildFailed = true;
                }
                else if (buildFailed
                    && firstError == null
                    && !string.IsNullOrWhiteSpace(o)
                    && o.StartsWith("error"))
                {
                    firstError = o;
                }
                if (totalLen < 10_000)
                {
                    totalLen += o.Length;
                    output.Add(o);
                }
            });
            var result = await process.Run().ConfigureAwait(false);
            if (result == 0) return ErrorResponse.Success;
            firstError = firstError?.TrimStart($"{targetPath} : ");
            if (firstError == null && cancel.IsCancellationRequested)
            {
                firstError = "Cancelled";
            }
            return ErrorResponse.Fail(reason: firstError ?? $"Unknown Error: {string.Join(Environment.NewLine, output)}");
        }

        public static bool IsApplicableErrorLine(ReadOnlySpan<char> str)
        {
            return str.Contains(": error ", StringComparison.Ordinal);
        }

        public static void PrintErrorMessage(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim, ReadOnlySpanAction<char, object?> toDo)
        {
            foreach (var item in message.SplitLines())
            {
                if (!IsApplicableErrorLine(item.Line)) continue;
                toDo(TrimErrorMessage(item.Line, relativePathTrim), null);
            }
        }

        public static ReadOnlySpan<char> TrimErrorMessage(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim)
        {
            if (message.StartsWith(relativePathTrim))
            {
                message = message.Slice(relativePathTrim.Length);
            }

            int index = 0;
            while (index < message.Length)
            {
                var slice = message.Slice(index);
                var actualIndex = slice.IndexOf('[');
                if (actualIndex == -1) break;
                index = actualIndex + index;
                if (index == message.Length) break;
                if (message.Slice(index + 1).StartsWith(relativePathTrim))
                {
                    message = message.Slice(0, index);
                }
                index++;
            }

            return message.Trim();
        }
    }

    // ToDo
    // Move to CSharpExt
    public static class StringExtensions
    {
        public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str)
        {
            // LineSplitEnumerator is a struct so there is no allocation here
            return new LineSplitEnumerator(str);
        }
    }

    // Must be a ref struct as it contains a ReadOnlySpan<char>
    public ref struct LineSplitEnumerator
    {
        private ReadOnlySpan<char> _str;

        public LineSplitEnumerator(ReadOnlySpan<char> str)
        {
            _str = str;
            Current = default;
        }

        // Needed to be compatible with the foreach operator
        public LineSplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var span = _str;
            if (span.Length == 0) // Reach the end of the string
                return false;

            var index = span.IndexOfAny('\r', '\n');
            if (index == -1) // The string is composed of only one line
            {
                _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            if (index < span.Length - 1 && span[index] == '\r')
            {
                // Try to consume the '\n' associated to the '\r'
                var next = span[index + 1];
                if (next == '\n')
                {
                    Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 2));
                    _str = span.Slice(index + 2);
                    return true;
                }
            }

            Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 1));
            _str = span.Slice(index + 1);
            return true;
        }

        public LineSplitEntry Current { get; private set; }
    }

    public readonly ref struct LineSplitEntry
    {
        public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
        {
            Line = line;
            Separator = separator;
        }

        public ReadOnlySpan<char> Line { get; }
        public ReadOnlySpan<char> Separator { get; }

        // This method allow to deconstruct the type, so you can write any of the following code
        // foreach (var entry in str.SplitLines()) { _ = entry.Line; }
        // foreach (var (line, endOfLine) in str.SplitLines()) { _ = line; }
        // https://docs.microsoft.com/en-us/dotnet/csharp/deconstruct?WT.mc_id=DT-MVP-5003978#deconstructing-user-defined-types
        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
        {
            line = Line;
            separator = Separator;
        }

        // This method allow to implicitly cast the type into a ReadOnlySpan<char>, so you can write the following code
        // foreach (ReadOnlySpan<char> entry in str.SplitLines())
        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
    }
}
