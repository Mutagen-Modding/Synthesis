using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Newtonsoft.Json;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
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
        private readonly IBuild _Build;
        private readonly IProvideWorkingDirectory _WorkingDirectory;
        private readonly IWorkingDirectorySubPaths _Paths;
        private readonly ICheckOrCloneRepo _CheckOrCloneRepo;
        private readonly IProvideRepositoryCheckouts _RepositoryCheckouts;
        private readonly IProcessFactory _ProcessFactory;
        private readonly ICheckRunnability _Runnability;
        private readonly IRunner _Runner;
        public CancellationToken Cancel { get; }
        public IRunReporter? Reporter { get; }

        public RunPatcherPipeline(
            IBuild build,
            IProvideWorkingDirectory workingDirectory,
            IWorkingDirectorySubPaths paths,
            ICheckOrCloneRepo checkOrCloneRepo,
            IProvideRepositoryCheckouts repositoryCheckouts,
            IProcessFactory processFactory,
            ICheckRunnability runnability,
            IRunner runner,
            CancellationToken cancel, 
            IRunReporter? reporter)
        {
            _Build = build;
            _WorkingDirectory = workingDirectory;
            _Paths = paths;
            _CheckOrCloneRepo = checkOrCloneRepo;
            _RepositoryCheckouts = repositoryCheckouts;
            _ProcessFactory = processFactory;
            _Runnability = runnability;
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

                ISynthesisProfile? profile;
                if (string.IsNullOrWhiteSpace(run.ProfileName))
                {
                    profile = JsonConvert.DeserializeObject<SynthesisProfile>(File.ReadAllText(run.ProfileDefinitionPath), Constants.JsonSettings)!;
                }
                else
                {
                    var settings = JsonConvert.DeserializeObject<PipelineSettings>(File.ReadAllText(run.ProfileDefinitionPath), Constants.JsonSettings)!;
                    profile = settings.Profiles.FirstOrDefault(profile =>
                    {
                        if (run.ProfileName.Equals(profile.Nickname)) return true;
                        if (run.ProfileName.Equals(profile.ID)) return true;
                        return false;
                    });
                }

                if (string.IsNullOrWhiteSpace(profile?.ID))
                {
                    throw new ArgumentException("File did not point to a valid profile");
                }

                if (profile.TargetRelease != run.GameRelease)
                {
                    throw new ArgumentException($"Target game release did not match profile's: {run.GameRelease} != {profile.TargetRelease}");
                }

                if (run.LoadOrderFilePath.IsNullOrWhitespace())
                {
                    run.LoadOrderFilePath = PluginListings.GetListingsPath(run.GameRelease);
                }

                Reporter?.Write(default, "Patchers to run:");
                var patchers = profile.Patchers
                    .Where(p => p.On)
                    .Select<PatcherSettings, IPatcherRun>(patcherSettings =>
                    {
                        if (Reporter != null)
                        {
                            patcherSettings.Print(Reporter);
                        }
                        return patcherSettings switch
                        {
                            CliPatcherSettings cli => new CliPatcherRun(
                                _ProcessFactory,
                                cli.Nickname,
                                cli.PathToExecutable,
                                pathToExtra: null),
                            SolutionPatcherSettings sln => new SolutionPatcherRun(
                                name: sln.Nickname,
                                pathToSln: sln.SolutionPath,
                                pathToExtraDataBaseFolder: run.ExtraDataFolder ?? _Paths.TypicalExtraData,
                                pathToProj: Path.Combine(Path.GetDirectoryName(sln.SolutionPath)!, sln.ProjectSubpath),
                                checkRunnability: _Runnability,
                                processFactory: _ProcessFactory,
                                repositoryCheckouts: _RepositoryCheckouts,
                                build: _Build),
                            GithubPatcherSettings git => new GitPatcherRun(
                                settings: git,
                                localDir: GitPatcherRun.RunnerRepoDirectory(_WorkingDirectory, profile.ID, git.ID),
                                checkOrClone: _CheckOrCloneRepo),
                            _ => throw new NotImplementedException(),
                        };
                    })
                    .ToList();

                await _Runner
                    .Run(
                    workingDirectory: _Paths.ProfileWorkingDirectory(profile.ID),
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