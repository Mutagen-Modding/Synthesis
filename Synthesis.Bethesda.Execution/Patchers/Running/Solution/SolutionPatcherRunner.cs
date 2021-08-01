using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution
{
    public interface ISolutionPatcherRunner
    {
        Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
    }

    public class SolutionPatcherRunner : ISolutionPatcherRunner
    {
        public IDotNetCommandStartConstructor CommandStartConstructor { get; }
        public IPathToProjProvider PathToProjProvider { get; }
        public IConstructSolutionPatcherRunArgs ConstructArgs { get; }
        public IProcessRunner ProcessRunner { get; }
        public IFormatCommandLine Formatter { get; }
        private readonly ILogger _logger;

        public SolutionPatcherRunner(
            IDotNetCommandStartConstructor commandStartConstructor,
            IPathToProjProvider pathToProjProvider,
            IConstructSolutionPatcherRunArgs constructArgs,
            IProcessRunner processRunner,
            IFormatCommandLine formatter,
            ILogger logger)
        {
            CommandStartConstructor = commandStartConstructor;
            PathToProjProvider = pathToProjProvider;
            ConstructArgs = constructArgs;
            ProcessRunner = processRunner;
            Formatter = formatter;
            _logger = logger;
        }
        
        public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
        {
            _logger.Information("Running");
            var args = ConstructArgs.Construct(settings);
            var result = await ProcessRunner.Run(
                CommandStartConstructor.Construct(
                    "run --project",
                    PathToProjProvider.Path, 
                    "--no-build",
                    Formatter.Format(args)),
                cancel: cancel);
            
            if (result != 0)
            {
                throw new CliUnsuccessfulRunException(result, "Error running solution patcher");
            }
        }
    }
}