using System.IO;
using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using DynamicData.Binding;
using Microsoft.Win32;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.FileAssociations;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Services.Patchers.Git;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;

public class GitPatcherVm : PatcherVm, IPathToSolutionFileProvider
{
    private readonly ILogger _logger;
    private readonly ICopyOverExtraData _copyOverExtraData;
    public override bool IsNameEditable => false;

    public ISelectedProjectInputVm SelectedProjectInput { get; }
    public IGitRemoteRepoPathInputVm RemoteRepoPathInput { get; }

    private readonly ObservableAsPropertyHelper<ConfigurationState> _state;
    public override ConfigurationState State => _state?.Value ?? ConfigurationState.Success;

    public string ID { get; private set; }

    public string LocalDriverRepoDirectory { get; }
    public string LocalRunnerRepoDirectory { get; }

    private readonly ObservableAsPropertyHelper<ErrorResponse> _repoValidity;
    public ErrorResponse RepoValidity => _repoValidity.Value;

    public IObservableCollection<string> AvailableProjects { get; }

    public IObservableCollection<string> AvailableTags { get; }

    private readonly ObservableAsPropertyHelper<RunnerRepoInfo?> _runnableData;
    public RunnerRepoInfo? RunnableData => _runnableData.Value;

    public ICommand OpenGitPageCommand { get; }

    public ICommand OpenGitPageToVersionCommand { get; }

    public ICommand NavigateToInternalFilesCommand { get; }

    public ICommand ExportSynthFileCommand { get; }

    private readonly ObservableAsPropertyHelper<bool> _attemptedCheckout;
    public bool AttemptedCheckout => _attemptedCheckout.Value;

    public PatcherUserSettingsVm PatcherSettings { get; }

    private readonly ObservableAsPropertyHelper<StatusRecord> _statusDisplay;
    public StatusRecord StatusDisplay => _statusDisplay.Value;

    [Reactive]
    public GithubPatcherLastRunState? LastSuccessfulRun { get; set; }

    public ICommand SetToLastSuccessfulRunCommand { get; }
        
    public ILockToCurrentVersioning Locking { get; }
    public IRepoClonesValidStateVm RepoClonesValid { get; }
    public INugetDiffProviderVm NugetDiff { get; }
    public IGitPatcherTargetingVm PatcherTargeting { get; }
    public IGitNugetTargetingVm NugetTargeting { get; }
    public IUpdateAllCommand UpdateAllCommand { get; }
    public ICommand DeleteUserDataCommand { get; }

    public GitPatcherVm(
        IGithubPatcherIdentifier ident,
        IPatcherNameVm nameVm,
        IProfileDisplayControllerVm selPatcher,
        IConfirmationPanelControllerVm confirmation,
        ISelectedProjectInputVm selectedProjectInput,
        IGitRemoteRepoPathInputVm remoteRepoPathInputVm,
        INavigateTo navigate, 
        IAvailableTags availableTags,
        ILockToCurrentVersioning lockToCurrentVersioning,
        IAvailableProjects availableProjects,
        ICompilationProvider compilationProvider,
        IBaseRepoDirectoryProvider baseRepoDir,
        IGitStatusDisplay gitStatusDisplay,
        IDriverRepoDirectoryProvider driverRepoDirectoryProvider,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
        IGetRepoPathValidity getRepoPathValidity,
        IRepoClonesValidStateVm repoClonesValid,
        INugetDiffProviderVm nugetDiff,
        IGitPatcherState state,
        ILogger logger,
        IRunnableStateProvider runnableStateProvider,
        ILifetimeScope scope,
        IGitPatcherTargetingVm patcherTargeting,
        ICheckoutInputProvider checkoutInputProvider,
        IGitNugetTargetingVm nugetTargetingVm,
        IUpdateAllCommand updateAllCommand,
        IAttemptedCheckout attemptedCheckout,
        IPatcherIdProvider idProvider,
        ICopyOverExtraData copyOverExtraData,
        ExportGitAddFile exportGitAddFile,
        PatcherRenameActionVm.Factory renameFactory,
        DeleteUserData deleteUserData,
        PatcherUserSettingsVm.Factory settingsVmFactory,
        PatcherGroupTarget groupTarget,
        GithubPatcherSettings? settings = null)
        : base(
            scope, nameVm, selPatcher,
            confirmation, idProvider, renameFactory, groupTarget, settings)
    {
        _logger = logger;
        _copyOverExtraData = copyOverExtraData;
        SelectedProjectInput = selectedProjectInput;
        RemoteRepoPathInput = remoteRepoPathInputVm;
        Locking = lockToCurrentVersioning;
        RepoClonesValid = repoClonesValid;
        NugetDiff = nugetDiff;
        PatcherTargeting = patcherTargeting;
        NugetTargeting = nugetTargetingVm;
        UpdateAllCommand = updateAllCommand;

        DeleteUserDataCommand = ReactiveCommand.Create(deleteUserData.Delete);

        ID = ident.Id;
            
        CopyInSettings(settings);

        LocalDriverRepoDirectory = driverRepoDirectoryProvider.Path.Path;
        LocalRunnerRepoDirectory = runnerRepoDirectoryProvider.Path.Path;

        _repoValidity = getRepoPathValidity.RepoPath
            .Select(r => r.RunnableState)
            .ToGuiProperty(this, nameof(RepoValidity), deferSubscription: true);

        AvailableProjects = availableProjects.Projects;

        AvailableTags = availableTags.Tags;

        _attemptedCheckout = checkoutInputProvider.Input
            .Select(attemptedCheckout.Attempted)
            .ToGuiProperty(this, nameof(AttemptedCheckout), deferSubscription: true);

        _runnableData = runnableStateProvider.WhenAnyValue(x => x.State.Item)
            .ToGuiProperty(this, nameof(RunnableData), default(RunnerRepoInfo?), deferSubscription: true);

        _state = state.State
            .ToGuiProperty(this, nameof(State), new ConfigurationState(ErrorResponse.Fail("Evaluating"))
            {
                IsHaltingError = false
            }, deferSubscription: true);

        OpenGitPageCommand = ReactiveCommand.Create(
            canExecute: this.WhenAnyValue(x => x.RepoValidity)
                .Select(x => x.Succeeded),
            execute: () => navigate.Navigate(RemoteRepoPathInput.RemoteRepoPath));

        OpenGitPageToVersionCommand = ReactiveCommand.Create(
            canExecute: this.WhenAnyFallback(x => x.RunnableData)
                .Select(x => x != null),
            execute: () =>
            {
                try
                {
                    if (RunnableData is not {} runnable) return;
                    navigate.Navigate(Path.Combine(RemoteRepoPathInput.RemoteRepoPath, "tree", runnable.Target.Target));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error opening Git webpage");
                }
            });

        NavigateToInternalFilesCommand = ReactiveCommand.Create(() => navigate.Navigate(baseRepoDir.Path));

        PatcherSettings = settingsVmFactory(
                false, 
                compilationProvider.State.Select(c =>
                    {
                        if (c.RunnableState.Failed)
                        {
                            return new PatcherUserSettingsVm.Inputs(c.RunnableState.BubbleFailure<TargetProject>(), null, default);
                        }
                        return new PatcherUserSettingsVm.Inputs(GetResponse<TargetProject>.Succeed(c.Item.Project), c.Item.TargetVersions.Synthesis, c.Item.MetaPath);
                    })
                    .DistinctUntilChanged())
            .DisposeWith(this);

        _statusDisplay = gitStatusDisplay.StatusDisplay
            .ToGuiProperty(this, nameof(StatusDisplay),
                new StatusRecord(
                    Text: "Initializing",
                    Processing: false,
                    Blocking: false,
                    Command: null), deferSubscription: true);

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

        ExportSynthFileCommand = ReactiveCommand.Create(() =>
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Synth file|*.synth";
            dialog.Title = "Save a synth file";
            dialog.FileName = $"{NameVm.Name}.synth";
            dialog.ShowDialog();

            if (dialog.FileName == "") return;
                
            exportGitAddFile.ExportAsFile(
                dialog.FileName,
                remoteRepoPathInputVm.RemoteRepoPath,
                selectedProjectInput.ProjectSubpath);
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
        try
        {
            _copyOverExtraData.Copy();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to copy in extra data");
        }
        try
        {
            PatcherSettings.Persist();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save patcher settings");
        }
        return ret;
    }

    private void CopyInSettings(GithubPatcherSettings? settings)
    {
        if (settings == null) return;
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

    public override void PrepForRun()
    {
        base.PrepForRun();
        _copyOverExtraData.Copy();
        try
        {
            PatcherSettings.Persist();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save patcher settings");
        }
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
            _logger.Error(ex, "Failure deleting git repo: {LocalDriverRepoDirectory}", LocalDriverRepoDirectory);
        }
        try
        {
            var dir = new DirectoryInfo(this.LocalRunnerRepoDirectory);
            dir.DeleteEntireFolder();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failure deleting git repo: {LocalRunnerRepoDirectory}", this.LocalRunnerRepoDirectory);
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

    FilePath IPathToSolutionFileProvider.Path => RunnableData?.Project.SolutionPath ?? throw new ArgumentNullException($"{nameof(IPathToSolutionFileProvider)}.{nameof(IPathToSolutionFileProvider.Path)}");
}