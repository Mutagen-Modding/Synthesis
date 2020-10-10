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
using System.Diagnostics;

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
        public PatcherVersioningEnum PatcherVersioning { get; set; } = PatcherVersioningEnum.Master;

        [Reactive]
        public MutagenVersioningEnum MutagenVersioning { get; set; } = MutagenVersioningEnum.Match;

        public IObservableCollection<string> AvailableTags { get; }

        [Reactive]
        public string TargetTag { get; set; } = string.Empty;

        [Reactive]
        public string TargetCommit { get; set; } = string.Empty;

        [Reactive]
        public string TargetBranchName { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<RunnerRepoInfo?> _RunnableData;
        public RunnerRepoInfo? RunnableData => _RunnableData.Value;

        private readonly ObservableAsPropertyHelper<string> _ExePath;
        public string ExePath => _ExePath.Value;

        public ICommand OpenGitPageCommand { get; }

        public ICommand OpenGitPageToVersionCommand { get; }

        public GitPatcherVM(ProfileVM parent, GithubPatcherSettings? settings = null)
            : base(parent, settings)
        {
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            CopyInSettings(settings);

            LocalDriverRepoDirectory = Path.Combine(Profile.ProfileDirectory, "Git", ID, "Driver");
            LocalRunnerRepoDirectory = Path.Combine(Profile.ProfileDirectory, "Git", ID, "Runner");

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
                        var state = await GitPatcherRun.PrepRepo(path.ToGetResponse(), LocalDriverRepoDirectory, cancel);
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
                            Configuration config = repo.Config;
                            Signature author = config.BuildSignature(DateTimeOffset.Now);
                            Commands.Pull(repo, author, null);
                            tags = repo.Tags.Select(tag => tag.FriendlyName).WithIndex().ToList();
                        }
                        catch (Exception ex)
                        {
                            return new ConfigurationStateVM<DriverRepoInfo>(default!, ErrorResponse.Fail(ex));
                        }

                        // Try to locate a solution to drive from
                        var slnPath = GetPathToSolution(LocalDriverRepoDirectory);
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
                    ErrorResponse.Fail("Cloning runner repository"),
                    async (path, cancel) =>
                    {
                        if (path.RunnableState.Failed) return path.RunnableState;
                        using var timing = Logger.ForContext("RemotePath", path.Item).Time("runner repo");
                        return await GitPatcherRun.PrepRepo(path.ToGetResponse(), LocalRunnerRepoDirectory, cancel);
                    })
                .Replay(1)
                .RefCount();

            // Expose a lot of the metadata
            _RepoClonesValid = Observable.CombineLatest(
                    driverRepoInfo,
                    runnerRepoState,
                    (driver, runner) => driver.RunnableState.Succeeded && runner.Succeeded)
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

            // Checkout desired patcher commit on the runner repository
            var checkoutInput = Observable.CombineLatest(
                    driverRepoInfo.Select(x => x.RunnableState.Failed ? string.Empty : x.Item.MasterBranchName),
                    this.WhenAnyValue(x => x.PatcherVersioning),
                    runnerRepoState,
                    SelectedProjectPath.PathState()
                        .Select(x => x.Succeeded ? x : GetResponse<string>.Fail("No patcher project selected.")),
                    this.WhenAnyValue(x => x.TargetTag),
                    this.WhenAnyValue(x => x.TargetCommit),
                    this.WhenAnyValue(x => x.TargetBranchName),
                    (master, versioning, runnerState, proj, tag, commit, branch) => (master, versioning, runnerState, proj, tag, commit, branch))
                .Replay(1)
                .RefCount();
            var runnableState = checkoutInput
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .Do(item => Logger.Information($"Checking out {CheckoutStateToString(item)}"))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectReplaceWithIntermediate(
                    new ConfigurationStateVM<RunnerRepoInfo>(default!)
                    {
                        RunnableState = ErrorResponse.Fail("Checking out the proper commit"),
                        IsHaltingError = false,
                    },
                    async (item, cancel) =>
                    {
                        async Task<GetResponse<RunnerRepoInfo>> Execute()
                        {
                            if (item.runnerState.Failed) return item.runnerState.BubbleFailure<RunnerRepoInfo>();
                            if (item.proj.Failed) return item.proj.BubbleFailure<RunnerRepoInfo>();
                            cancel.ThrowIfCancellationRequested();
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
                                switch (item.versioning)
                                {
                                    case PatcherVersioningEnum.Master:
                                        targetSha = repo.Branches
                                            .Where(b => b.FriendlyName == item.master)
                                            .FirstOrDefault()
                                            ?.Tip.Sha;
                                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate master commit");
                                        target = null;
                                        break;
                                    case PatcherVersioningEnum.Tag:
                                        if (string.IsNullOrWhiteSpace(item.tag)) return GetResponse<RunnerRepoInfo>.Fail("No tag selected");
                                        targetSha = repo.Tags[item.tag]?.Target.Sha;
                                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate tag");
                                        target = item.tag;
                                        break;
                                    case PatcherVersioningEnum.Commit:
                                        targetSha = item.commit;
                                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit");
                                        target = item.commit;
                                        break;
                                    case PatcherVersioningEnum.Branch:
                                        if (string.IsNullOrWhiteSpace(item.branch)) return GetResponse<RunnerRepoInfo>.Fail($"Target branch had no name.");
                                        var targetBranch = repo.Branches[item.branch];
                                        if (targetBranch == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate branch: {item.branch}");
                                        targetSha = targetBranch.Tip.Sha;
                                        target = item.branch;
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                if (!ObjectId.TryParse(targetSha, out var objId)) return GetResponse<RunnerRepoInfo>.Fail("Malformed sha string");

                                var commit = repo.Lookup(objId, ObjectType.Commit) as Commit;
                                if (commit == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");

                                var slnPath = GetPathToSolution(LocalRunnerRepoDirectory);
                                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                                var projName = Path.GetFileName(item.proj.Value);

                                var availableProjs = SolutionPatcherConfigLogic.AvailableProject(slnPath).ToList();

                                var foundProjSubPath = availableProjs
                                    .Where(av => Path.GetFileName(av).Equals(projName))
                                    .FirstOrDefault();

                                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {projName}.");

                                repo.Reset(ResetMode.Hard, commit, new CheckoutOptions());
                                return GetResponse<RunnerRepoInfo>.Succeed(
                                    new RunnerRepoInfo(
                                        slnPath: slnPath,
                                        projPath: Path.Combine(LocalDriverRepoDirectory, foundProjSubPath),
                                        target: target,
                                        commitMsg: commit.Message,
                                        commitDate: commit.Author.When.LocalDateTime));
                            }
                            catch (Exception ex)
                            {
                                return GetResponse<RunnerRepoInfo>.Fail(ex);
                            }
                        }
                        var ret = new ConfigurationStateVM<RunnerRepoInfo>(await Execute());
                        if (ret.RunnableState.Succeeded)
                        {
                            Logger.Information($"Finished checking out {CheckoutStateToString(item)}");
                        }
                        else
                        {
                            Logger.Information($"Failed checking out {CheckoutStateToString(item)}");
                        }
                        return ret;
                    })
                .Replay(1)
                .RefCount();

            _RunnableData = runnableState
                .Select(x => x.RunnableState.Succeeded ? x.Item : default(RunnerRepoInfo?))
                .ToGuiProperty(this, nameof(RunnableData));

            _ExePath = runnableState
                .SelectReplace(async (x, cancel) =>
                {
                    if (x.RunnableState.Failed) return string.Empty;
                    using var timing = Logger.Time($"locate path to exe from {x.Item.ProjPath}");
                    var exePath = await SolutionPatcherConfigLogic.PathToExe(x.Item.ProjPath, cancel);
                    if (exePath.Failed) return string.Empty;
                    return exePath.Value;
                })
                .ToGuiProperty<string>(this, nameof(ExePath));

            _State = Observable.CombineLatest(
                    driverRepoInfo
                        .Select(x => x.ToUnit()),
                    runnerRepoState
                        .Select(x => new ConfigurationStateVM(x)),
                    runnableState
                        .Select(x => x.ToUnit()),
                    (driver, runner, checkout) =>
                    {
                        if (driver.IsHaltingError) return driver;
                        if (runner.IsHaltingError) return runner;
                        return checkout;
                    })
                .ToGuiProperty<ConfigurationStateVM>(this, nameof(State), ConfigurationStateVM.Success);

            OpenGitPageCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.RepoValidity)
                    .Select(x => x.Succeeded),
                execute: () => Utility.OpenWebsite(RemoteRepoPath));

            OpenGitPageToVersionCommand = ReactiveCommand.Create(
                canExecute: runnableState
                    .Select(x => x.RunnableState.Succeeded),
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
                TargetTag = this.TargetTag,
                TargetCommit = this.TargetCommit,
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
            this.TargetTag = settings.TargetTag;
            this.TargetCommit = settings.TargetCommit;
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
                    nickname: DisplayName,
                    pathToExe: ExePath,
                    pathToSln: RunnableData.SolutionPath,
                    pathToProj: SelectedProjectPath.TargetPath));
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

        private static string GetPathToSolution(string pathToRepo)
        {
            return Directory.EnumerateFiles(pathToRepo, "*.sln").FirstOrDefault();
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

            public RunnerRepoInfo(
                string slnPath,
                string projPath,
                string? target,
                string commitMsg,
                DateTime commitDate)
            {
                SolutionPath = slnPath;
                ProjPath = projPath;
                Target = target;
                CommitMessage = commitMsg;
                CommitDate = commitDate;
            }
        }

        private static string CheckoutStateToString((
            string MasterBranchName,
            PatcherVersioningEnum Versioning,
            ErrorResponse RunnerState,
            GetResponse<string> Project,
            string Tag,
            string Commit,
            string Branch) item)
        {
            if (item.RunnerState.Failed
                || item.Project.Failed)
            {
                return "Failed checkout state";
            }

            switch (item.Versioning)
            {
                case PatcherVersioningEnum.Master:
                    return $"main branch {item.MasterBranchName}";
                case PatcherVersioningEnum.Tag:
                    return $"tag {item.Tag}";
                case PatcherVersioningEnum.Branch:
                    return $"branch {item.Branch}";
                case PatcherVersioningEnum.Commit:
                    return $"master {item.Commit}";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
