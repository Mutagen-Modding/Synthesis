using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using LibGit2Sharp;
using System.IO;
using System.Linq;
using DynamicData.Binding;
using DynamicData;
using static Synthesis.Bethesda.GUI.SolutionPatcherVM;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Input;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers;
using System.Reactive;
using System.Text;
using Synthesis.Bethesda.DTO;
using Newtonsoft.Json;
using Mutagen.Bethesda;
using Synthesis.Bethesda.Execution;
using System.Threading;
using Mutagen.Bethesda.Synthesis.WPF;

namespace Synthesis.Bethesda.GUI
{
    public class GitPatcherVM : PatcherVM
    {
        public override bool IsNameEditable => false;

        [Reactive]
        public string RemoteRepoPath { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

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
        public string ProjectSubpath { get; set; } = string.Empty;

        public PathPickerVM SelectedProjectPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

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

        public PatcherSettingsVM PatcherSettings { get; }

        public record StatusRecord(string Text, bool Processing, bool Blocking, ICommand? Command);
        private readonly ObservableAsPropertyHelper<StatusRecord> _StatusDisplay;
        public StatusRecord StatusDisplay => _StatusDisplay.Value;

        [Reactive]
        public GithubPatcherLastRunState? LastSuccessfulRun { get; set; }

        public ICommand SetToLastSuccessfulRunCommand { get; }

        public GitPatcherVM(ProfileVM parent, GithubPatcherSettings? settings = null)
            : base(parent, settings)
        {
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            CopyInSettings(settings);

            var localRepoDir = Path.Combine(Profile.ProfileDirectory, "Git", ID);
            LocalDriverRepoDirectory = Path.Combine(localRepoDir, "Driver");
            LocalRunnerRepoDirectory = GitPatcherRun.RunnerRepoDirectory(Profile.ID, ID);

            _DisplayName = this.WhenAnyValue(
                x => x.Nickname,
                x => x.RemoteRepoPath,
                GetNickname)
                .ToGuiProperty<string>(this, nameof(DisplayName), GetNickname(Nickname, RemoteRepoPath));

            // Check to see if remote path points to a reachable git repository
            var remoteRepoPath = GetRepoPathValidity(this.WhenAnyValue(x => x.RemoteRepoPath))
                .Replay(1)
                .RefCount();
            _RepoValidity = remoteRepoPath
                .Select(r => r.RunnableState)
                .ToGuiProperty(this, nameof(RepoValidity));

            // Clone repository to a folder where driving information will be retreived from master.
            // This will be where we get available projects + tags, etc.
            var driverRepoInfo = remoteRepoPath
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectReplaceWithIntermediate(
                    new ConfigurationState<DriverRepoInfo>(default!)
                    {
                        IsHaltingError = false,
                        RunnableState = ErrorResponse.Fail("Cloning driver repository"),
                    },
                    async (path, cancel) =>
                    {
                        if (!path.IsHaltingError && path.RunnableState.Failed) return path.BubbleError<DriverRepoInfo>();
                        using var timing = Logger.Time("Cloning driver repository");
                        // Clone and/or double check the clone is correct
                        var state = await GitUtility.CheckOrCloneRepo(path.ToGetResponse(), LocalDriverRepoDirectory, (x) => Logger.Information(x), cancel);
                        if (state.Failed)
                        {
                            Logger.Error($"Failed to check out driver repository: {state.Reason}");
                            return new ConfigurationState<DriverRepoInfo>(default!, (ErrorResponse)state);
                        }
                        cancel.ThrowIfCancellationRequested();

                        // Grab all the interesting metadata
                        List<(int Index, string Name, string Sha)> tags;
                        Dictionary<string, string> branchShas;
                        string masterBranch;
                        try
                        {
                            using var repo = new Repository(LocalDriverRepoDirectory);
                            var master = repo.Branches.Where(b => b.IsCurrentRepositoryHead).FirstOrDefault();
                            if (master == null)
                            {
                                Logger.Error($"Failed to check out driver repository: Could not locate master branch");
                                return new ConfigurationState<DriverRepoInfo>(default!, ErrorResponse.Fail("Could not locate master branch."));
                            }
                            masterBranch = master.FriendlyName;
                            repo.Reset(ResetMode.Hard);
                            Commands.Checkout(repo, master);
                            Signature author = new("please", "whymustidothis@gmail.com", DateTimeOffset.Now);
                            Commands.Pull(repo, author, null);
                            tags = repo.Tags.Select(tag => (tag.FriendlyName, tag.Target.Sha))
                                .WithIndex()
                                .Select(x => (x.Index, x.Item.FriendlyName, x.Item.Sha))
                                .ToList();
                            branchShas = repo.Branches
                                .ToDictionary(x => x.FriendlyName, x => x.Tip.Sha, StringComparer.OrdinalIgnoreCase);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Failed to check out driver repository");
                            return new ConfigurationState<DriverRepoInfo>(default!, ErrorResponse.Fail(ex));
                        }

                        // Try to locate a solution to drive from
                        var slnPath = GitPatcherRun.GetPathToSolution(LocalDriverRepoDirectory);
                        if (slnPath == null)
                        {
                            Logger.Error($"Failed to check out driver repository: Could not locate solution to run.");
                            return new ConfigurationState<DriverRepoInfo>(default!, ErrorResponse.Fail("Could not locate solution to run."));
                        }
                        var availableProjs = SolutionPatcherRun.AvailableProjectSubpaths(slnPath).ToList();
                        return new ConfigurationState<DriverRepoInfo>(
                            new DriverRepoInfo(
                                slnPath: slnPath,
                                masterBranchName: masterBranch,
                                branchShas: branchShas,
                                tags: tags,
                                availableProjects: availableProjs));
                    })
                .Replay(1)
                .RefCount();

            // Clone a second repository that we will check out the desired target commit to actually run
            var runnerRepoState = remoteRepoPath
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
                        return (ErrorResponse)await GitUtility.CheckOrCloneRepo(path.ToGetResponse(), LocalRunnerRepoDirectory, x => Logger.Information(x), cancel);
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
                this.WhenAnyValue(x => x.SelectedProjectPath.TargetPath),
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

            var projPath = SolutionPatcherConfigLogic.ProjectPath(
                driverRepoInfo
                    .Select(x => x.Item?.SolutionPath ?? string.Empty),
                this.WhenAnyValue(x => x.ProjectSubpath));

            projPath
                .Subscribe(p =>
                {
                    Logger.Information($"Setting target project path to: {p}");
                    SelectedProjectPath.TargetPath = p;
                })
                .DisposeWith(this);

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
                        this.WhenAnyValue(x => x.Profile.LockUpgrades),
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
                        this.WhenAnyValue(x => x.Profile.LockUpgrades),
                        (auto, locked) => !locked && auto),
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.BranchAutoUpdate),
                        this.WhenAnyValue(x => x.Profile.LockUpgrades),
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
                    this.WhenAnyValue(x => x.Profile.ActiveVersioning)
                        .Switch(),
                    this.WhenAnyValue(x => x.MutagenVersioning),
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    parent.Config.MainVM.NewestMutagenVersion,
                    this.WhenAnyValue(x => x.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    parent.Config.MainVM.NewestSynthesisVersion,
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
                    SelectedProjectPath.PathState()
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
                .Select((item) =>
                {
                    return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
                    {
                        try
                        {
                            if (item.runnerState.RunnableState.Failed)
                            {
                                observer.OnNext(item.runnerState.BubbleError<RunnerRepoInfo>());
                                return;
                            }
                            if (item.proj.Failed)
                            {
                                observer.OnNext(item.proj.BubbleFailure<RunnerRepoInfo>());
                                return;
                            }
                            if (item.libraryNugets.Failed)
                            {
                                observer.OnNext(item.libraryNugets.BubbleFailure<RunnerRepoInfo>());
                                return;
                            }

                            observer.OnNext(new ConfigurationState<RunnerRepoInfo>(default!)
                            {
                                RunnableState = ErrorResponse.Fail("Checking out the proper commit"),
                                IsHaltingError = false,
                            });

                            var runInfo = await GitPatcherRun.CheckoutRunnerRepository(
                                proj: item.proj.Value,
                                localRepoDir: LocalRunnerRepoDirectory,
                                patcherVersioning: item.patcherVersioning,
                                nugetVersioning: item.libraryNugets.Value,
                                logger: (s) => Logger.Information(s),
                                cancel: cancel,
                                compile: false);

                            if (runInfo.RunnableState.Failed)
                            {
                                Logger.Error($"Checking out runner repository failed: {runInfo.RunnableState.Reason}");
                                observer.OnNext(runInfo);
                                return;
                            }

                            Logger.Error($"Checking out runner repository succeeded");

                            await SolutionPatcherRun.CopyOverExtraData(runInfo.Item.ProjPath, Execution.Paths.TypicalExtraData, DisplayName, Logger.Information);

                            observer.OnNext(runInfo);
                        }
                        catch (Exception ex)
                        {
                            var str = $"Error checking out runner repository: {ex}";
                            Logger.Error(str);
                            observer.OnNext(ErrorResponse.Fail(str).BubbleFailure<RunnerRepoInfo>());
                        }
                        observer.OnCompleted();
                    });
                })
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
                .Except(this.Profile.LoadOrder.Connect()
                    .Transform(x => x.Listing.ModKey))
                .RefCount();

            var compilation = runnableState
                .Select(state =>
                {
                    return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
                    {
                        if (state.RunnableState.Failed)
                        {
                            observer.OnNext(state);
                            return;
                        }

                        try
                        {
                            Logger.Information("Compiling");
                            // Return early with the values, but mark not complete
                            observer.OnNext(new ConfigurationState<RunnerRepoInfo>(state.Item)
                            {
                                IsHaltingError = false,
                                RunnableState = ErrorResponse.Fail("Compiling")
                            });

                            // Compile to help prep
                            var compileResp = await DotNetCommands.Compile(state.Item.ProjPath, cancel, Logger.Information);
                            if (compileResp.Failed)
                            {
                                Logger.Information($"Compiling failed: {compileResp.Reason}");
                                observer.OnNext(compileResp.BubbleFailure<RunnerRepoInfo>());
                                return;
                            }

                            // Return things again, without error
                            Logger.Information("Finished compiling");
                            observer.OnNext(state);
                        }
                        catch (Exception ex)
                        {
                            var str = $"Error checking out runner repository: {ex}";
                            Logger.Error(str);
                            observer.OnNext(ErrorResponse.Fail(str).BubbleFailure<RunnerRepoInfo>());
                        }
                        observer.OnCompleted();
                    });
                })
                .Switch()
                .StartWith(new ConfigurationState<RunnerRepoInfo>(GetResponse<RunnerRepoInfo>.Fail("Compilation uninitialized")))
                .Replay(1)
                .RefCount();

            var runnability = Observable.CombineLatest(
                    compilation,
                    parent.WhenAnyValue(x => x.DataFolder),
                    parent.LoadOrder.Connect()
                        .QueryWhenChanged()
                        .StartWith(ListExt.Empty<LoadOrderEntryVM>()),
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
                            var runnability = await Synthesis.Bethesda.Execution.CLI.Commands.CheckRunnability(
                                path: i.comp.Item.ProjPath,
                                directExe: false,
                                release: parent.Release,
                                dataFolder: i.data,
                                cancel: cancel,
                                loadOrder: i.loadOrder.Select(lvm => lvm.Listing));
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
                    this.WhenAnyValue(x => x.Profile.Config.MainVM)
                        .Select(x => x.DotNetSdkInstalled)
                        .Switch()
                        .Select(x => (x, true))
                        .StartWith((new DotNetVersion(string.Empty, false), false)),
                    missingReqMods
                        .QueryWhenChanged()
                        .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                        .StartWith(ListExt.Empty<ModKey>()),
                    (driver, runner, checkout, runnability, dotnet, reqModsMissing) =>
                    {
                        if (driver.IsHaltingError) return driver;
                        if (runner.IsHaltingError) return runner;
                        if (!dotnet.Item2)
                        {
                            Logger.Information("Determining DotNet SDK installed");
                            return new ConfigurationState(ErrorResponse.Fail("Determining DotNet SDK installed"))
                            {
                                IsHaltingError = false
                            };
                        }
                        if (!dotnet.Item1.Acceptable) return new ConfigurationState(ErrorResponse.Fail("No DotNet SDK installed"));
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
                execute: () => Utility.NavigateToPath(RemoteRepoPath));

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
                            Utility.NavigateToPath(RemoteRepoPath);
                        }
                        else
                        {
                            Utility.NavigateToPath(Path.Combine(RemoteRepoPath, "tree", runnable.Target));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error opening Git webpage", ex);
                    }
                });

            NavigateToInternalFilesCommand = ReactiveCommand.Create(() => Utility.NavigateToPath(localRepoDir));

            UpdateMutagenManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: parent.Config.MainVM.NewestMutagenVersion,
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
                objectSource: parent.Config.MainVM.NewestSynthesisVersion,
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

            PatcherSettings = new PatcherSettingsVM(
                Logger,
                this,
                compilation.Select(c =>
                {
                    if (c.RunnableState.Failed) return (c.RunnableState.BubbleFailure<string>(), null);
                    return (GetResponse<string>.Succeed(c.Item.ProjPath), c.Item.TargetSynthesisVersion);
                })
                .DistinctUntilChanged(x => (x.Item1.Value, x.TargetSynthesisVersion)),
                needBuild: false)
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
                    this.RemoteRepoPath = LastSuccessfulRun.TargetRepo;
                    this.ProjectSubpath = LastSuccessfulRun.ProjectSubpath;
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
                RemoteRepoPath = this.RemoteRepoPath,
                ID = this.ID,
                SelectedProjectSubpath = this.ProjectSubpath,
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
            PatcherSettings.Persist(Logger.Information);
            return ret;
        }

        private void CopyInSettings(GithubPatcherSettings? settings)
        {
            if (settings == null)
            {
                this.ID = Guid.NewGuid().ToString();
                return;
            }
            this.RemoteRepoPath = settings.RemoteRepoPath;
            this.ID = string.IsNullOrWhiteSpace(settings.ID) ? Guid.NewGuid().ToString() : settings.ID;
            this.ProjectSubpath = settings.SelectedProjectSubpath;
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

        public override PatcherRunVM ToRunner(PatchersRunVM parent)
        {
            if (RunnableData == null)
            {
                throw new ArgumentNullException(nameof(RunnableData));
            }
            PatcherSettings.Persist(Logger.Information);
            return new PatcherRunVM(
                parent,
                this,
                new SolutionPatcherRun(
                    name: DisplayName,
                    pathToSln: RunnableData.SolutionPath,
                    pathToExtraDataBaseFolder: Execution.Paths.TypicalExtraData,
                    pathToProj: RunnableData.ProjPath));
        }

        public static IObservable<ConfigurationState<string>> GetRepoPathValidity(IObservable<string> repoPath)
        {
            return repoPath
                .DistinctUntilChanged()
                .Select(x => new ConfigurationState<string>(string.Empty)
                {
                    IsHaltingError = false,
                    RunnableState = ErrorResponse.Fail("Checking remote repository correctness.")
                })
                // But merge in the work of checking the repo on that same path to get the eventual result
                .Merge(repoPath
                    .DistinctUntilChanged()
                    .Debounce(TimeSpan.FromMilliseconds(300), RxApp.MainThreadScheduler)
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(p =>
                    {
                        try
                        {
                            if (Repository.ListRemoteReferences(p).Any()) return new ConfigurationState<string>(p);
                        }
                        catch (Exception)
                        {
                        }
                        return new ConfigurationState<string>(string.Empty, ErrorResponse.Fail("Path does not point to a valid repository."));
                    }));
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
                Log.Logger.Error(ex, $"Failure deleting git repo: {this.LocalDriverRepoDirectory}");
            }
            try
            {
                var dir = new DirectoryInfo(this.LocalRunnerRepoDirectory);
                dir.DeleteEntireFolder();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Failure deleting git repo: {this.LocalRunnerRepoDirectory}");
            }
        }

        private static string GetNickname(string nickname, string path)
        {
            if (!string.IsNullOrWhiteSpace(nickname)) return nickname;
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return "Mutagen Git Patcher";
                var span = path.AsSpan();
                var slashIndex = span.LastIndexOf('/');
                if (slashIndex != -1)
                {
                    span = span.Slice(slashIndex + 1);
                }
                return span.ToString();
            }
            catch (Exception)
            {
                return "Mutagen Git Patcher";
            }
        }

        public override void SuccessfulRunCompleted()
        {
            if (MutagenVersionDiff.SelectedVersion == null) return;
            if (SynthesisVersionDiff.SelectedVersion == null) return;
            LastSuccessfulRun = new GithubPatcherLastRunState(
                TargetRepo: this.RemoteRepoPath,
                ProjectSubpath: this.ProjectSubpath,
                Commit: this.TargetCommit,
                MutagenVersion: MutagenVersionDiff.SelectedVersion,
                SynthesisVersion: SynthesisVersionDiff.SelectedVersion);
        }
    }
}
