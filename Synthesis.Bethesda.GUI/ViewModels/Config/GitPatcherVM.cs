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
using Synthesis.Bethesda.Execution;
using DynamicData.Binding;
using DynamicData;
using static Synthesis.Bethesda.GUI.SolutionPatcherVM;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class GitPatcherVM : PatcherVM
    {
        [Reactive]
        public string RemoteRepoPath { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        private readonly ObservableAsPropertyHelper<ConfigurationStateVM> _State;
        public override ConfigurationStateVM State => _State.Value;

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
        public bool LatestTag { get; set; } = true;

        [Reactive]
        public string TargetCommit { get; set; } = string.Empty;

        [Reactive]
        public bool FollowDefaultBranch { get; set; } = true;

        [Reactive]
        public string TargetBranchName { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<RunnerRepoInfo?> _RunnableData;
        public RunnerRepoInfo? RunnableData => _RunnableData.Value;

        public ICommand OpenGitPageCommand { get; }

        public ICommand OpenGitPageToVersionCommand { get; }

        [Reactive]
        public MutagenVersioningEnum MutagenVersioning { get; set; } = MutagenVersioningEnum.Latest;

        [Reactive]
        public string ManualMutagenVersion { get; set; } = string.Empty;

        [Reactive]
        public SynthesisVersioningEnum SynthesisVersioning { get; set; } = SynthesisVersioningEnum.Latest;

        [Reactive]
        public string ManualSynthesisVersion { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<(string? MatchVersion, string? SelectedVersion)> _UsedMutagenVersion;
        public (string? MatchVersion, string? SelectedVersion) UsedMutagenVersion => _UsedMutagenVersion.Value;

        private readonly ObservableAsPropertyHelper<(string? MatchVersion, string? SelectedVersion)> _UsedSynthesisVersion;
        public (string? MatchVersion, string? SelectedVersion) UsedSynthesisVersion => _UsedSynthesisVersion.Value;

        public GitPatcherVM(ProfileVM parent, GithubPatcherSettings? settings = null)
            : base(parent, settings)
        {
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            CopyInSettings(settings);

            LocalDriverRepoDirectory = Path.Combine(Profile.ProfileDirectory, "Git", ID, "Driver");
            LocalRunnerRepoDirectory = GitPatcherRun.RunnerRepoDirectory(Profile.ID, ID);

            _DisplayName = this.WhenAnyValue(
                x => x.Nickname,
                x => x.RemoteRepoPath,
                (nickname, path) =>
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
                })
                .ToGuiProperty<string>(this, nameof(DisplayName));

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
                    new ConfigurationStateVM<DriverRepoInfo>(default!)
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
                        if (state.Failed) return new ConfigurationStateVM<DriverRepoInfo>(default!, (ErrorResponse)state);
                        cancel.ThrowIfCancellationRequested();

                        // Grab all the interesting metadata
                        List<(int Index, string Name)> tags;
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
                            tags = repo.Tags.Select(tag => tag.FriendlyName).WithIndex().ToList();
                        }
                        catch (Exception ex)
                        {
                            return new ConfigurationStateVM<DriverRepoInfo>(default!, ErrorResponse.Fail(ex));
                        }

                        // Try to locate a solution to drive from
                        var slnPath = GitPatcherRun.GetPathToSolution(LocalDriverRepoDirectory);
                        if (slnPath == null) return new ConfigurationStateVM<DriverRepoInfo>(default!, ErrorResponse.Fail("Could not locate solution to run."));
                        var availableProjs = Utility.AvailableProjectSubpaths(slnPath).ToList();
                        return new ConfigurationStateVM<DriverRepoInfo>(
                            new DriverRepoInfo(
                                slnPath: slnPath,
                                masterBranchName: masterBranch,
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
                    new ConfigurationStateVM(ErrorResponse.Fail("Cloning runner repository"))
                    {
                        IsHaltingError = false
                    },
                    async (path, cancel) =>
                    {
                        if (path.RunnableState.Failed) return new ConfigurationStateVM(path.RunnableState);
                        var log = Logger.ForContext("RemotePath", path.Item);
                        using var timing = log.Time("runner repo");
                        return (ErrorResponse)await GitPatcherRun.CheckOrCloneRepo(path.ToGetResponse(), LocalRunnerRepoDirectory, x => log.Information(x), cancel);
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
                .Select(x => x.Item?.Tags ?? Enumerable.Empty<(int Index, string Name)>())
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .Filter(
                    tagInput.Select(x =>
                    {
                        if (x.count == 0) return new Func<(int Index, string Name), bool>(_ => false);
                        if (x.count == 1) return new Func<(int Index, string Name), bool>(_ => true);
                        if (!x.targetPath.EndsWith(".csproj")) return new Func<(int Index, string Name), bool>(_ => false);
                        var projName = Path.GetFileName(x.targetPath);
                        return new Func<(int Index, string Name), bool>(i => i.Name.StartsWith(projName, StringComparison.OrdinalIgnoreCase));
                    }))
                .Sort(SortExpressionComparer<(int Index, string Name)>.Descending(x => x.Index))
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
            driverRepoInfo.Select(x => x.RunnableState.Failed ? string.Empty : x.Item.MasterBranchName)
                .FilterSwitch(this.WhenAnyValue(x => x.FollowDefaultBranch))
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x => TargetBranchName = x)
                .DisposeWith(this);
            driverRepoInfo.Select(x => x.RunnableState.Failed ? string.Empty : x.Item.Tags.OrderByDescending(x => x.Index).Select(x => x.Name).FirstOrDefault())
                .FilterSwitch(this.WhenAnyValue(x => x.LatestTag))
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x => TargetTag = x)
                .DisposeWith(this);

            // Get the selected versioning preferences
            var patcherVersioning = Observable.CombineLatest(
                this.WhenAnyValue(x => x.PatcherVersioning),
                this.WhenAnyValue(x => x.TargetTag),
                this.WhenAnyValue(x => x.TargetCommit),
                this.WhenAnyValue(x => x.TargetBranchName),
                (versioning, tag, commit, branch) => (versioning, tag, commit, branch));

            var libraryNugets = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.MutagenVersioning),
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    this.WhenAnyValue(x => x.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    parent.Config.MainVM.NewestMutagenVersion,
                    parent.Config.MainVM.NewestSynthesisVersion,
                    (mutaVersioning, mutaManual, synthVersioning, synthManual, newestMuta, newestSynth) =>
                    (mutaVersioning, mutaManual, synthVersioning, synthManual, newestMuta, newestSynth))
                .Select(nugets =>
                {
                    if (nugets.mutaVersioning == MutagenVersioningEnum.Latest && nugets.newestMuta == null)
                    {
                        return GetResponse<(MutagenVersioningEnum MutaVersioning, string MutaVersion, SynthesisVersioningEnum SynthVersioning, string SynthVersion)>
                            .Fail("Latest Mutagen version is desired, but latest version is not known.");
                    }
                    if (nugets.synthVersioning == SynthesisVersioningEnum.Latest && nugets.newestSynth == null)
                    {
                        return GetResponse<(MutagenVersioningEnum MutaVersioning, string MutaVersion, SynthesisVersioningEnum SynthVersioning, string SynthVersion)>
                            .Fail("Latest Synthesis version is desired, but latest version is not known.");
                    }
                    var muta = nugets.mutaVersioning switch
                    {
                        MutagenVersioningEnum.Latest => (nugets.newestMuta, nugets.mutaVersioning),
                        MutagenVersioningEnum.Match => (null, nugets.mutaVersioning),
                        MutagenVersioningEnum.Manual => (nugets.mutaManual, nugets.mutaVersioning),
                        _ => throw new NotImplementedException(),
                    };
                    var synth = nugets.synthVersioning switch
                    {
                        SynthesisVersioningEnum.Latest => (nugets.newestSynth, nugets.synthVersioning),
                        SynthesisVersioningEnum.Match => (null, nugets.synthVersioning),
                        SynthesisVersioningEnum.Manual => (nugets.synthManual, nugets.synthVersioning),
                        _ => throw new NotImplementedException(),
                    };
                    return GetResponse<(MutagenVersioningEnum MutaVersioning, string MutaVersion, SynthesisVersioningEnum SynthVersioning, string SynthVersion)>
                        .Succeed((muta.mutaVersioning, muta.Item1!, synth.synthVersioning, synth.Item1!));
                })
                .Replay(1)
                .RefCount();

            // Checkout desired patcher commit on the runner repository
            var checkoutInput = Observable.CombineLatest(
                    runnerRepoState,
                    SelectedProjectPath.PathState()
                        .Select(x => x.Succeeded ? x : GetResponse<string>.Fail("No patcher project selected.")),
                    patcherVersioning,
                    libraryNugets,
                    (runnerState, proj, patcherVersioning, libraryNugets) =>
                    (runnerState, proj, patcherVersioning, libraryNugets))
                .Replay(1)
                .RefCount();
            var runnableState = checkoutInput
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectReplaceWithIntermediate(
                    new ConfigurationStateVM<RunnerRepoInfo>(default!)
                    {
                        RunnableState = ErrorResponse.Fail("Checking out the proper commit"),
                        IsHaltingError = false,
                    },
                    async (item, cancel) =>
                    {
                        async Task<ConfigurationStateVM<RunnerRepoInfo>> Execute()
                        {
                            if (item.runnerState.RunnableState.Failed) return item.runnerState.BubbleError<RunnerRepoInfo>();
                            if (item.proj.Failed) return item.proj.BubbleFailure<RunnerRepoInfo>();
                            if (item.libraryNugets.Failed) return item.libraryNugets.BubbleFailure<RunnerRepoInfo>();
                            cancel.ThrowIfCancellationRequested();

                            var checkoutTargetStr = item.patcherVersioning.versioning switch
                            {
                                PatcherVersioningEnum.Tag => $"tag {item.patcherVersioning.tag}",
                                PatcherVersioningEnum.Branch  => $"branch {item.patcherVersioning.branch}",
                                PatcherVersioningEnum.Commit => $"master {item.patcherVersioning.commit}",
                                _ => throw new NotImplementedException(),
                            };
                            Logger.Information($"Targeting {checkoutTargetStr}");

                            using var timing = Logger.Time("runner checkout");
                            try
                            {
                                const string RunnerBranch = "SynthesisRunner";
                                using var repo = new Repository(LocalRunnerRepoDirectory);
                                var runnerBranch = repo.Branches[RunnerBranch] ?? repo.CreateBranch(RunnerBranch);
                                repo.Reset(ResetMode.Hard);
                                Commands.Checkout(repo, runnerBranch);
                                string? targetSha;
                                string? target;
                                switch (item.patcherVersioning.versioning)
                                {
                                    case PatcherVersioningEnum.Tag:
                                        if (string.IsNullOrWhiteSpace(item.patcherVersioning.tag)) return GetResponse<RunnerRepoInfo>.Fail("No tag selected");
                                        targetSha = repo.Tags[item.patcherVersioning.tag]?.Target.Sha;
                                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate tag");
                                        target = item.patcherVersioning.tag;
                                        break;
                                    case PatcherVersioningEnum.Commit:
                                        targetSha = item.patcherVersioning.commit;
                                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit");
                                        target = item.patcherVersioning.commit;
                                        break;
                                    case PatcherVersioningEnum.Branch:
                                        if (string.IsNullOrWhiteSpace(item.patcherVersioning.branch)) return GetResponse<RunnerRepoInfo>.Fail($"Target branch had no name.");
                                        var targetBranch = repo.Branches[$"origin/{item.patcherVersioning.branch}"];
                                        if (targetBranch == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate branch: {item.patcherVersioning.branch}");
                                        targetSha = targetBranch.Tip.Sha;
                                        target = item.patcherVersioning.branch;
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                if (!ObjectId.TryParse(targetSha, out var objId)) return GetResponse<RunnerRepoInfo>.Fail("Malformed sha string");

                                cancel.ThrowIfCancellationRequested();
                                var commit = repo.Lookup(objId, ObjectType.Commit) as Commit;
                                if (commit == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");

                                cancel.ThrowIfCancellationRequested();
                                var slnPath = GitPatcherRun.GetPathToSolution(LocalRunnerRepoDirectory);
                                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                                var foundProjSubPath = SolutionPatcherRun.AvailableProject(slnPath, item.proj.Value);

                                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {item.proj.Value}.");

                                cancel.ThrowIfCancellationRequested();
                                Logger.Information($"Checking out {targetSha}");
                                repo.Reset(ResetMode.Hard, commit, new CheckoutOptions());

                                var projPath = Path.Combine(LocalRunnerRepoDirectory, foundProjSubPath);

                                // Compile to help prep
                                cancel.ThrowIfCancellationRequested();
                                Logger.Information($"Mutagen Nuget: {item.libraryNugets.Value.MutaVersioning} {item.libraryNugets.Value.MutaVersion}");
                                Logger.Information($"Synthesis Nuget: {item.libraryNugets.Value.SynthVersioning} {item.libraryNugets.Value.SynthVersion}");
                                GitPatcherRun.SwapInDesiredVersionsForSolution(
                                    slnPath,
                                    drivingProjSubPath: foundProjSubPath,
                                    mutagenVersion: item.libraryNugets.Value.MutaVersioning == MutagenVersioningEnum.Match ? null : item.libraryNugets.Value.MutaVersion,
                                    listedMutagenVersion: out var listedMutagenVersion,
                                    synthesisVersion: item.libraryNugets.Value.SynthVersioning == SynthesisVersioningEnum.Match ? null : item.libraryNugets.Value.SynthVersion,
                                    listedSynthesisVersion: out var listedSynthesisVersion);
                                var compileResp = await SolutionPatcherRun.CompileWithDotnet(projPath, cancel);
                                if (compileResp.Failed) return compileResp.BubbleFailure<RunnerRepoInfo>();

                                return GetResponse<RunnerRepoInfo>.Succeed(
                                    new RunnerRepoInfo(
                                        slnPath: slnPath,
                                        projPath: projPath,
                                        target: target,
                                        commitMsg: commit.Message,
                                        commitDate: commit.Author.When.LocalDateTime,
                                        listedSynthesis: listedSynthesisVersion,
                                        listedMutagen: listedMutagenVersion));
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                return GetResponse<RunnerRepoInfo>.Fail(ex);
                            }
                        }
                        var ret = await Execute();
                        if (ret.RunnableState.Succeeded)
                        {
                            Logger.Information($"Finished checking out");
                        }
                        else
                        {
                            Logger.Error(ret.RunnableState, $"Failed checking out");
                        }
                        return ret;
                    })
                .Replay(1)
                .RefCount();

            _RunnableData = runnableState
                .Select(x => x.RunnableState.Succeeded ? x.Item : default(RunnerRepoInfo?))
                .ToGuiProperty(this, nameof(RunnableData));

            _UsedMutagenVersion = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.RunnableData)
                        .Select(x => x?.ListedMutagenVersion),
                    libraryNugets.Select(x => x.Value.MutaVersion),
                    (matchVersion, selVersion) => (matchVersion, selVersion))
                .ToGuiProperty(this, nameof(UsedMutagenVersion));

            _UsedSynthesisVersion = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.RunnableData)
                        .Select(x => x?.ListedSynthesisVersion),
                    libraryNugets.Select(x => x.Value.SynthVersion),
                    (matchVersion, selVersion) => (matchVersion, selVersion))
                .ToGuiProperty(this, nameof(UsedSynthesisVersion));

            _State = Observable.CombineLatest(
                    driverRepoInfo
                        .Select(x => x.ToUnit()),
                    runnerRepoState,
                    runnableState
                        .Select(x => x.ToUnit()),
                    this.WhenAnyValue(x => x.Profile.Config.MainVM)
                        .Select(x => x.DotNetSdkInstalled)
                        .Switch(),
                    (driver, runner, checkout, dotnet) =>
                    {
                        if (driver.IsHaltingError) return driver;
                        if (runner.IsHaltingError) return runner;
                        if (dotnet == null) return new ConfigurationStateVM(ErrorResponse.Fail("No dotnet SDK installed"));
                        return checkout;
                    })
                .ToGuiProperty<ConfigurationStateVM>(this, nameof(State), new ConfigurationStateVM(ErrorResponse.Fail("Evaluating"))
                {
                    IsHaltingError = false
                });

            OpenGitPageCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.RepoValidity)
                    .Select(x => x.Succeeded),
                execute: () => Utility.OpenWebsite(RemoteRepoPath));

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
                            Utility.OpenWebsite(RemoteRepoPath);
                        }
                        else
                        {
                            Utility.OpenWebsite(Path.Combine(RemoteRepoPath, "tree", runnable.Target));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error opening Git webpage", ex);
                    }
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
                MutagenVersioning = this.MutagenVersioning,
                ManualMutagenVersion = this.ManualMutagenVersion,
                SynthesisVersioning = this.SynthesisVersioning,
                ManualSynthesisVersion = this.ManualSynthesisVersion,
                TargetTag = this.TargetTag,
                TargetCommit = this.TargetCommit,
                LatestTag = this.LatestTag,
                FollowDefaultBranch = this.FollowDefaultBranch,
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
            this.MutagenVersioning = settings.MutagenVersioning;
            this.SynthesisVersioning = settings.SynthesisVersioning;
            this.ManualMutagenVersion = settings.ManualMutagenVersion;
            this.ManualSynthesisVersion = settings.ManualSynthesisVersion;
            this.TargetTag = settings.TargetTag;
            this.TargetCommit = settings.TargetCommit;
            this.FollowDefaultBranch = settings.FollowDefaultBranch;
            this.LatestTag = settings.LatestTag;
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

        public static IObservable<ConfigurationStateVM<string>> GetRepoPathValidity(IObservable<string> repoPath)
        {
            return repoPath
                .DistinctUntilChanged()
                .Select(x => new ConfigurationStateVM<string>(string.Empty)
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
                            if (Repository.ListRemoteReferences(p).Any()) return new ConfigurationStateVM<string>(p);
                        }
                        catch (Exception)
                        {
                        }
                        return new ConfigurationStateVM<string>(string.Empty, ErrorResponse.Fail("Path does not point to a valid repository."));
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

        private class DriverRepoInfo
        {
            public readonly string SolutionPath;
            public readonly List<(int Index, string Name)> Tags;
            public readonly List<string> AvailableProjects;
            public readonly string MasterBranchName;

            public DriverRepoInfo(
                string slnPath,
                string masterBranchName,
                List<(int Index, string Name)> tags,
                List<string> availableProjects)
            {
                SolutionPath = slnPath;
                Tags = tags;
                MasterBranchName = masterBranchName;
                AvailableProjects = availableProjects;
            }
        }

        public class RunnerRepoInfo
        {
            public readonly string SolutionPath;
            public readonly string ProjPath;
            public readonly string? Target;
            public readonly string CommitMessage;
            public readonly DateTime CommitDate;
            public readonly string? ListedMutagenVersion;
            public readonly string? ListedSynthesisVersion;

            public RunnerRepoInfo(
                string slnPath,
                string projPath,
                string? target,
                string commitMsg,
                DateTime commitDate,
                string? listedSynthesis,
                string? listedMutagen)
            {
                SolutionPath = slnPath;
                ProjPath = projPath;
                Target = target;
                CommitMessage = commitMsg;
                CommitDate = commitDate;
                ListedMutagenVersion = listedMutagen;
                ListedSynthesisVersion = listedSynthesis;
            }
        }
    }
}
