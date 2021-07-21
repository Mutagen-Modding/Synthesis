using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Newtonsoft.Json;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;
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

        private readonly ObservableAsPropertyHelper<bool> _RepoClonesValid;
        public bool RepoClonesValid => _RepoClonesValid.Value;

        public IObservableCollection<string> AvailableProjects { get; }

        [Reactive]
        public PatcherVersioningEnum PatcherVersioning { get; set; } = PatcherVersioningEnum.Branch;

        public IObservableCollection<string> AvailableTags { get; }

        [Reactive]
        public string TargetTag { get; set; } = string.Empty;

        [Reactive]
        public bool TagAutoUpdate { get; set; } = false;

        [Reactive]
        public string TargetCommit { get; set; } = string.Empty;

        [Reactive]
        public bool BranchAutoUpdate { get; set; } = false;

        [Reactive]
        public bool BranchFollowMain { get; set; } = true;

        [Reactive]
        public string TargetBranchName { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<string> _TargetOriginBranchName;
        public string TargetOriginBranchName => _TargetOriginBranchName.Value;

        private readonly ObservableAsPropertyHelper<RunnerRepoInfo?> _RunnableData;
        public RunnerRepoInfo? RunnableData => _RunnableData.Value;

        public ICommand OpenGitPageCommand { get; }

        public ICommand OpenGitPageToVersionCommand { get; }

        public ICommand NavigateToInternalFilesCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateAllCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateToBranchCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateToTagCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateMutagenManualToLatestCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateSynthesisManualToLatestCommand { get; }

        [Reactive]
        public PatcherNugetVersioningEnum MutagenVersioning { get; set; } = PatcherNugetVersioningEnum.Profile;

        [Reactive]
        public string ManualMutagenVersion { get; set; } = string.Empty;

        [Reactive]
        public PatcherNugetVersioningEnum SynthesisVersioning { get; set; } = PatcherNugetVersioningEnum.Profile;

        [Reactive]
        public string ManualSynthesisVersion { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<(string? MatchVersion, string? SelectedVersion)> _MutagenVersionDiff;
        public (string? MatchVersion, string? SelectedVersion) MutagenVersionDiff => _MutagenVersionDiff.Value;

        private readonly ObservableAsPropertyHelper<(string? MatchVersion, string? SelectedVersion)> _SynthesisVersionDiff;
        public (string? MatchVersion, string? SelectedVersion) SynthesisVersionDiff => _SynthesisVersionDiff.Value;

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

        public GitPatcherVm(
            IGithubPatcherIdentifier ident,
            IProfileIdentifier profileIdent,
            IPatcherNameVm nameVm,
            ISelectedProjectInputVm selectedProjectInput,
            IGitRemoteRepoPathInputVm remoteRepoPathInputVm,
            IProfileLoadOrder loadOrder,
            IProfileVersioning versioning,
            IProfileDataFolder dataFolder,
            IRemovePatcherFromProfile remove,
            INavigateTo navigate, 
            ICheckOrCloneRepo checkOrClone,
            ICheckRunnability checkRunnability,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            ILockToCurrentVersioning lockToCurrentVersioning,
            IInstalledSdkProvider dotNetInstalled,
            IEnvironmentErrorsVm envErrors,
            INewestLibraryVersions newest,
            IPerformGitPatcherCompilation performGitPatcherCompilation,
            IPrepareDriverRepository prepareDriverRepository,
            IBaseRepoDirectoryProvider baseRepoDir,
            IDriverRepoDirectoryProvider driverRepoDirectoryProvider,
            IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
            IGetRepoPathValidity getRepoPathValidity,
            ILogger logger,
            IPrepareRunnableState prepareRunnableState,
            IToSolutionRunner toSolutionRunner,
            PatcherSettingsVm.Factory settingsVmFactory,
            GithubPatcherSettings? settings = null)
            : base(nameVm, remove, selPatcher, confirmation, settings)
        {
            _Logger = logger;
            SelectedProjectInput = selectedProjectInput;
            RemoteRepoPathInput = remoteRepoPathInputVm;
            _toSolutionRunner = toSolutionRunner;
            Locking = lockToCurrentVersioning;

            ID = ident.Id;
            
            CopyInSettings(settings);

            LocalDriverRepoDirectory = driverRepoDirectoryProvider.Path.Path;
            LocalRunnerRepoDirectory = runnerRepoDirectoryProvider.Path.Path;

            _RepoValidity = getRepoPathValidity.RepoPath
                .Select(r => r.RunnableState)
                .ToGuiProperty(this, nameof(RepoValidity));

            var driverRepoInfo = prepareDriverRepository.DriverInfo;

            // Clone a second repository that we will check out the desired target commit to actually run
            var runnerRepoState = getRepoPathValidity.RepoPath
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectReplaceWithIntermediate(
                    new ConfigurationState(ErrorResponse.Fail("Cloning runner repository"))
                    {
                        IsHaltingError = false
                    },
                    async (path, cancel) =>
                    {
                        if (path.RunnableState.Failed) return path.ToUnit();
                        using var timing = Logger.Time($"runner repo: {path.Item}");
                        return (ErrorResponse)checkOrClone.Check(path.ToGetResponse(), LocalRunnerRepoDirectory, x => Logger.Information(x), cancel);
                    })
                .Replay(1)
                .RefCount();

            _RepoClonesValid = Observable.CombineLatest(
                    driverRepoInfo,
                    runnerRepoState,
                    (driver, runner) => driver.RunnableState.Succeeded && runner.RunnableState.Succeeded)
                .ToGuiProperty(this, nameof(RepoClonesValid));

            AvailableProjects = driverRepoInfo
                .Select(x => x.Item?.AvailableProjects ?? Enumerable.Empty<string>())
                .Select(x => x.AsObservableChangeSet<string>())
                .Switch()
                .ToObservableCollection(this);

            var tagInput = Observable.CombineLatest(
                SelectedProjectInput.Picker.WhenAnyValue(x => x.TargetPath),
                this.WhenAnyValue(x => x.AvailableProjects.Count),
                (targetPath, count) => (targetPath, count));
            AvailableTags = driverRepoInfo
                .Select(x => x.Item?.Tags ?? Enumerable.Empty<(int Index, string Name, string Sha)>())
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .Filter(
                    tagInput.Select(x =>
                    {
                        if (x.count == 0) return new Func<(int Index, string Name, string Sha), bool>(_ => false);
                        if (x.count == 1) return new Func<(int Index, string Name, string Sha), bool>(_ => true);
                        if (!x.targetPath.EndsWith(".csproj")) return new Func<(int Index, string Name, string Sha), bool>(_ => false);
                        var projName = Path.GetFileName(x.targetPath);
                        return new Func<(int Index, string Name, string Sha), bool>(i => i.Name.StartsWith(projName, StringComparison.OrdinalIgnoreCase));
                    }))
                .Sort(SortExpressionComparer<(int Index, string Name, string Sha)>.Descending(x => x.Index))
                .Transform(x => x.Name)
                .ToObservableCollection(this);

            _TargetOriginBranchName = this.WhenAnyValue(x => x.TargetBranchName)
                .Select(x => $"origin/{x}")
                .ToGuiProperty(this, nameof(TargetOriginBranchName), string.Empty);

            // Set latest checkboxes to drive user input
            driverRepoInfo
                .FilterSwitch(this.WhenAnyValue(x => x.BranchFollowMain))
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(state =>
                {
                    if (state.RunnableState.Succeeded)
                    {
                        this.TargetBranchName = state.Item.MasterBranchName;
                    }
                })
                .DisposeWith(this);
            Observable.CombineLatest(
                    driverRepoInfo,
                    this.WhenAnyValue(x => x.TargetOriginBranchName),
                    (Driver, TargetBranch) => (Driver, TargetBranch))
                .FilterSwitch(
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.BranchAutoUpdate),
                        this.WhenAnyValue(x => x.PatcherVersioning),
                        (autoBranch, versioning) => autoBranch && versioning == PatcherVersioningEnum.Branch))
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (x.Driver.RunnableState.Succeeded
                        && x.Driver.Item.BranchShas.TryGetValue(x.TargetBranch, out var sha))
                    {
                        this.TargetCommit = sha;
                    }
                })
                .DisposeWith(this);
            driverRepoInfo.Select(x => x.RunnableState.Failed ? default : x.Item.Tags.OrderByDescending(x => x.Index).FirstOrDefault())
                .FilterSwitch(
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.TagAutoUpdate),
                        this.WhenAnyValue(x => x.Locking.Lock),
                        this.WhenAnyValue(x => x.PatcherVersioning),
                        (autoTag, locked, versioning) => !locked && autoTag && versioning == PatcherVersioningEnum.Tag))
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    this.TargetTag = x.Name;
                    this.TargetCommit = x.Sha;
                })
                .DisposeWith(this);

            var targetBranchSha = Observable.CombineLatest(
                    driverRepoInfo.Select(x => x.RunnableState.Failed ? default : x.Item.BranchShas),
                    this.WhenAnyValue(x => x.TargetOriginBranchName),
                    (dict, branch) => dict?.GetOrDefault(branch))
                .Replay(1)
                .RefCount();
            var targetTag = Observable.CombineLatest(
                    driverRepoInfo.Select(x => x.RunnableState.Failed ? default : x.Item.Tags),
                    this.WhenAnyValue(x => x.TargetTag),
                    (tags, tag) => tags?
                        .Where(tagItem => tagItem.Name == tag)
                        .FirstOrDefault())
                .Replay(1)
                .RefCount();

            // Set up empty target autofill
            // Usually for initial bootstrapping
            Observable.CombineLatest(
                    targetBranchSha,
                    this.WhenAnyValue(x => x.TargetCommit),
                    (targetBranchSha, targetCommit) => (targetBranchSha, targetCommit))
                .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                .Where(x => x.targetBranchSha != null && TargetCommit.IsNullOrWhitespace())
                .Subscribe(x =>
                {
                    this.TargetCommit = x.targetBranchSha ?? string.Empty;
                })
                .DisposeWith(this);
            Observable.CombineLatest(
                    targetTag,
                    this.WhenAnyValue(x => x.TargetCommit),
                    (targetTagSha, targetCommit) => (targetTagSha, targetCommit))
                .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                .Where(x => x.targetTagSha != null && TargetCommit.IsNullOrWhitespace())
                .Subscribe(x =>
                {
                    this.TargetCommit = x.targetTagSha?.Sha ?? string.Empty;
                })
                .DisposeWith(this);

            // Set up update available systems
            UpdateToBranchCommand = NoggogCommand.CreateFromObject(
                objectSource: Observable.CombineLatest(
                    targetBranchSha,
                    this.WhenAnyValue(x => x.TargetCommit),
                    (branch, target) => (BranchSha: branch, Current: target)),
                canExecute: o => o.BranchSha != null && o.BranchSha != o.Current,
                extraCanExecute: this.WhenAnyValue(x => x.PatcherVersioning)
                    .Select(vers => vers == PatcherVersioningEnum.Branch),
                execute: o =>
                {
                    this.TargetCommit = o.BranchSha!;
                },
                this.CompositeDisposable);
            UpdateToTagCommand = NoggogCommand.CreateFromObject(
                objectSource: Observable.CombineLatest(
                    targetTag,
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.TargetCommit),
                        this.WhenAnyValue(x => x.TargetTag),
                        (TargetSha, TargetTag) => (TargetSha, TargetTag)),
                    (tag, target) => (TagSha: tag?.Sha, Tag: tag?.Name, Current: target)),
                canExecute: o => (o.TagSha != null && o.Tag != null)
                    && (o.TagSha != o.Current.TargetSha || o.Tag != o.Current.TargetTag),
                extraCanExecute: this.WhenAnyValue(x => x.PatcherVersioning)
                    .Select(vers => vers == PatcherVersioningEnum.Tag),
                execute: o =>
                {
                    this.TargetTag = o.Tag!;
                    this.TargetCommit = o.TagSha!;
                },
                this.CompositeDisposable);

            // Get the selected versioning preferences
            var patcherVersioning = Observable.CombineLatest(
                this.WhenAnyValue(x => x.PatcherVersioning),
                this.WhenAnyValue(x => x.TargetTag),
                this.WhenAnyValue(x => x.TargetCommit),
                this.WhenAnyValue(x => x.TargetOriginBranchName),
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.TagAutoUpdate),
                        this.WhenAnyValue(x => x.Locking.Lock),
                        (auto, locked) => !locked && auto),
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.BranchAutoUpdate),
                        this.WhenAnyValue(x => x.Locking.Lock),
                        (auto, locked) => !locked && auto),
                (versioning, tag, commit, branch, tagAuto, branchAuto) =>
                {
                    return GitPatcherVersioning.Factory(
                        versioning: versioning,
                        tag: tag,
                        commit: commit,
                        branch: branch,
                        autoTag: tagAuto,
                        autoBranch: branchAuto);
                })
                .DistinctUntilChanged()
                .Replay(1)
                .RefCount();

            var nugetTarget = Observable.CombineLatest(
                    versioning.WhenAnyValue(x => x.ActiveVersioning)
                        .Switch(),
                    this.WhenAnyValue(x => x.MutagenVersioning),
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    newest.NewestMutagenVersion,
                    this.WhenAnyValue(x => x.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    newest.NewestSynthesisVersion,
                    (profile, mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth) =>
                    {
                        var sb = new StringBuilder("Switching nuget targets");
                        NugetVersioning mutagen, synthesis;
                        if (mutaVersioning == PatcherNugetVersioningEnum.Profile)
                        {
                            sb.Append($"  Mutagen following profile: {profile.Mutagen}");
                            mutagen = profile.Mutagen;
                        }
                        else
                        {
                            mutagen = new NugetVersioning("Mutagen", mutaVersioning.ToNugetVersioningEnum(), mutaManual, newestMuta);
                            sb.Append($"  {mutagen}");
                        }
                        if (synthVersioning == PatcherNugetVersioningEnum.Profile)
                        {
                            sb.Append($"  Synthesis following profile: {profile.Synthesis}");
                            synthesis = profile.Synthesis;
                        }
                        else
                        {
                            synthesis = new NugetVersioning("Synthesis", synthVersioning.ToNugetVersioningEnum(), synthManual, newestSynth);
                            sb.Append($"  {synthesis}");
                        }
                        Logger.Information(sb.ToString());
                        return new SynthesisNugetVersioning(
                            mutagen: mutagen,
                            synthesis: synthesis);
                    })
                .Select(nuget => nuget.TryGetTarget())
                .Replay(1)
                .RefCount();

            // Checkout desired patcher commit on the runner repository
            var checkoutInput = Observable.CombineLatest(
                    runnerRepoState,
                    SelectedProjectInput.Picker.PathState()
                        .Select(x => x.Succeeded ? x : GetResponse<string>.Fail("No patcher project selected.")),
                    patcherVersioning,
                    nugetTarget,
                    (runnerState, proj, patcherVersioning, libraryNugets) =>
                    (runnerState, proj, patcherVersioning, libraryNugets))
                .Replay(1)
                .RefCount();
            var runnableState = checkoutInput
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select((item) => prepareRunnableState.Prepare(item.runnerState, item.proj, item.patcherVersioning, item.libraryNugets))
                .Switch()
                .StartWith(new ConfigurationState<RunnerRepoInfo>(GetResponse<RunnerRepoInfo>.Fail("Constructing runnable state"))
                {
                    IsHaltingError = false
                })
                .Replay(1)
                .RefCount();

            _AttemptedCheckout = checkoutInput
                .Select(input =>
                {
                    return input.runnerState.RunnableState.Succeeded
                        && input.proj.Succeeded
                        && input.libraryNugets.Succeeded;
                })
                .ToGuiProperty(this, nameof(AttemptedCheckout));

            _RunnableData = runnableState
                .Select(x => x.Item ?? default(RunnerRepoInfo?))
                .ToGuiProperty(this, nameof(RunnableData), default(RunnerRepoInfo?));

            _MutagenVersionDiff = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.RunnableData)
                        .Select(x => x?.ListedMutagenVersion),
                    nugetTarget.Select(x => x.Value?.MutagenVersion),
                    (matchVersion, selVersion) => (matchVersion, selVersion))
                .ToGuiProperty(this, nameof(MutagenVersionDiff));

            _SynthesisVersionDiff = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.RunnableData)
                        .Select(x => x?.ListedSynthesisVersion),
                    nugetTarget.Select(x => x.Value?.SynthesisVersion),
                    (matchVersion, selVersion) => (matchVersion, selVersion))
                .ToGuiProperty(this, nameof(SynthesisVersionDiff));

            var patcherConfiguration = runnableState
                .Select(x =>
                {
                    if (x.RunnableState.Failed) return Observable.Return(default(PatcherCustomization?));
                    var confPath = Path.Combine(Path.GetDirectoryName(x.Item.ProjPath)!, Constants.MetaFileName);
                    return Noggog.ObservableExt.WatchFile(confPath)
                        .StartWith(Unit.Default)
                        .Select(x =>
                        {
                            try
                            {
                                if (!File.Exists(confPath)) return default;
                                return JsonConvert.DeserializeObject<PatcherCustomization>(
                                    File.ReadAllText(confPath),
                                    Execution.Constants.JsonSettings);
                            }
                            catch (Exception)
                            {
                                return default(PatcherCustomization?);
                            }
                        });
                })
                .Switch()
                .Replay(1)
                .RefCount();

            var missingReqMods = patcherConfiguration
                .Select(conf =>
                {
                    if (conf == null) return Enumerable.Empty<ModKey>();
                    return conf.RequiredMods
                        .SelectWhere(x => TryGet<ModKey>.Create(ModKey.TryFromNameAndExtension(x, out var modKey), modKey));
                })
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .Except(loadOrder.LoadOrder.Connect()
                    .Transform(x => x.ModKey))
                .RefCount();

            var compilation = performGitPatcherCompilation.Process(runnableState)
                .Replay(1)
                .RefCount();

            var runnability = Observable.CombineLatest(
                    compilation,
                    dataFolder.WhenAnyValue(x => x.Path),
                    loadOrder.LoadOrder.Connect()
                        .QueryWhenChanged()
                        .StartWith(ListExt.Empty<ReadOnlyModListingVM>()),
                    (comp, data, loadOrder) => (comp, data, loadOrder))
                .Select(i =>
                {
                    return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
                    {
                        if (i.comp.RunnableState.Failed)
                        {
                            observer.OnNext(i.comp);
                            return;
                        }

                        Logger.Information("Checking runnability");
                        // Return early with the values, but mark not complete
                        observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item)
                        {
                            IsHaltingError = false,
                            RunnableState = ErrorResponse.Fail("Checking runnability")
                        });

                        try
                        {
                            var runnability = await checkRunnability.Check(
                                path: i.comp.Item.ProjPath,
                                directExe: false,
                                release: profileIdent.Release,
                                dataFolder: i.data,
                                cancel: cancel,
                                loadOrder: i.loadOrder.Select<ReadOnlyModListingVM, IModListingGetter>(lvm => lvm),
                                log: (s) => Logger.Information(s));
                            if (runnability.Failed)
                            {
                                Logger.Information($"Checking runnability failed: {runnability.Reason}");
                                observer.OnNext(runnability.BubbleFailure<RunnerRepoInfo>());
                                return;
                            }

                            // Return things again, without error
                            Logger.Information("Checking runnability succeeded");
                            observer.OnNext(i.comp);
                        }
                        catch (Exception ex)
                        {
                            var str = $"Error checking runnability on runner repository: {ex}";
                            Logger.Error(str);
                            observer.OnNext(ErrorResponse.Fail(str).BubbleFailure<RunnerRepoInfo>());
                        }
                        observer.OnCompleted();
                    });
                })
                .Switch()
                .Replay(1)
                .RefCount();

            _State = Observable.CombineLatest(
                    driverRepoInfo
                        .Select(x => x.ToUnit()),
                    runnerRepoState,
                    runnableState
                        .Select(x => x.ToUnit()),
                    runnability,
                    dotNetInstalled.DotNetSdkInstalled
                        .Select(x => (x, true))
                        .StartWith((new DotNetVersion(string.Empty, false), false)),
                    envErrors.WhenAnyFallback(x => x.ActiveError!.ErrorString),
                    missingReqMods
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

            UpdateMutagenManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: newest.NewestMutagenVersion,
                canExecute: v =>
                {
                    return Observable.CombineLatest(
                            this.WhenAnyValue(x => x.ManualMutagenVersion),
                            v,
                            (manual, latest) => latest != null && latest != manual);
                },
                execute: v => ManualMutagenVersion = v ?? string.Empty,
                extraCanExecute: this.WhenAnyValue(x => x.MutagenVersioning)
                    .Select(vers => vers == PatcherNugetVersioningEnum.Manual),
                disposable: this.CompositeDisposable);
            UpdateSynthesisManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: newest.NewestSynthesisVersion,
                canExecute: v =>
                {
                    return Observable.CombineLatest(
                            this.WhenAnyValue(x => x.ManualSynthesisVersion),
                            v,
                            (manual, latest) => latest != null && latest != manual);
                },
                execute: v => ManualSynthesisVersion = v ?? string.Empty,
                extraCanExecute: this.WhenAnyValue(x => x.SynthesisVersioning)
                    .Select(vers => vers == PatcherNugetVersioningEnum.Manual),
                disposable: this.CompositeDisposable);

            UpdateAllCommand = CommandExt.CreateCombinedAny(
                UpdateMutagenManualToLatestCommand,
                UpdateSynthesisManualToLatestCommand,
                UpdateToBranchCommand,
                UpdateToTagCommand);

            PatcherSettings = settingsVmFactory(
                Logger, false, 
                compilation.Select(c =>
                    {
                        if (c.RunnableState.Failed) return (c.RunnableState.BubbleFailure<FilePath>(), null);
                        return (GetResponse<FilePath>.Succeed(c.Item.ProjPath), c.Item.TargetSynthesisVersion);
                    })
                    .DistinctUntilChanged(x => (x.Item1.Value, x.TargetSynthesisVersion)))
                .DisposeWith(this);

            _StatusDisplay = Observable.CombineLatest(
                driverRepoInfo,
                runnableState,
                compilation,
                runnability,
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
                    this.TargetCommit = LastSuccessfulRun.Commit;
                    this.ManualMutagenVersion = LastSuccessfulRun.MutagenVersion;
                    this.ManualSynthesisVersion = LastSuccessfulRun.SynthesisVersion;
                    this.PatcherVersioning = PatcherVersioningEnum.Commit;
                    this.SynthesisVersioning = PatcherNugetVersioningEnum.Manual;
                    this.MutagenVersioning = PatcherNugetVersioningEnum.Manual;
                });
        }

        public override PatcherSettings Save()
        {
            var ret = new GithubPatcherSettings
            {
                RemoteRepoPath = RemoteRepoPathInput.RemoteRepoPath,
                ID = this.ID,
                SelectedProjectSubpath = this.SelectedProjectInput.ProjectSubpath,
                PatcherVersioning = this.PatcherVersioning,
                MutagenVersionType = this.MutagenVersioning,
                ManualMutagenVersion = this.ManualMutagenVersion,
                SynthesisVersionType = this.SynthesisVersioning,
                ManualSynthesisVersion = this.ManualSynthesisVersion,
                TargetTag = this.TargetTag,
                TargetCommit = this.TargetCommit,
                LatestTag = this.TagAutoUpdate,
                FollowDefaultBranch = this.BranchFollowMain,
                AutoUpdateToBranchTip = this.BranchAutoUpdate,
                TargetBranch = this.TargetBranchName,
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
            this.PatcherVersioning = settings.PatcherVersioning;
            this.MutagenVersioning = settings.MutagenVersionType;
            this.SynthesisVersioning = settings.SynthesisVersionType;
            this.ManualMutagenVersion = settings.ManualMutagenVersion;
            this.ManualSynthesisVersion = settings.ManualSynthesisVersion;
            this.TargetTag = settings.TargetTag;
            this.TargetCommit = settings.TargetCommit;
            this.BranchAutoUpdate = settings.AutoUpdateToBranchTip;
            this.BranchFollowMain = settings.FollowDefaultBranch;
            this.TagAutoUpdate = settings.LatestTag;
            this.TargetBranchName = settings.TargetBranch;
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
            if (MutagenVersionDiff.SelectedVersion == null) return;
            if (SynthesisVersionDiff.SelectedVersion == null) return;
            LastSuccessfulRun = new GithubPatcherLastRunState(
                TargetRepo: RemoteRepoPathInput.RemoteRepoPath,
                ProjectSubpath: this.SelectedProjectInput.ProjectSubpath,
                Commit: this.TargetCommit,
                MutagenVersion: MutagenVersionDiff.SelectedVersion,
                SynthesisVersion: SynthesisVersionDiff.SelectedVersion);
        }

        FilePath IPathToProjProvider.Path => RunnableData?.ProjPath ?? throw new ArgumentNullException($"{nameof(IPathToProjProvider)}.{nameof(IPathToProjProvider.Path)}");
        FilePath IPathToSolutionFileProvider.Path => RunnableData?.SolutionPath ?? throw new ArgumentNullException($"{nameof(IPathToProjProvider)}.{nameof(IPathToProjProvider.Path)}");
    }
}
