using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running
{
    public interface IRunPatcherPipeline
    {
        Task Run(RunPatcherPipelineInstructions run);
    }

    public class RunPatcherPipeline : IRunPatcherPipeline
    {
        private readonly IWorkingDirectorySubPaths _Paths;
        private readonly IRunProfileProvider _profileProvider;
        private readonly IRunner _Runner;
        public CancellationToken Cancel { get; }
        public IRunReporter? Reporter { get; }

        public RunPatcherPipeline(
            IWorkingDirectorySubPaths paths,
            IRunProfileProvider profileProvider,
            IRunner runner,
            CancellationToken cancel, 
            IRunReporter? reporter)
        {
            _Paths = paths;
            _profileProvider = profileProvider;
            _Runner = runner;
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

                Reporter?.Write(null, default, "Patchers to run:");
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

                await _Runner
                    .Run(
                    workingDirectory: _Paths.ProfileWorkingDirectory(_profileProvider.Profile.ID),
                    outputPath: run.OutputPath,
                    patchers: patchers,
                    sourcePath: run.SourcePath == null ? default : ModPath.FromPath(run.SourcePath),
                    reporter: Reporter,
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