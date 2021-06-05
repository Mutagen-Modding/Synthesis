using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution
{
    public interface IQueryExecutablePath
    {
        Task<GetResponse<string>> Query(string projectPath, CancellationToken cancel, Action<string>? log);
    }

    public class QueryExecutablePath : IQueryExecutablePath
    {
        private readonly IProvideBuildString _BuildString;

        public QueryExecutablePath(IProvideBuildString buildString)
        {
            _BuildString = buildString;
        }
        
        public async Task<GetResponse<string>> Query(string projectPath, CancellationToken cancel, Action<string>? log)
        {
            // Hacky way to locate executable, but running a build and extracting the path its logs spit out
            // Tried using Buildalyzer, but it has a lot of bad side effects like clearing build outputs when
            // locating information like this.
            using var proc = ProcessWrapper.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", _BuildString.Get($"\"{projectPath}\"")),
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
            if (!DotNetCommands.TryGetExecutablePathFromOutput(outs, out var path))
            {
                log?.Invoke($"Could not locate target executable: {string.Join(Environment.NewLine, outs)}");
                return GetResponse<string>.Fail("Could not locate target executable.");
            }
            return GetResponse<string>.Succeed(path);
        }
    }
}