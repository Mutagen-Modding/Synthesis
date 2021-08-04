using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;
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
        private readonly IRunProfileProvider _profileProvider;
        private readonly IExecuteRun _executeRun;
        public CancellationToken Cancel { get; }
        public IRunReporter? Reporter { get; }

        public RunPatcherPipeline(
            IRunProfileProvider profileProvider,
            IExecuteRun executeRun,
            CancellationToken cancel, 
            IRunReporter? reporter)
        {
            _profileProvider = profileProvider;
            _executeRun = executeRun;
            Cancel = cancel;
            Reporter = reporter;
        }
        
        public async Task Run(RunPatcherPipelineInstructions run)
        {
            try
            {
                // Locate profile
                if (string.IsNullOrWhiteSpace(run.ProfileDefinitionPath))
                {
                    throw new ArgumentNullException("Profile", "Could not locate profile to run");
                }

                if (_profileProvider.Profile.TargetRelease != run.GameRelease)
                {
                    throw new ArgumentException($"Target game release did not match profile's: {run.GameRelease} != {_profileProvider.Profile.TargetRelease}");
                }

                if (run.LoadOrderFilePath.IsNullOrWhitespace())
                {
                    run.LoadOrderFilePath = PluginListings.GetListingsPath(run.GameRelease);
                }

                Reporter?.Write(default(int), default, "Patchers to run:");
                var patchers = _profileProvider.Profile.Patchers
                    .Where(p => p.On)
                    .Select(patcherSettings =>
                    {
                        if (Reporter != null)
                        {
                            patcherSettings.Print(Reporter);
                        }

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
            catch (Exception ex)
            when (Reporter != null)
            {
                Reporter.ReportOverallProblem(ex);
            }
        }
    }
}