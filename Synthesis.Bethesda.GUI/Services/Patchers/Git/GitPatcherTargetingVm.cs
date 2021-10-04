using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IPatcherVersioningFollower
    {
        IObservable<GitPatcherVersioning> ActivePatcherVersion { get; }
    }
    
    public interface IGitPatcherTargetingVm : IPatcherVersioningFollower
    {
        PatcherVersioningEnum PatcherVersioning { get; set; }
        string TargetTag { get; set; }
        string TargetCommit { get; set; }
        string TargetBranchName { get; set; }
        bool TagAutoUpdate { get; set; }
        bool BranchAutoUpdate { get; set; }
        bool BranchFollowMain { get; set; }
        ReactiveCommand<Unit, Unit> UpdateToBranchCommand { get; }
        ReactiveCommand<Unit, Unit> UpdateToTagCommand { get; }
    }

    public class GitPatcherTargetingVm : ViewModel, IGitPatcherTargetingVm
    {
        [Reactive] public PatcherVersioningEnum PatcherVersioning { get; set; } = PatcherVersioningEnum.Branch;

        [Reactive] public string TargetTag { get; set; } = string.Empty;

        [Reactive] public string TargetCommit { get; set; } = string.Empty;

        [Reactive] public string TargetBranchName { get; set; } = string.Empty;

        [Reactive] public bool TagAutoUpdate { get; set; } = false;
        
        [Reactive] public bool BranchAutoUpdate { get; set; } = false;

        [Reactive] public bool BranchFollowMain { get; set; } = true;

        public IObservable<GitPatcherVersioning> ActivePatcherVersion { get; }

        public ReactiveCommand<Unit, Unit> UpdateToBranchCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateToTagCommand { get; }

        public GitPatcherTargetingVm(
            ILockToCurrentVersioning lockToCurrentVersioning,
            IDriverRepositoryPreparationFollower driverRepositoryPreparation)
        {
            var targetOriginBranchName = this.WhenAnyValue(x => x.TargetBranchName)
                .Select(x => $"origin/{x}")
                .Replay(1).RefCount();
            
            // Set latest checkboxes to drive user input
            driverRepositoryPreparation.DriverInfo
                .FlowSwitch(this.WhenAnyValue(x => x.BranchFollowMain))
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
                    driverRepositoryPreparation.DriverInfo,
                    targetOriginBranchName,
                    (Driver, TargetBranch) => (Driver, TargetBranch))
                .FlowSwitch(
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
            driverRepositoryPreparation.DriverInfo
                .Select(x =>
                    x.RunnableState.Failed ? default : x.Item.Tags.OrderByDescending(x => x.Index).FirstOrDefault())
                .FlowSwitch(
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.TagAutoUpdate),
                        lockToCurrentVersioning.WhenAnyValue(x => x.Lock),
                        this.WhenAnyValue(x => x.PatcherVersioning),
                        (autoTag, locked, versioning) => !locked && autoTag && versioning == PatcherVersioningEnum.Tag))
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    this.TargetTag = x?.Name ?? string.Empty;
                    this.TargetCommit = x?.Sha ?? string.Empty;
                })
                .DisposeWith(this);

            var targetBranchSha = Observable.CombineLatest(
                    driverRepositoryPreparation.DriverInfo
                        .Select(x => x.RunnableState.Failed ? default : x.Item.BranchShas),
                    targetOriginBranchName,
                    (dict, branch) => dict?.GetOrDefault(branch))
                .Replay(1)
                .RefCount();
            var targetTag = Observable.CombineLatest(
                    driverRepositoryPreparation.DriverInfo
                        .Select(x => x.RunnableState.Failed ? default : x.Item.Tags),
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
                .Subscribe(x => { this.TargetCommit = x.targetBranchSha ?? string.Empty; })
                .DisposeWith(this);
            Observable.CombineLatest(
                    targetTag,
                    this.WhenAnyValue(x => x.TargetCommit),
                    (targetTagSha, targetCommit) => (targetTagSha, targetCommit))
                .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                .Where(x => x.targetTagSha != null && TargetCommit.IsNullOrWhitespace())
                .Subscribe(x => { this.TargetCommit = x.targetTagSha?.Sha ?? string.Empty; })
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
                execute: o => { this.TargetCommit = o.BranchSha!; },
                this);

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
                this);

            ActivePatcherVersion = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.PatcherVersioning),
                    this.WhenAnyValue(x => x.TargetTag),
                    this.WhenAnyValue(x => x.TargetCommit),
                    targetOriginBranchName,
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.TagAutoUpdate),
                        lockToCurrentVersioning.WhenAnyValue(x => x.Lock),
                        (auto, locked) => !locked && auto),
                    Observable.CombineLatest(
                        this.WhenAnyValue(x => x.BranchAutoUpdate),
                        lockToCurrentVersioning.WhenAnyValue(x => x.Lock),
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
        }
    }
}