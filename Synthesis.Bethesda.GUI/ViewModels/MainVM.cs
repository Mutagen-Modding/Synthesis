using DynamicData.Binding;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
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
using Mutagen.Bethesda.Synthesis.Versioning;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.DotNet;

#if !DEBUG
using Noggog.Utility;
using System.Diagnostics;
#endif

namespace Synthesis.Bethesda.GUI
{
    public class MainVM : ViewModel
    {
        private readonly ISelectedProfileControllerVm _SelectedProfile;
        private readonly IRetrieveSaveSettings _Save;
        private readonly ISettings _Settings;
        private readonly IActivePanelControllerVm _ActivePanelControllerVm;
        public ConfigurationVM Configuration { get; }

        private readonly ObservableAsPropertyHelper<ViewModel?> _ActivePanel;
        public ViewModel? ActivePanel => _ActivePanel.Value;

        public ICommand OpenProfilesPageCommand { get; }

        public IConfirmationPanelControllerVm Confirmation { get; }

        // Whether to show red glow in background
        private readonly ObservableAsPropertyHelper<bool> _Hot;
        public bool Hot => _Hot.Value;

        public string SynthesisVersion { get; }

        public string MutagenVersion { get; }

        public IObservable<string?> NewestSynthesisVersion { get; }
        public IObservable<string?> NewestMutagenVersion { get; }

        private readonly ObservableAsPropertyHelper<ConfirmationActionVM?> _ActiveConfirmation;
        public ConfirmationActionVM? ActiveConfirmation => _ActiveConfirmation.Value;

        private readonly ObservableAsPropertyHelper<bool> _InModal;
        public bool InModal => _InModal.Value;

        public IEnvironmentErrorsVM EnvironmentErrors { get; }
        
        public bool IsShutdown { get; private set; }

        public MainVM(
            IProvideInstalledSdk installedSdk,
            IConfirmationPanelControllerVm confirmationControllerVm,
            IProvideCurrentVersions currentVersions,
            ISelectedProfileControllerVm selectedProfile,
            IEnvironmentErrorsVM environmentErrors,
            ISaveSignal saveSignal,
            IRetrieveSaveSettings save,
            ISettings settings,
            IActivePanelControllerVm activePanelControllerVm)
        {
            _SelectedProfile = selectedProfile;
            _Save = save;
            _Settings = settings;
            _ActivePanelControllerVm = activePanelControllerVm;
            _ActivePanel = activePanelControllerVm.WhenAnyValue(x => x.ActivePanel)
                .ToGuiProperty(this, nameof(ActivePanel), default);
            Configuration = new ConfigurationVM(this, selectedProfile, saveSignal)
                .DisposeWith(this);
            activePanelControllerVm.ActivePanel = Configuration;
            Confirmation = confirmationControllerVm;

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
                activePanelControllerVm.ActivePanel = new ProfilesDisplayVM(Configuration, ActivePanel);
            },
            canExecute: Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.Configuration.CurrentRun!.Running, fallback: false),
                    this.WhenAnyValue(x => x.ActivePanel)
                        .Select(x => x is ProfilesDisplayVM),
                    (running, isProfile) => !running && !isProfile));

            Task.Run(() => Mutagen.Bethesda.WarmupAll.Init()).FireAndForget();

            SynthesisVersion = currentVersions.SynthesisVersion;
            MutagenVersion = currentVersions.MutagenVersion;

            var latestVersions = Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .CombineLatest(
                    installedSdk.DotNetSdkInstalled,
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
                    this.WhenAnyFallback(x => x.Configuration.SelectedProfile!.ConsiderPrereleaseNugets),
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
                    this.WhenAnyValue(x => x.Confirmation.TargetConfirmation),
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
            
            EnvironmentErrors = environmentErrors;

            saveSignal.Saving
                .Subscribe((x) => Save(x.Gui, x.Pipe))
                .DisposeWith(this);
        }

        public void Load()
        {
            Configuration.Load(_Settings.Gui, _Settings.Pipeline);
        }

        private void Save(SynthesisGuiSettings guiSettings, PipelineSettings _)
        {
            guiSettings.MainRepositoryFolder = _Settings.Gui.MainRepositoryFolder;
            guiSettings.OpenIdeAfterCreating = _Settings.Gui.OpenIdeAfterCreating;
        }

        public void Init()
        {
            if (Configuration.Profiles.Count == 0)
            {
                _ActivePanelControllerVm.ActivePanel = new NewProfileVM(this.Configuration, (profile) =>
                {
                    profile.Nickname = profile.Release.ToDescriptionString();
                    _SelectedProfile.SelectedProfile = profile;
                    _ActivePanelControllerVm.ActivePanel = Configuration;
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
                var ret = await Inject.Scope.GetInstance<IQueryLibraryVersions>().Query(projPath, current: false, includePrerelease: includePrerelease, CancellationToken.None);
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

        public async void Shutdown()
        {
            IsShutdown = true;
            var toDo = new List<Task>();
            toDo.Add(Task.Run(() =>
            {
                try
                {
                    _Save.Retrieve(out var gui, out var pipe);
                    File.WriteAllText(Execution.Paths.SettingsFileName, JsonConvert.SerializeObject(pipe, Formatting.Indented, Execution.Constants.JsonSettings));
                    File.WriteAllText(Paths.GuiSettingsPath, JsonConvert.SerializeObject(gui, Formatting.Indented, Execution.Constants.JsonSettings));
                    Dispose();
                }
                catch (Exception e)
                {
                    Log.Logger.Error("Error saving settings", e);
                }
            }));
#if !DEBUG
            toDo.Add(Task.Run(async () =>
            {
                try
                {
                    using var process = ProcessWrapper.Create(
                        new ProcessStartInfo("dotnet", $"build-server shutdown"));
                    using var output = process.Output.Subscribe(x => Log.Logger.Information(x));
                    using var error = process.Error.Subscribe(x => Log.Logger.Information(x));
                    var ret = await process.Run();
                }
                catch (Exception e)
                {
                    Log.Logger.Error("Error shutting down build server", e);
                }
            }));
#endif

            toDo.Add(Task.Run(async () =>
            {
                try
                {
                    Log.Logger.Information("Disposing scope");
                    await Inject.Scope.DisposeAsync();
                    Log.Logger.Information("Disposed scope");
                    Log.Logger.Information("Disposing injection");
                    await Inject.Container.DisposeAsync();
                    Log.Logger.Information("Disposed injection");
                }
                catch (Exception e)
                {
                    Log.Logger.Error("Error shutting down injector actions", e);
                }
            }));
            await Task.WhenAll(toDo);
            Application.Current.Shutdown();
        }
    }
}
