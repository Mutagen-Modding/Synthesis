using System;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath
{
    public interface IQueryExecutablePath
    {
        Task<GetResponse<string>> Query(FilePath projectPath, CancellationToken cancel);
    }

    public class QueryExecutablePath : IQueryExecutablePath
    {
        private readonly ILogger _Logger;
        public IProcessRunner Runner { get; }
        public IRetrieveExecutablePath RetrievePath { get; }
        public IBuildStartInfoProvider StartInfoProvider { get; }

        public QueryExecutablePath(
            ILogger logger,
            IProcessRunner processRunner,
            IRetrieveExecutablePath retrieveExecutablePath,
            IBuildStartInfoProvider buildStartInfoProvider)
        {
            _Logger = logger;
            Runner = processRunner;
            RetrievePath = retrieveExecutablePath;
            StartInfoProvider = buildStartInfoProvider;
        }
        
        public async Task<GetResponse<string>> Query(FilePath projectPath, CancellationToken cancel)
        {
            // Hacky way to locate executable, but running a build and extracting the path its logs spit out
            // Tried using Buildalyzer, but it has a lot of bad side effects like clearing build outputs when
            // locating information like this.
            var result = await Runner.RunAndCapture(
                StartInfoProvider.Construct(projectPath),
                cancel: cancel);
            if (result.Errors.Count > 0)
            {
                return GetResponse<string>.Fail($"{string.Join(Environment.NewLine, result.Errors)}");
            }
            if (!RetrievePath.TryGet(projectPath, result.Out, out var path))
            {
                _Logger.Warning("Could not locate target executable: {Lines}", string.Join(Environment.NewLine, result.Out));
                return GetResponse<string>.Fail("Could not locate target executable.");
            }
            return GetResponse<string>.Succeed(path);
        }
    }
}