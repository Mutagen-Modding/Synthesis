using CommandLine;
using Mutagen.Bethesda;
using Newtonsoft.Json;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.CLI
{
    public static class Commands
    {
        public static async Task Run(
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
                            CodeSnippetPatcherSettings snippet => new CodeSnippetPatcherRun(snippet),
                            CliPatcherSettings cli => new CliPatcherRun(
                                cli.Nickname,
                                cli.PathToExecutable,
                                pathToExtra: null),
                            SolutionPatcherSettings sln => new SolutionPatcherRun(
                                name: sln.Nickname,
                                pathToSln: sln.SolutionPath,
                                pathToExtraDataBaseFolder: run.ExtraDataFolder ?? Paths.TypicalExtraData,
                                pathToProj: Path.Combine(Path.GetDirectoryName(sln.SolutionPath)!, sln.ProjectSubpath)),
                            GithubPatcherSettings git => new GitPatcherRun(
                                settings: git,
                                localDir: GitPatcherRun.RunnerRepoDirectory(profile.ID, git.ID)),
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

        public static async Task<SettingsConfiguration> GetSettingsStyle(
            string path,
            bool directExe,
            CancellationToken cancel,
            bool build,
            Action<string> log)
        {
            log($"Checking {path} for settings.  Direct exe? {directExe}.  Build? {build}");
            using var proc = ProcessWrapper.Create(
                GetStart(path, directExe, new Synthesis.Bethesda.SettingsQuery(), build: build),
                cancel: cancel,
                hookOntoOutput: true);

            List<string> output = new();
            using var outputSub = proc.Output
                .Subscribe(s => output.Add(s));

            switch ((Codes)await proc.Run())
            {
                case Codes.OpensForSettings:
                    return new SettingsConfiguration(SettingsStyle.Open, Array.Empty<ReflectionSettingsConfig>());
                case Codes.AutogeneratedSettingsClass:
                    return new SettingsConfiguration(
                        SettingsStyle.SpecifiedClass,
                        JsonConvert.DeserializeObject<ReflectionSettingsConfigs>(
                            string.Join(Environment.NewLine, output))!.Configs);
                default:
                    return new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig>());
            }
        }

        public static async Task<int> OpenForSettings(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolderPath,
            IEnumerable<LoadOrderListing> loadOrder,
            Rectangle rect,
            CancellationToken cancel)
        {
            using var loadOrderFile = GetTemporaryLoadOrder(release, loadOrder);

            using var proc = ProcessWrapper.Create(
                GetStart(path, directExe, new Synthesis.Bethesda.OpenForSettings()
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Height = rect.Height,
                    Width = rect.Width,
                    LoadOrderFilePath = loadOrderFile.File.Path,
                    DataFolderPath = dataFolderPath,
                    GameRelease = release,
                }),
                cancel: cancel,
                hookOntoOutput: false);

            return await proc.Run();
        }

        public static async Task<int> OpenSettingHost(
            string patcherName,
            string path,
            GameRelease release,
            string dataFolderPath,
            IEnumerable<LoadOrderListing> loadOrder,
            Rectangle rect,
            CancellationToken cancel)
        {
            using var loadOrderFile = GetTemporaryLoadOrder(release, loadOrder);

            using var proc = ProcessWrapper.Create(
                GetStart("SettingsHost/Synthesis.Bethesda.SettingsHost.exe", directExe: true, new Synthesis.Bethesda.HostSettings()
                {
                    PatcherName = patcherName,
                    PatcherPath = path,
                    Left = rect.Left,
                    Top = rect.Top,
                    Height = rect.Height,
                    Width = rect.Width,
                    LoadOrderFilePath = loadOrderFile.File.Path,
                    DataFolderPath = dataFolderPath,
                    GameRelease = release,
                }),
                cancel: cancel,
                hookOntoOutput: false);

            return await proc.Run();
        }

        public static TempFile GetTemporaryLoadOrder(GameRelease release, IEnumerable<LoadOrderListing> loadOrder)
        {
            var loadOrderFile = new TempFile(
                Path.Combine(Synthesis.Bethesda.Execution.Paths.WorkingDirectory, "RunnabilityChecks", Path.GetRandomFileName()));

            LoadOrder.Write(
                loadOrderFile.File.Path,
                release,
                loadOrder,
                removeImplicitMods: true);

            return loadOrderFile;
        }

        public static async Task<ErrorResponse> CheckRunnability(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            IEnumerable<LoadOrderListing> loadOrder,
            CancellationToken cancel)
        {
            using var loadOrderFile = GetTemporaryLoadOrder(release, loadOrder);

            return await CheckRunnability(
                path,
                directExe: directExe,
                release: release,
                dataFolder: dataFolder,
                loadOrderPath: loadOrderFile.File.Path,
                cancel: cancel);
        }

        public static async Task<ErrorResponse> CheckRunnability(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            string loadOrderPath,
            CancellationToken cancel)
        {
            var checkState = new Synthesis.Bethesda.CheckRunnability()
            {
                DataFolderPath = dataFolder,
                GameRelease = release,
                LoadOrderFilePath = loadOrderPath
            };

            using var proc = ProcessWrapper.Create(
                GetStart(path, directExe, checkState),
                cancel: cancel);

            var results = new List<string>();
            void AddResult(string s)
            {
                if (results.Count < 100)
                {
                    results.Add(s);
                }
            }
            using var ouputSub = proc.Output.Subscribe(AddResult);
            using var errSub = proc.Error.Subscribe(AddResult);

            var result = await proc.Run();

            if (result == (int)Codes.NotRunnable)
            {
                return ErrorResponse.Fail(string.Join(Environment.NewLine, results));
            }

            // Other error codes are likely the target app just not handling runnability checks, so return as runnable unless
            // explicity told otherwise with the above error code
            return ErrorResponse.Success;
        }

        private static ProcessStartInfo GetStart(string path, bool directExe, object args, bool build = false)
        {
            if (directExe)
            {
                return new ProcessStartInfo(path, Parser.Default.FormatCommandLine(args));
            }
            else
            {
                return new ProcessStartInfo("dotnet", $"run --project \"{path}\" --runtime win-x64{(build ? string.Empty : " --no-build")} {Parser.Default.FormatCommandLine(args)}");
            }
        }
    }
}
