using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IRunPatcherPipeline
    {
        Task Run(
            RunPatcherPipelineInstructions run, 
            CancellationToken cancel, 
            IRunReporter? reporter = null);
    }

    public class RunPatcherPipeline : IRunPatcherPipeline
    {
        private readonly ICheckRunnability _Runnability;

        public RunPatcherPipeline(ICheckRunnability runnability)
        {
            _Runnability = runnability;
        }
        
        public async Task Run(
            RunPatcherPipelineInstructions run, 
            CancellationToken cancel, 
            IRunReporter? reporter = null)
        {
            try
            {
                // Locate profile
                if (string.IsNullOrWhiteSpace(run.ProfileDefinitionPath))
                {
                    throw new ArgumentNullException("Profile", "Could not locate profile to run");
                }

                SynthesisProfile? profile;
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

                reporter?.Write(default, "Patchers to run:");
                var patchers = profile.Patchers
                    .Where(p => p.On)
                    .Select<PatcherSettings, IPatcherRun>(patcherSettings =>
                    {
                        if (reporter != null)
                        {
                            patcherSettings.Print(reporter);
                        }
                        return patcherSettings switch
                        {
                            CliPatcherSettings cli => new CliPatcherRun(
                                cli.Nickname,
                                cli.PathToExecutable,
                                pathToExtra: null),
                            SolutionPatcherSettings sln => new SolutionPatcherRun(
                                name: sln.Nickname,
                                pathToSln: sln.SolutionPath,
                                pathToExtraDataBaseFolder: run.ExtraDataFolder ?? Paths.TypicalExtraData,
                                pathToProj: Path.Combine(Path.GetDirectoryName(sln.SolutionPath)!, sln.ProjectSubpath),
                                checkRunnability: _Runnability),
                            GithubPatcherSettings git => new GitPatcherRun(
                                settings: git,
                                localDir: GitPatcherRun.RunnerRepoDirectory(profile.ID, git.ID),
                                checkOrClone: new CheckOrCloneRepo(new DeleteOldRepo())),
                            _ => throw new NotImplementedException(),
                        };
                    })
                    .ToList();

                await Runner.Run(
                    workingDirectory: Paths.ProfileWorkingDirectory(profile.ID),
                    outputPath: run.OutputPath,
                    dataFolder: run.DataFolderPath,
                    loadOrder: LoadOrder.GetListings(
                        run.GameRelease, 
                        dataPath: run.DataFolderPath,
                        pluginsFilePath: run.LoadOrderFilePath,
                        creationClubFilePath: CreationClubListings.GetListingsPath(
                            run.GameRelease.ToCategory(),
                            run.DataFolderPath)),
                    release: run.GameRelease,
                    patchers: patchers,
                    sourcePath: run.SourcePath == null ? default : ModPath.FromPath(run.SourcePath),
                    reporter: reporter,
                    cancel: cancel,
                    persistenceMode: run.PersistenceMode ?? PersistenceMode.Text,
                    persistencePath: run.PersistencePath);
            }
            catch (Exception ex)
            when (reporter != null)
            {
                reporter.ReportOverallProblem(ex);
            }
        }

    }
}