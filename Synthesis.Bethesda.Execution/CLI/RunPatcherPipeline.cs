using Microsoft.Build.Logging;
using Mutagen.Bethesda;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers;
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
        public static async Task Run(RunPatcherPipelineInstructions run, ILogger logger)
        {
            // Locate profile
            if (string.IsNullOrWhiteSpace(run.ProfileDefinitionPath))
            {
                throw new ArgumentNullException("Profile", "Could not locate profile to run");
            }

            SynthesisProfile profile;
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

            if (string.IsNullOrWhiteSpace(profile.ID))
            {
                throw new ArgumentException("File did not point to a valid profile");
            }

            if (profile.TargetRelease != run.GameRelease)
            {
                throw new ArgumentException($"Target game release did not match profile's: {run.GameRelease} != {profile.TargetRelease}");
            }

            if (run.LoadOrderFilePath.IsNullOrWhitespace())
            {
                if (!LoadOrder.TryGetPluginsFile(run.GameRelease, out var pluginPath))
                {
                    throw new ArgumentException("Could not locate plugins file");
                }
                run.LoadOrderFilePath = pluginPath.Path;
            }

            var patchers = profile.Patchers
                .Where(p => p.On)
                .Select<PatcherSettings, IPatcherRun>(patcherSettings =>
                {
                    patcherSettings.Print(logger);
                    return patcherSettings switch
                    {
                        CodeSnippetPatcherSettings snippet => new CodeSnippetPatcherRun(snippet),
                        CliPatcherSettings cli => new CliPatcherRun(
                            cli.Nickname,
                            cli.PathToExecutable,
                            pathToExtra: null),
                        SolutionPatcherSettings sln => new SolutionPatcherRun(
                            nickname: sln.Nickname,
                            pathToSln: sln.SolutionPath,
                            pathToProj: Path.Combine(Path.GetDirectoryName(sln.SolutionPath)!, sln.ProjectSubpath),
                            pathToExe: null),
                        GithubPatcherSettings git => new GitPatcherRun(
                            nickname: git.Nickname,
                            remote: git.RemoteRepoPath,
                            localDir: GitPatcherRun.RunnerRepoDirectory(profile.ID, git.ID),
                            projSubpath: git.SelectedProjectSubpath),
                        _ => throw new NotImplementedException(),
                    };
                })
                .ToList();

            System.Console.WriteLine("Prepping all patchers");
            await Task.WhenAll(patchers.Select(p => Task.Run(() => p.Prep(run.GameRelease, logger))));

            foreach (var patcherRun in patchers)
            {
                System.Console.WriteLine($"Starting: {patcherRun.Name}");
                await patcherRun.Run(
                    new RunSynthesisPatcher()
                    {
                        DataFolderPath = run.DataFolderPath,
                        GameRelease = run.GameRelease,
                        LoadOrderFilePath = run.LoadOrderFilePath,
                        OutputPath = run.OutputPath,
                        SourcePath = run.SourcePath
                    },
                    log: logger);
                System.Console.WriteLine($"Finished: {patcherRun.Name}");
            }
            System.Console.WriteLine($"Patching pipeline complete");
        }
    }
}
