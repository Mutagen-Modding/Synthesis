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
using System.Threading.Tasks;
using System.Windows.Input;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers;
using System.Reactive;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class GitPatcherVM : PatcherVM
    {
        [Reactive]
        public string RemoteRepoPath { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        private readonly ObservableAsPropertyHelper<ConfigurationState> _State;
        public override ConfigurationState State => _State.Value;

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
                        var state = await GitPatcherRun.CheckOrCloneRepo(path.ToGetResponse(), LocalDriverRepoDirectory, (x) => Logger.Information(x), cancel);
                        if (state.Failed) return new ConfigurationState<DriverRepoInfo>(default!, (ErrorResponse)state);
                        cancel.ThrowIfCancellationRequested();

                        // Grab all the interesting metadata
                        List<(int Index, string Name, string Sha)> tags;
                        Dictionary<string, string> branchShas;
                        string masterBranch;
                        try
                        {
                            using var repo = new Repository(LocalDriverRepoDirectory);
                            var master = repo.Branches.Where(b => b.IsCurrentRepositoryHead).FirstOrDefault();
                            masterBranch = master.FriendlyName;
                            repo.Reset(ResetMode.Hard);
                            Commands.Checkout(repo, master);
                            Signature author = new Signature("please", "whymustidothis@gmail.com", DateTimeOffset.Now);
                            Commands.Pull(repo, author, null);
                            tags = repo.Tags.Select(tag => (tag.FriendlyName, tag.Target.Sha))
                                .WithIndex()
                                .Select(x => (x.Index, x.Item.FriendlyName, x.Item.Sha))
                                .ToList();
                            branchShas = repo.Branches
                                .ToDictionary(x => x.FriendlyName, x => x.Tip.Sha);
                        }
                        catch (Exception ex)
                        {
                            return new ConfigurationState<DriverRepoInfo>(default!, ErrorResponse.Fail(ex));
                        }

                        // Try to locate a solution to drive from
                        var slnPath = GitPatcherRun.GetPathToSolution(LocalDriverRepoDirectory);
                        if (slnPath == null) return new ConfigurationState<DriverRepoInfo>(default!, ErrorResponse.Fail("Could not locate solution to run."));
                        var availableProjs = Utility.AvailableProjectSubpaths(slnPath).ToList();
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
                        if (path.RunnableState.Failed) return new ConfigurationState(path.RunnableState);
                        using var timing = Logger.Time($"runner repo: {path.Item}");
                        return (ErrorResponse)await GitPatcherRun.CheckOrCloneRepo(path.ToGetResponse(), LocalRunnerRepoDirectory, x => Logger.Information(x), cancel);
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
                .Subscribe(p => SelectedProjectPath.TargetPath = p)
                .DisposeWith(this);

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
                    this.WhenAnyValue(x => x.TargetBranchName),
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
                        && x.Driver.Item.BranchShas.TryGetValue(x.TargetBranch, out var masterSha))
                    {
                        this.TargetCommit = masterSha;
                    }
                })
                .DisposeWith(this);
            driverRepoInfo.Select(x => x.RunnableState.Failed ? default : x.Item.Tags.OrderByDescending(x => x.Index).FirstOrDefault())
                .FilterSwitch(
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.TagAutoUpdate),
                        this.WhenAnyValue(x => x.PatcherVersioning),
                        (autoTag, versioning) => autoTag && versioning == PatcherVersioningEnum.Tag))
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    this.TargetTag = x.Name;
                    this.TargetCommit = x.Sha;
                })
                .DisposeWith(this);

            // Set up update available systems
            UpdateToBranchCommand = NoggogCommand.CreateFromObject(
                objectSource: Observable.CombineLatest(
                    Observable.CombineLatest(
                        driverRepoInfo.Select(x => x.RunnableState.Failed ? default : x.Item.BranchShas),
                        this.WhenAnyValue(x => x.TargetBranchName),
                        (dict, branch) => dict?.GetOrDefault(branch)),
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
                    Observable.CombineLatest(
                        driverRepoInfo.Select(x => x.RunnableState.Failed ? default : x.Item.Tags),
                        this.WhenAnyValue(x => x.TargetTag),
                        (tags, tag) => tags?
                            .Where(tagItem => tagItem.Name == tag)
                            .FirstOrDefault()),
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.TargetCommit),
                        this.WhenAnyValue(x => x.TargetTag),
                        (TargetSha, TargetTag) => (TargetSha, TargetTag)),
                    (tag, target) => (TagSha: tag?.Sha, Tag: tag?.Name, Current:target )),
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
                this.WhenAnyValue(x => x.TargetBranchName),
                (versioning, tag, commit, branch) => GitPatcherVersioning.Factory(versioning, tag, commit, branch));

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

                            Logger.Information("Checking out the proper commit");
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
                                observer.OnNext(runInfo);
                                return;
                            }

                            // Return early with the values, but mark not complete
                            observer.OnNext(new ConfigurationState<RunnerRepoInfo>(runInfo.Item)
                            {
                                IsHaltingError = false,
                                RunnableState = ErrorResponse.Fail("Compiling")
                            });

                            // Compile to help prep
                            var compileResp = await SolutionPatcherRun.CompileWithDotnet(item.proj.Value, cancel);
                            if (compileResp.Failed)
                            {
                                observer.OnNext(compileResp.BubbleFailure<RunnerRepoInfo>());
                                return;
                            }

                            // Return things again, without error
                            observer.OnNext(runInfo);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error checking out runner repository: {ex}");
                            throw;
                        }
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

            _State = Observable.CombineLatest(
                    driverRepoInfo
                        .Select(x => x.ToUnit()),
                    runnerRepoState,
                    runnableState
                        .Select(x => x.ToUnit()),
                    this.WhenAnyValue(x => x.Profile.Config.MainVM)
                        .Select(x => x.DotNetSdkInstalled)
                        .Switch()
                        .Select(x => (x, true))
                        .StartWith((default(System.Version?), false)),
                    (driver, runner, checkout, dotnet) =>
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
                        if (dotnet.Item1 == null) return new ConfigurationState(ErrorResponse.Fail("No DotNet SDK installed"));
                        return checkout;
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
                FollowDefaultBranch = this.BranchAutoUpdate,
                TargetBranch = this.TargetBranchName,
            };
            CopyOverSave(ret);
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
            this.BranchAutoUpdate = settings.FollowDefaultBranch;
            this.TagAutoUpdate = settings.LatestTag;
            this.TargetBranchName = settings.TargetBranch;
        }

        public override PatcherRunVM ToRunner(PatchersRunVM parent)
        {
            if (RunnableData == null)
            {
                throw new ArgumentNullException(nameof(RunnableData));
            }
            return new PatcherRunVM(
                parent,
                this,
                new SolutionPatcherRun(
                    name: DisplayName,
                    pathToSln: RunnableData.SolutionPath,
                    pathToExtraDataBaseFolder: Execution.Constants.TypicalExtraData,
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
    }
}
