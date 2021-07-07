using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;
using Serilog;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IQueryExecutablePath
    {
        Task<GetResponse<string>> Query(string projectPath, CancellationToken cancel);
    }

    public class QueryExecutablePath : IQueryExecutablePath
    {
        private readonly ILogger _Logger;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideBuildString _BuildString;

        public QueryExecutablePath(
            ILogger logger,
            IProcessFactory processFactory,
            IProvideBuildString buildString)
        {
            _Logger = logger;
            _ProcessFactory = processFactory;
            _BuildString = buildString;
        }
        
        public async Task<GetResponse<string>> Query(string projectPath, CancellationToken cancel)
        {
            // Hacky way to locate executable, but running a build and extracting the path its logs spit out
            // Tried using Buildalyzer, but it has a lot of bad side effects like clearing build outputs when
            // locating information like this.
            using var proc = _ProcessFactory.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", _BuildString.Get($"\"{projectPath}\"")),
                cancel: cancel);
            _Logger.Information($"({proc.StartInfo.WorkingDirectory}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
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
                _Logger.Warning($"Could not locate target executable: {string.Join(Environment.NewLine, outs)}");
                return GetResponse<string>.Fail("Could not locate target executable.");
            }
            return GetResponse<string>.Succeed(path);
        }
    }
}