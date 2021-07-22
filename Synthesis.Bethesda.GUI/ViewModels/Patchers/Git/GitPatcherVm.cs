using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Services.Patchers.Git;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Git
{
    public class GitPatcherVm : PatcherVm, IPathToProjProvider, IPathToSolutionFileProvider
    {
        private readonly ILogger _Logger;
        private readonly IToSolutionRunner _toSolutionRunner;
        public override bool IsNameEditable => false;

        public ISelectedProjectInputVm SelectedProjectInput { get; }
        public IGitRemoteRepoPathInputVm RemoteRepoPathInput { get; }

        private readonly ObservableAsPropertyHelper<ConfigurationState> _State;
        public override ConfigurationState State => _State?.Value ?? ConfigurationState.Success;

        public string ID { get; private set; } = string.Empty;

        public string LocalDriverRepoDirectory { get; }
        public string LocalRunnerRepoDirectory { get; }

        private readonly ObservableAsPropertyHelper<ErrorResponse> _RepoValidity;
        public ErrorResponse RepoValidity => _RepoValidity.Value;

        public IObservableCollection<string> AvailableProjects { get; }

        public IObservableCollection<string> AvailableTags { get; }

        private readonly ObservableAsPropertyHelper<RunnerRepoInfo?> _RunnableData;
        public RunnerRepoInfo? RunnableData => _RunnableData.Value;

        public ICommand OpenGitPageCommand { get; }

        public ICommand OpenGitPageToVersionCommand { get; }

        public ICommand NavigateToInternalFilesCommand { get; }

        private readonly ObservableAsPropertyHelper<bool> _AttemptedCheckout;
        public bool AttemptedCheckout => _AttemptedCheckout.Value;

        public PatcherSettingsVm PatcherSettings { get; }

        public record StatusRecord(string Text, bool Processing, bool Blocking, ICommand? Command);
        private readonly ObservableAsPropertyHelper<StatusRecord> _StatusDisplay;
        public StatusRecord StatusDisplay => _StatusDisplay.Value;

        [Reactive]
        public GithubPatcherLastRunState? LastSuccessfulRun { get; set; }

        public ICommand SetToLastSuccessfulRunCommand { get; }
        
        public ILockToCurrentVersioning Locking { get; }
        public IRepoClonesValidStateVm RepoClonesValid { get; }
        public INugetDiffProviderVm NugetDiff { get; }
        public IGitPatcherTargetingVm PatcherTargeting { get; }
        public IGitNugetTargetingVm NugetTargeting { get; }
        public IUpdateAllCommand UpdateAllCommand { get; }

        public GitPatcherVm(
            IGithubPatcherIdentifier ident,
            IPatcherNameVm nameVm,
            ISelectedProjectInputVm selectedProjectInput,
            IGitRemoteRepoPathInputVm remoteRepoPathInputVm,
            IRemovePatcherFromProfile remove,
            INavigateTo navigate, 
            IAvailableTags availableTags,
            IRunnerRepositoryPreparation runnerRepositoryState,
            IPatcherRunnabilityCliState runnabilityCliState,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            ILockToCurrentVersioning lockToCurrentVersioning,
            IInstalledSdkProvider dotNetInstalled,
            IEnvironmentErrorsVm envErrors,
            IAvailableProjects availableProjects,
            ICompliationProvider compliationProvider,
            IDriverRepositoryPreparation driverRepositoryPreparation,
            IBaseRepoDirectoryProvider baseRepoDir,
            IDriverRepoDirectoryProvider driverRepoDirectoryProvider,
            IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
            IGetRepoPathValidity getRepoPathValidity,
            IRepoClonesValidStateVm repoClonesValid,
            INugetDiffProviderVm nugetDiff,
            IMissingMods missingMods,
            ILogger logger,
            IRunnableStateProvider runnableStateProvider,
            IToSolutionRunner toSolutionRunner,
            IGitPatcherTargetingVm patcherTargeting,
            ICheckoutInputProvider checkoutInputProvider,
            IGitNugetTargetingVm nugetTargetingVm,
            IUpdateAllCommand updateAllCommand,
            IAttemptedCheckout attemptedCheckout,
            PatcherSettingsVm.Factory settingsVmFactory,
            GithubPatcherSettings? settings = null)
            : base(nameVm, remove, selPatcher, confirmation, settings)
        {
            _Logger = logger;
            SelectedProjectInput = selectedProjectInput;
            RemoteRepoPathInput = remoteRepoPathInputVm;
            _toSolutionRunner = toSolutionRunner;
            Locking = lockToCurrentVersioning;
            RepoClonesValid = repoClonesValid;
            NugetDiff = nugetDiff;
            PatcherTargeting = patcherTargeting;
            NugetTargeting = nugetTargetingVm;
            UpdateAllCommand = updateAllCommand;

            ID = ident.Id;
            
            CopyInSettings(settings);

            LocalDriverRepoDirectory = driverRepoDirectoryProvider.Path.Path;
            LocalRunnerRepoDirectory = runnerRepoDirectoryProvider.Path.Path;

            _RepoValidity = getRepoPathValidity.RepoPath
                .Select(r => r.RunnableState)
                .ToGuiProperty(this, nameof(RepoValidity));

            var driverRepoInfo = driverRepositoryPreparation.DriverInfo;

            AvailableProjects = availableProjects.Projects;

            AvailableTags = availableTags.Tags;

            _AttemptedCheckout = checkoutInputProvider.Input
                .Select(attemptedCheckout.Attempted)
                .ToGuiProperty(this, nameof(AttemptedCheckout));

            _RunnableData = runnableStateProvider.State
                .Select(x => x.Item ?? default(RunnerRepoInfo?))
                .ToGuiProperty(this, nameof(RunnableData), default(RunnerRepoInfo?));

            _State = Observable.CombineLatest(
                    driverRepoInfo
                        .Select(x => x.ToUnit()),
                    runnerRepositoryState.State,
                    runnableStateProvider.State
                        .Select(x => x.ToUnit()),
                    runnabilityCliState.Runnable,
                    dotNetInstalled.DotNetSdkInstalled
                        .Select(x => (x, true))
                        .StartWith((new DotNetVersion(string.Empty, false), false)),
                    envErrors.WhenAnyFallback(x => x.ActiveError!.ErrorString),
                    missingMods.Missing
                        .QueryWhenChanged()
                        .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                        .StartWith(ListExt.Empty<ModKey>()),
                    (driver, runner, checkout, runnability, dotnet, envError, reqModsMissing) =>
                    {
                        if (driver.IsHaltingError) return driver;
                        if (runner.IsHaltingError) return runner;
                        if (!dotnet.Item2)
                        {
                            return new ConfigurationState(ErrorResponse.Fail("Determining DotNet SDK installed"))
                            {
                                IsHaltingError = false
                            };
                        }
                        if (!dotnet.Item1.Acceptable) return new ConfigurationState(ErrorResponse.Fail("No DotNet SDK installed"));
                        if (envError != null)
                        {
                            return new ConfigurationState(ErrorResponse.Fail(envError));
                        }
                        if (reqModsMissing.Count > 0)
                        {
                            return new ConfigurationState(ErrorResponse.Fail($"Required mods missing from load order:{Environment.NewLine}{string.Join(Environment.NewLine, reqModsMissing)}"));
                        }
                        if (runnability.RunnableState.Failed)
                        {
                            return runnability.BubbleError();
                        }
                        if (checkout.RunnableState.Failed)
                        {
                            return checkout.BubbleError();
                        }
                        Logger.Information("State returned success!");
                        return ConfigurationState.Success;
                    })
                .ToGuiProperty<ConfigurationState>(this, nameof(State), new ConfigurationState(ErrorResponse.Fail("Evaluating"))
                {
                    IsHaltingError = false
                });

            OpenGitPageCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.RepoValidity)
                    .Select(x => x.Succeeded),
                execute: () => navigate.Navigate(RemoteRepoPathInput.RemoteRepoPath));

            OpenGitPageToVersionCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.RunnableData)
                    .Select(x => x != null),
                execute: () =>
                {
                    try
                    {
                        if (!RunnableData.TryGet(out var runnable)) return;
                        if (runnable.Target == null)
                        {
                            navigate.Navigate(RemoteRepoPathInput.RemoteRepoPath);
                        }
                        else
                        {
                            navigate.Navigate(Path.Combine(RemoteRepoPathInput.RemoteRepoPath, "tree", runnable.Target));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error opening Git webpage", ex);
                    }
                });

            NavigateToInternalFilesCommand = ReactiveCommand.Create(() => navigate.Navigate(baseRepoDir.Path));


            PatcherSettings = settingsVmFactory(
                Logger, false, 
                compliationProvider.State.Select(c =>
                    {
                        if (c.RunnableState.Failed) return (c.RunnableState.BubbleFailure<FilePath>(), null);
                        return (GetResponse<FilePath>.Succeed(c.Item.ProjPath), c.Item.TargetSynthesisVersion);
                    })
                    .DistinctUntilChanged(x => (x.Item1.Value, x.TargetSynthesisVersion)))
                .DisposeWith(this);

            _StatusDisplay = Observable.CombineLatest(
                driverRepoInfo,
                runnableStateProvider.State,
                compliationProvider.State,
                runnabilityCliState.Runnable,
                (driver, runnable, comp, runnability) =>
                {
                    if (driver.RunnableState.Failed)
                    {
                        if (driver.IsHaltingError)
                        {
                            return new StatusRecord(
                                Text: "Blocking Error",
                                Processing: false,
                                Blocking: true,
                                Command: null);
                        }
                        return new StatusRecord(
                            Text: "Analyzing repository",
                            Processing: true,
                            Blocking: false,
                            Command: null);
                    }
                    if (runnable.RunnableState.Failed)
                    {
                        if (runnable.IsHaltingError)
                        {
                            return new StatusRecord(
                                Text: "Blocking Error",
                                Processing: false,
                                Blocking: true,
                                Command: null);
                        }
                        return new StatusRecord(
                            Text: "Checking out desired state",
                            Processing: true,
                            Blocking: false,
                            Command: null);
                    }
                    if (comp.RunnableState.Failed)
                    {
                        if (comp.IsHaltingError)
                        {
                            return new StatusRecord(
                                Text: "Blocking Error",
                                Processing: false,
                                Blocking: true,
                                Command: null);
                        }
                        return new StatusRecord(
                            Text: "Compiling",
                            Processing: true,
                            Blocking: false,
                            Command: null);
                    }
                    if (runnability.RunnableState.Failed)
                    {
                        if (runnability.IsHaltingError)
                        {
                            return new StatusRecord(
                                Text: "Blocking Error",
                                Processing: false,
                                Blocking: true,
                                Command: null);
                        }
                        return new StatusRecord(
                            Text: "Checking runnability",
                            Processing: true,
                            Blocking: false,
                            Command: null);
                    }
                    return new StatusRecord(
                        Text: "Ready",
                        Processing: false,
                        Blocking: false,
                        Command: null);
                })
                .ToGuiProperty(this, nameof(StatusDisplay),
                    new StatusRecord(
                        Text: "Initializing",
                        Processing: false,
                        Blocking: false,
                        Command: null));

            SetToLastSuccessfulRunCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.LastSuccessfulRun)
                    .Select(x =>
                    {
                        return x != null
                            && !x.TargetRepo.IsNullOrWhitespace()
                            && !x.ProjectSubpath.IsNullOrWhitespace()
                            && !x.Commit.IsNullOrWhitespace()
                            && !x.MutagenVersion.IsNullOrWhitespace()
                            && !x.SynthesisVersion.IsNullOrWhitespace();
                    }),
                execute: () =>
                {
                    if (LastSuccessfulRun == null) return;
                    RemoteRepoPathInput.RemoteRepoPath = LastSuccessfulRun.TargetRepo;
                    this.SelectedProjectInput.ProjectSubpath = LastSuccessfulRun.ProjectSubpath;
                    this.PatcherTargeting.TargetCommit = LastSuccessfulRun.Commit;
                    this.NugetTargeting.ManualMutagenVersion = LastSuccessfulRun.MutagenVersion;
                    this.NugetTargeting.ManualSynthesisVersion = LastSuccessfulRun.SynthesisVersion;
                    this.PatcherTargeting.PatcherVersioning = PatcherVersioningEnum.Commit;
                    this.NugetTargeting.SynthesisVersioning = PatcherNugetVersioningEnum.Manual;
                    this.NugetTargeting.MutagenVersioning = PatcherNugetVersioningEnum.Manual;
                });
        }

        public override PatcherSettings Save()
        {
            var ret = new GithubPatcherSettings
            {
                RemoteRepoPath = RemoteRepoPathInput.RemoteRepoPath,
                ID = this.ID,
                SelectedProjectSubpath = this.SelectedProjectInput.ProjectSubpath,
                PatcherVersioning = this.PatcherTargeting.PatcherVersioning,
                MutagenVersionType = this.NugetTargeting.MutagenVersioning,
                ManualMutagenVersion = this.NugetTargeting.ManualMutagenVersion,
                SynthesisVersionType = this.NugetTargeting.SynthesisVersioning,
                ManualSynthesisVersion = this.NugetTargeting.ManualSynthesisVersion,
                TargetTag = this.PatcherTargeting.TargetTag,
                TargetCommit = this.PatcherTargeting.TargetCommit,
                LatestTag = this.PatcherTargeting.TagAutoUpdate,
                FollowDefaultBranch = this.PatcherTargeting.BranchFollowMain,
                AutoUpdateToBranchTip = this.PatcherTargeting.BranchAutoUpdate,
                TargetBranch = this.PatcherTargeting.TargetBranchName,
                LastSuccessfulRun = this.LastSuccessfulRun,
            };
            CopyOverSave(ret);
            PatcherSettings.Persist();
            return ret;
        }

        private void CopyInSettings(GithubPatcherSettings? settings)
        {
            if (settings == null) return;
            RemoteRepoPathInput.RemoteRepoPath = settings.RemoteRepoPath;
            this.ID = settings.ID;
            this.SelectedProjectInput.ProjectSubpath = settings.SelectedProjectSubpath;
            this.PatcherTargeting.PatcherVersioning = settings.PatcherVersioning;
            this.NugetTargeting.MutagenVersioning = settings.MutagenVersionType;
            this.NugetTargeting.SynthesisVersioning = settings.SynthesisVersionType;
            this.NugetTargeting.ManualMutagenVersion = settings.ManualMutagenVersion;
            this.NugetTargeting.ManualSynthesisVersion = settings.ManualSynthesisVersion;
            this.PatcherTargeting.TargetTag = settings.TargetTag;
            this.PatcherTargeting.TargetCommit = settings.TargetCommit;
            this.PatcherTargeting.BranchAutoUpdate = settings.AutoUpdateToBranchTip;
            this.PatcherTargeting.BranchFollowMain = settings.FollowDefaultBranch;
            this.PatcherTargeting.TagAutoUpdate = settings.LatestTag;
            this.PatcherTargeting.TargetBranchName = settings.TargetBranch;
            this.LastSuccessfulRun = settings.LastSuccessfulRun;
        }

        public override PatcherRunVm ToRunner(PatchersRunVm parent)
        {
            PatcherSettings.Persist();
            return _toSolutionRunner.GetRunner(parent, this);
        }

        public override void Delete()
        {
            base.Delete();
            try
            {
                var dir = new DirectoryInfo(this.LocalDriverRepoDirectory);
                dir.DeleteEntireFolder();
            }
            catch (Exception ex)
            {
                _Logger.Error(ex, $"Failure deleting git repo: {this.LocalDriverRepoDirectory}");
            }
            try
            {
                var dir = new DirectoryInfo(this.LocalRunnerRepoDirectory);
                dir.DeleteEntireFolder();
            }
            catch (Exception ex)
            {
                _Logger.Error(ex, $"Failure deleting git repo: {this.LocalRunnerRepoDirectory}");
            }
        }

        public override void SuccessfulRunCompleted()
        {
            if (NugetDiff.MutagenVersionDiff.SelectedVersion == null) return;
            if (NugetDiff.SynthesisVersionDiff.SelectedVersion == null) return;
            LastSuccessfulRun = new GithubPatcherLastRunState(
                TargetRepo: RemoteRepoPathInput.RemoteRepoPath,
                ProjectSubpath: this.SelectedProjectInput.ProjectSubpath,
                Commit: this.PatcherTargeting.TargetCommit,
                MutagenVersion: NugetDiff.MutagenVersionDiff.SelectedVersion,
                SynthesisVersion: NugetDiff.SynthesisVersionDiff.SelectedVersion);
        }

        FilePath IPathToProjProvider.Path => RunnableData?.ProjPath ?? throw new ArgumentNullException($"{nameof(IPathToProjProvider)}.{nameof(IPathToProjProvider.Path)}");
        FilePath IPathToSolutionFileProvider.Path => RunnableData?.SolutionPath ?? throw new ArgumentNullException($"{nameof(IPathToProjProvider)}.{nameof(IPathToProjProvider.Path)}");
    }
}
