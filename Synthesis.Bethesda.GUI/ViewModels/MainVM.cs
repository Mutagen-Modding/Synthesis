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
using System.Threading;
using System.Windows;
using System.Drawing;
#if !DEBUG
using Noggog.Utility;
using System.Diagnostics;
#endif

namespace Synthesis.Bethesda.GUI
{
    public class MainVM : ViewModel
    {
        public ConfigurationVM Configuration { get; }
        public SynthesisGuiSettings Settings { get; private set; } = new SynthesisGuiSettings();

        [Reactive]
        public ViewModel ActivePanel { get; set; }

        public ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
        public ReactiveCommand<Unit, Unit> DiscardActionCommand { get; }
        public ICommand OpenProfilesPageCommand { get; }

        [Reactive]
        public ConfirmationActionVM? TargetConfirmation { get; set; }

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
        public IObservable<DotNetVersion> DotNetSdkInstalled { get; }

        private readonly Window _window;
        public Rectangle Rectangle => new(
            x: (int)_window.Left,
            y: (int)_window.Top,
            width: (int)_window.Width,
            height: (int)_window.Height);

        private readonly ObservableAsPropertyHelper<ConfirmationActionVM?> _ActiveConfirmation;
        public ConfirmationActionVM? ActiveConfirmation => _ActiveConfirmation.Value;

        private readonly ObservableAsPropertyHelper<bool> _InModal;
        public bool InModal => _InModal.Value;

        public EnvironmentErrorsVM EnvironmentErrors { get; }

        public MainVM(Window window)
        {
            _window = window;
            var dotNet = Observable.Interval(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                .StartWith(0)
                .SelectTask(async i =>
                {
                    try
                    {
                        return await DotNetCommands.DotNetSdkVersion(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, $"Error retrieving dotnet SDK version");
                        return new DotNetVersion(string.Empty, false);
                    }
                });
            DotNetSdkInstalled = dotNet
                .Take(1)
                .Merge(dotNet
                    .FirstAsync(v => v != null))
                .DistinctUntilChanged()
                .Do(x => Log.Logger.Information($"dotnet SDK: {x}"))
                .Replay(1)
                .RefCount();

            Configuration = new ConfigurationVM(this)
                .DisposeWith(this);
            ActivePanel = Configuration;
            DiscardActionCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.TargetConfirmation),
                canExecute: target => target != null,
                execute: (_) =>
                {
                    TargetConfirmation = null;
                },
                disposable: this);
            ConfirmActionCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyFallback(x => x.TargetConfirmation!.ToDo),
                canExecute: toDo => toDo != null,
                execute: toDo =>
                {
                    toDo?.Invoke();
                    TargetConfirmation = null;
                },
                disposable: this);

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
                    (_, DotNetVersions) => DotNetVersions)
                .SelectTask(async x =>
                {
                    try
                    {
                        if (!x.Acceptable)
                        {
                            Log.Logger.Error("Can not query for latest nuget versions as there is no acceptable dotnet SDK installed.");
                            return (Normal: (MutagenVersion: default(string?), SynthesisVersion: default(string?)), Prerelease: (MutagenVersion: default(string?), SynthesisVersion: default(string?)));
                        }

                        var projPath = PrepLatestVersionProject();
                    
                        Log.Logger.Information("Querying for latest published library versions");
                        var normalUpdate = await GetLatestVersions(includePrerelease: false, projPath);
                        var prereleaseUpdate = await GetLatestVersions(includePrerelease: true, projPath);
                        return (Normal: normalUpdate, Prerelease: prereleaseUpdate);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error($"Error querying for versions: {e}");
                        return (Normal: (MutagenVersion: default(string?), SynthesisVersion: default(string?)), Prerelease: (MutagenVersion: default(string?), SynthesisVersion: default(string?)));
                    }
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

            _ActiveConfirmation = Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.Configuration.SelectedProfile!.SelectedPatcher)
                        .Select(x =>
                        {
                            if (x is not GitPatcherVM gitPatcher) return Observable.Return(default(GitPatcherVM?));
                            return gitPatcher.WhenAnyValue(x => x.PatcherSettings.SettingsOpen)
                                .Select(open => open ? (GitPatcherVM?)gitPatcher : null);
                        })
                        .Switch(),
                    this.WhenAnyValue(x => x.TargetConfirmation),
                    (openPatcher, target) =>
                    {
                        if (target != null) return target;
                        if (openPatcher == null) return default(ConfirmationActionVM?);
                        return new ConfirmationActionVM(
                            "External Patcher Settings Open",
                            $"{openPatcher.Nickname} is open for settings manipulation.",
                            toDo: null);
                    })
                .ToGuiProperty(this, nameof(ActiveConfirmation), default(ConfirmationActionVM?));

            _InModal = this.WhenAnyValue(x => x.ActiveConfirmation)
                .Select(x => x != null)
                .ToGuiProperty(this, nameof(InModal));
            
            EnvironmentErrors = new EnvironmentErrorsVM(this);
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
        
        public string PrepLatestVersionProject()
        {
            var bootstrapProjectDir = new DirectoryPath(Path.Combine(Execution.Paths.WorkingDirectory, "VersionQuery"));
            bootstrapProjectDir.DeleteEntireFolder();
            bootstrapProjectDir.Create();
            var slnPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.sln");
            SolutionInitialization.CreateSolutionFile(slnPath);
            var projPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.csproj");
            SolutionInitialization.CreateProject(projPath, GameCategory.Skyrim, insertOldVersion: true);
            SolutionInitialization.AddProjectToSolution(slnPath, projPath);
            return projPath;
        }

        public async Task<(string? MutagenVersion, string? SynthesisVersion)> GetLatestVersions(bool includePrerelease, string projPath)
        {
            try
            {
                var ret = await DotNetCommands.QuerySynthesisVersions(projPath, current: false, includePrerelease: includePrerelease, CancellationToken.None);
                Log.Logger.Information($"Latest published {(includePrerelease ? " prerelease" : null)} library versions:");
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
#if !DEBUG
            Task.Run(async () =>
            {
                using var process = ProcessWrapper.Create(
                    new ProcessStartInfo("dotnet", $"build-server shutdown"));
                using var output = process.Output.Subscribe(x => Log.Logger.Information(x));
                using var error = process.Error.Subscribe(x => Log.Logger.Information(x));
                var ret = await process.Run();
                return ret;
            }).Wait();
#endif
        }
    }
}
