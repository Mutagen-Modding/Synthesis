using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;
using Serilog;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath
{
    public interface IQueryExecutablePath
    {
        Task<GetResponse<string>> Query(FilePath projectPath, CancellationToken cancel);
    }

    public class QueryExecutablePath : IQueryExecutablePath
    {
        private readonly ILogger _Logger;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IRetrieveExecutablePath _retrieveExecutablePath;
        private readonly IBuildStartProvider _buildStartProvider;

        public QueryExecutablePath(
            ILogger logger,
            IProcessFactory processFactory,
            IRetrieveExecutablePath retrieveExecutablePath,
            IBuildStartProvider buildStartProvider)
        {
            _Logger = logger;
            _ProcessFactory = processFactory;
            _retrieveExecutablePath = retrieveExecutablePath;
            _buildStartProvider = buildStartProvider;
        }
        
        public async Task<GetResponse<string>> Query(FilePath projectPath, CancellationToken cancel)
        {
            // Hacky way to locate executable, but running a build and extracting the path its logs spit out
            // Tried using Buildalyzer, but it has a lot of bad side effects like clearing build outputs when
            // locating information like this.
            using var proc = _ProcessFactory.Create(
                _buildStartProvider.Construct(projectPath),
                cancel: cancel);
            _Logger.Information("({WorkingDirectory}): {FileName} {Args}",
                proc.StartInfo.WorkingDirectory,
                proc.StartInfo.FileName,
                proc.StartInfo.Arguments);
            List<string> outs = new();
            using var outp = proc.Output.Subscribe(o => outs.Add(o));
            List<string> errs = new();
            using var errp = proc.Error.Subscribe(o => errs.Add(o));
            var result = await proc.Run();
            if (errs.Count > 0)
            {
                throw new ArgumentException($"{string.Join(Environment.NewLine, errs)}");
            }
            if (!_retrieveExecutablePath.TryGet(projectPath, outs, out var path))
            {
                _Logger.Warning("Could not locate target executable: {Lines}", string.Join(Environment.NewLine, outs));
                return GetResponse<string>.Fail("Could not locate target executable.");
            }
            return GetResponse<string>.Succeed(path);
        }
    }
}