using DynamicData;
using DynamicData.Binding;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using Noggog;
using Mutagen.Bethesda;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive;
using System.IO;
using Synthesis.Bethesda.Execution;
using Mutagen.Bethesda.Synthesis;
using Noggog.Utility;
using System.Diagnostics;
using System.Threading;

namespace Synthesis.Bethesda.GUI
{
    public class MainVM : ViewModel
    {
        public ConfigurationVM Configuration { get; }
        public SynthesisGuiSettings Settings { get; private set; } = new SynthesisGuiSettings();

        [Reactive]
        public ViewModel ActivePanel { get; set; }

        public ICommand ConfirmActionCommand { get; }
        public ICommand DiscardActionCommand { get; }
        public ICommand OpenProfilesPageCommand { get; }

        [Reactive]
        public ConfirmationActionVM? ActiveConfirmation { get; set; }

        public ObservableCollectionExtended<IDE> IdeOptions { get; } = new ObservableCollectionExtended<IDE>();

        [Reactive]
        public IDE Ide { get; set; }

        // Whether to show red glow in background
        private readonly ObservableAsPropertyHelper<bool> _Hot;
        public bool Hot => _Hot.Value;

        public string SynthesisVersion { get; }

        public string MutagenVersion { get; }

        public IObservable<string?> NewestSynthesisVersion { get; }
        public IObservable<string?> NewestMutagenVersion { get; }
        public IObservable<Version?> DotNetSdkInstalled { get; }

        public MainVM()
        {
            var dotNet = Observable.Interval(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                .StartWith(0)
                .SelectTask(async i =>
                {
                    try
                    {
                        var ret = await DotNetQueries.DotNetSdkVersion(CancellationToken.None);
                        Log.Logger.Information($"dotnet SDK: {ret}");
                        return ret;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, $"Error retrieving dotnet SDK version");
                        return default(Version?);
                    }
                });
            DotNetSdkInstalled = dotNet
                .Take(1)
                .Merge(dotNet
                    .FirstAsync(v => v != null))
                .DistinctUntilChanged()
                .Replay(1)
                .RefCount();

            Configuration = new ConfigurationVM(this);
            ActivePanel = Configuration;
            DiscardActionCommand = ReactiveCommand.Create(() => ActiveConfirmation = null);
            ConfirmActionCommand = ReactiveCommand.Create(
                () =>
                {
                    if (ActiveConfirmation == null) return;
                    ActiveConfirmation.ToDo();
                    ActiveConfirmation = null;
                });

            _Hot = this.WhenAnyValue(x => x.ActivePanel)
                .Select(x =>
                {
                    switch (x)
                    {
                        case ConfigurationVM config:
                            return config.WhenAnyFallback(x => x.CurrentRun!.Running, fallback: false);
                        case PatchersRunVM running:
                            return running.WhenAnyValue(x => x.Running);
                        default:
                            break;
                    }
                    return Observable.Return(false);
                })
                .Switch()
                .DistinctUntilChanged()
                .ToGuiProperty(this, nameof(Hot));

            OpenProfilesPageCommand = ReactiveCommand.Create(() =>
            {
                ActivePanel = new ProfilesDisplayVM(Configuration, ActivePanel);
            },
            canExecute: Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.Configuration.CurrentRun!.Running, fallback: false),
                    this.WhenAnyValue(x => x.ActivePanel)
                        .Select(x => x is ProfilesDisplayVM),
                    (running, isProfile) => !running && !isProfile));

            IdeOptions.AddRange(EnumExt.GetValues<IDE>());

            Task.Run(() => Mutagen.Bethesda.WarmupAll.Init()).FireAndForget();

            SynthesisVersion = Mutagen.Bethesda.Synthesis.Versions.SynthesisVersion;
            MutagenVersion = Mutagen.Bethesda.Synthesis.Versions.MutagenVersion;

            var latestVersions = Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .CombineLatest(
                    DotNetSdkInstalled,
                    (_, v) => v)
                .SelectTask(async v =>
                {
                    var normalUpdateTask = GetLatestVersions(v, includePrerelease: false);
                    var prereleaseUpdateTask = GetLatestVersions(v, includePrerelease: true);
                    await Task.WhenAll(normalUpdateTask, prereleaseUpdateTask);
                    return (Normal: await normalUpdateTask, Prerelease: await prereleaseUpdateTask);
                })
                .Replay(1)
                .RefCount();
            NewestMutagenVersion = Observable.CombineLatest(
                    latestVersions,
                    this.WhenAnyFallback(x=> x.Configuration.SelectedProfile!.ConsiderPrereleaseNugets),
                    (vers, prereleases) => prereleases ? vers.Prerelease.MutagenVersion : vers.Normal.MutagenVersion)
                .Replay(1)
                .RefCount();
            NewestSynthesisVersion = Observable.CombineLatest(
                    latestVersions,
                    this.WhenAnyFallback(x => x.Configuration.SelectedProfile!.ConsiderPrereleaseNugets),
                    (vers, prereleases) => prereleases ? vers.Prerelease.SynthesisVersion : vers.Normal.SynthesisVersion)
                .Replay(1)
                .RefCount();

            // Switch to DotNet screen if missing
            DotNetSdkInstalled
                .Subscribe(v =>
                {
                    if (v == null)
                    {
                        ActivePanel = new DotNetNotInstalledVM(this, this.ActivePanel, DotNetSdkInstalled);
                    }
                });
        }

        public void Load(SynthesisGuiSettings? guiSettings, PipelineSettings? pipeSettings)
        {
            if (guiSettings != null)
            {
                Settings = guiSettings;
                Ide = guiSettings.Ide;
            }
            Configuration.Load(Settings, pipeSettings ?? new PipelineSettings());
        }

        public void Save(out SynthesisGuiSettings guiSettings, out PipelineSettings pipeSettings)
        {
            Configuration.Save(out guiSettings, out pipeSettings);
            guiSettings.Ide = this.Ide;
            guiSettings.MainRepositoryFolder = Settings.MainRepositoryFolder;
            guiSettings.OpenIdeAfterCreating = Settings.OpenIdeAfterCreating;
        }

        public void Init()
        {
            if (Configuration.Profiles.Count == 0)
            {
                ActivePanel = new NewProfileVM(this.Configuration, (profile) =>
                {
                    profile.Nickname = profile.Release.ToDescriptionString();
                    Configuration.SelectedProfile = profile;
                    Configuration.MainVM.ActivePanel = Configuration;
                });
            }
        }

        public async Task<(string? MutagenVersion, string? SynthesisVersion)> GetLatestVersions(Version? dotNetVersion, bool includePrerelease)
        {
            try
            {
                if (dotNetVersion == null)
                {
                    Log.Logger.Error("Can not query for latest nuget versions as there is not dotnet SDK installed.");
                    return (null, null);
                }
                Log.Logger.Information("Querying for latest published library versions");
                var bootstrapProjectDir = new DirectoryPath(Path.Combine(Execution.Constants.WorkingDirectory, "VersionQuery"));
                bootstrapProjectDir.DeleteEntireFolder();
                bootstrapProjectDir.Create();
                var slnPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.sln");
                SolutionInitialization.CreateSolutionFile(slnPath);
                var projPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.csproj");
                SolutionInitialization.CreateProject(projPath, GameCategory.Skyrim, insertOldVersion: true);
                SolutionInitialization.AddProjectToSolution(slnPath, projPath);
                var ret = await DotNetQueries.QuerySynthesisVersions(projPath, current: false, includePrerelease: includePrerelease, CancellationToken.None);
                Log.Logger.Information("Latest published library versions:");
                Log.Logger.Information($"  Mutagen: {ret.MutagenVersion}");
                Log.Logger.Information($"  Synthesis: {ret.SynthesisVersion}");
                return (ret.MutagenVersion ?? this.MutagenVersion, ret.SynthesisVersion ?? this.SynthesisVersion);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error querying for latest nuget versions");
                return (null, null);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Task.Run(async () =>
            {
                using var process = ProcessWrapper.Create(
                    new ProcessStartInfo("dotnet", $"build-server shutdown"));
                using var output = process.Output.Subscribe(x => Log.Logger.Information(x));
                using var error = process.Error.Subscribe(x => Log.Logger.Information(x));
                var ret = await process.Run();
                return ret;
            }).Wait();
        }
    }
}
