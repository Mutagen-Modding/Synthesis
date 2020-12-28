using Mutagen.Bethesda;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.CLI
{
    /// <summary>
    /// Class that runs the patcher pipeline headless without a GUI
    /// </summary>
    public static class RunPatcherPipeline
    {
        public static async Task Run(RunPatcherPipelineInstructions run, IRunReporter? reporter = null)
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
                            CodeSnippetPatcherSettings snippet => new CodeSnippetPatcherRun(snippet),
                            CliPatcherSettings cli => new CliPatcherRun(
                                cli.Nickname,
                                cli.PathToExecutable,
                                pathToExtra: null),
                            SolutionPatcherSettings sln => new SolutionPatcherRun(
                                name: sln.Nickname,
                                pathToSln: sln.SolutionPath,
                                pathToExtraDataBaseFolder: run.ExtraDataFolder ?? Constants.TypicalExtraData,
                                pathToProj: Path.Combine(Path.GetDirectoryName(sln.SolutionPath)!, sln.ProjectSubpath)),
                            GithubPatcherSettings git => new GitPatcherRun(
                                settings: git,
                                localDir: GitPatcherRun.RunnerRepoDirectory(profile.ID, git.ID)),
                            _ => throw new NotImplementedException(),
                        };
                    })
                    .ToList();

                await Runner.Run(
                    workingDirectory: Constants.ProfileWorkingDirectory(profile.ID),
                    outputPath: run.OutputPath,
                    dataFolder: run.DataFolderPath,
                    loadOrder: LoadOrder.GetListings(run.GameRelease, dataPath: run.DataFolderPath),
                    release: run.GameRelease,
                    patchers: patchers,
                    sourcePath: run.SourcePath == null ? default : ModPath.FromPath(run.SourcePath),
                    reporter: reporter);
            }
            catch (Exception ex)
            when (reporter != null)
            {
                reporter.ReportOverallProblem(ex);
            }
        }
    }
}
