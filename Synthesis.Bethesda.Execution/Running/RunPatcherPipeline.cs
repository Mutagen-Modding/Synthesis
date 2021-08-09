using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running
{
    public interface IRunPatcherPipeline
    {
        Task Run(RunPatcherPipelineInstructions run);
    }

    public class RunPatcherPipeline : IRunPatcherPipeline
    {
        private readonly ILogger _logger;
        private readonly IRunProfileProvider _profileProvider;
        private readonly IExecuteRun _executeRun;
        public CancellationToken Cancel { get; }

        public RunPatcherPipeline(
            ILogger logger,
            IRunProfileProvider profileProvider,
            IExecuteRun executeRun,
            CancellationToken cancel)
        {
            _logger = logger;
            _profileProvider = profileProvider;
            _executeRun = executeRun;
            Cancel = cancel;
        }
        
        public async Task Run(RunPatcherPipelineInstructions run)
        {
            if (_profileProvider.Profile.TargetRelease != run.GameRelease)
            {
                throw new ArgumentException($"Target game release did not match profile's: {run.GameRelease} != {_profileProvider.Profile.TargetRelease}");
            }

            if (run.LoadOrderFilePath.IsNullOrWhitespace())
            {
                run.LoadOrderFilePath = PluginListings.GetListingsPath(run.GameRelease);
            }

            _logger.Information("Patchers to run:");
            var patchers = _profileProvider.Profile.Patchers
                .Where(p => p.On)
                .Select(patcherSettings =>
                {
                    patcherSettings.Print(_logger);

                    throw new NotImplementedException();
                    return default(IPatcherRun);
                    // return _runnerFactory.Create(patcherSettings);
                })
                .ToList();

            await _executeRun
                .Run(
                    outputPath: run.OutputPath,
                    patchers: patchers.Select((p, i) => (i + 1, p)).ToArray(),
                    sourcePath: run.SourcePath == null ? default : new FilePath(run.SourcePath),
                    cancel: Cancel,
                    persistenceMode: run.PersistenceMode ?? PersistenceMode.Text,
                    persistencePath: run.PersistencePath);
        }
    }
}