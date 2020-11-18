using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Wabbajack.Common;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileVM : ViewModel, INugetVersioningVM
    {
        public ConfigurationVM Config { get; }
        public GameRelease Release { get; }

        public SourceList<PatcherVM> Patchers { get; } = new SourceList<PatcherVM>();

        public ICommand AddGitPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }
        public ICommand AddSnippetPatcherCommand { get; }
        public ICommand GoToErrorPatcher { get; }

        public string ID { get; private set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public string ProfileDirectory { get; }
        public string WorkingDirectory { get; }

        private readonly ObservableAsPropertyHelper<string> _DataFolder;
        public string DataFolder => _DataFolder.Value;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _BlockingError;
        public ErrorResponse BlockingError => _BlockingError.Value;

        private readonly ObservableAsPropertyHelper<GetResponse<PatcherVM>> _LargeOverallError;
        public GetResponse<PatcherVM> LargeOverallError => _LargeOverallError.Value;

        public IObservableList<LoadOrderListing> LoadOrder { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsActive;
        public bool IsActive => _IsActive.Value;

        [Reactive]
        public NugetVersioningEnum MutagenVersioning { get; set; }

        [Reactive]
        public string ManualMutagenVersion { get; set; } = string.Empty;

        [Reactive]
        public NugetVersioningEnum SynthesisVersioning { get; set; }

        [Reactive]
        public string ManualSynthesisVersion { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<(string? MatchVersion, string? SelectedVersion)> _MutagenVersionDiff;
        public (string? MatchVersion, string? SelectedVersion) MutagenVersionDiff => _MutagenVersionDiff.Value;

        private readonly ObservableAsPropertyHelper<(string? MatchVersion, string? SelectedVersion)> _SynthesisVersionDiff;
        public (string? MatchVersion, string? SelectedVersion) SynthesisVersionDiff => _SynthesisVersionDiff.Value;

        public IObservable<SynthesisNugetVersioning> UsedNugets { get; }

        public ProfileVM(ConfigurationVM parent, GameRelease? release = null, string? id = null)
        {
            ID = id ?? Guid.NewGuid().ToString();
            Config = parent;
            Release = release ?? GameRelease.Oblivion;
            AddGitPatcherCommand = ReactiveCommand.Create(() => SetInitializer(new GitPatcherInitVM(this)));
            AddSolutionPatcherCommand = ReactiveCommand.Create(() => SetInitializer(new SolutionPatcherInitVM(this)));
            AddCliPatcherCommand = ReactiveCommand.Create(() => SetInitializer(new CliPatcherInitVM(this)));
            AddSnippetPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new CodeSnippetPatcherVM(this)));

            ProfileDirectory = Path.Combine(Execution.Constants.WorkingDirectory, ID);
            WorkingDirectory = Execution.Constants.ProfileWorkingDirectory(ID);

            var dataFolderResult = this.WhenAnyValue(x => x.Release)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    try
                    {
                        return GetResponse<string>.Succeed(
                            Path.Combine(x.ToWjGame().MetaData().GameLocation().ToString(), "Data"));
                    }
                    catch (Exception ex)
                    {
                        return GetResponse<string>.Fail(string.Empty, ex);
                    }
                })
                .Replay(1)
                .RefCount();

            _DataFolder = dataFolderResult
                .Select(x => x.Value)
                .ToGuiProperty<string>(this, nameof(DataFolder));

            var loadOrderResult = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.Release),
                    dataFolderResult,
                    (release, dataFolder) => (release, dataFolder))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    if (x.dataFolder.Failed)
                    {
                        return (Results: Observable.Empty<IChangeSet<LoadOrderListing>>(), State: Observable.Return<ErrorResponse>(ErrorResponse.Fail("Data folder not set")));
                    }
                    return (Results: Mutagen.Bethesda.LoadOrder.GetLiveLoadOrder(x.release, x.dataFolder.Value, out var errors), State: errors);
                })
                .Replay(1)
                .RefCount();

            LoadOrder = loadOrderResult
                .Select(x => x.Results)
                .Switch()
                .AsObservableList();

            _LargeOverallError = Observable.CombineLatest(
                    dataFolderResult,
                    loadOrderResult
                        .Select(x => x.State)
                        .Switch(),
                    Patchers.Connect()
                        .AutoRefresh(x => x.IsOn)
                        .Filter(p => p.IsOn)
                        .AutoRefresh(x => x.State)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<PatcherVM>()),
                    (dataFolder, loadOrder, coll) =>
                    {
                        if (coll.Count == 0) return GetResponse<PatcherVM>.Fail("There are no enabled patchers to run.");
                        if (!dataFolder.Succeeded) return dataFolder.BubbleFailure<PatcherVM>();
                        if (!loadOrder.Succeeded) return loadOrder.BubbleFailure<PatcherVM>();
                        var blockingError = coll.FirstOrDefault(p => p.State.IsHaltingError);
                        if (blockingError != null)
                        {
                            return GetResponse<PatcherVM>.Fail(blockingError, $"\"{blockingError.DisplayName}\" has a blocking error");
                        }
                        return GetResponse<PatcherVM>.Succeed(null!);
                    })
                .ToGuiProperty(this, nameof(LargeOverallError), GetResponse<PatcherVM>.Fail("Uninitialized"));

            _BlockingError = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.LargeOverallError),
                    Patchers.Connect()
                        .AutoRefresh(x => x.IsOn)
                        .Filter(p => p.IsOn)
                        .AutoRefresh(x => x.State)
                        .Transform(p => p.State, transformOnRefresh: true)
                        .QueryWhenChanged(errs =>
                        {
                            var blocking = errs.Cast<ConfigurationState?>().FirstOrDefault<ConfigurationState?>(e => (!e?.RunnableState.Succeeded) ?? false);
                            if (blocking == null) return ErrorResponse.Success;
                            return blocking.RunnableState;
                        }),
                (overall, patchers) =>
                {
                    if (!overall.Succeeded) return overall;
                    return patchers;
                })
                .ToGuiProperty<ErrorResponse>(this, nameof(BlockingError), ErrorResponse.Fail("Uninitialized"));

            _IsActive = this.WhenAnyValue(x => x.Config.SelectedProfile)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsActive));

            GoToErrorPatcher = ReactiveCommand.Create(
                () =>
                {
                    if (LargeOverallError.Value.TryGet(out var patcher))
                    {
                        Config.SelectedPatcher = patcher;
                    }
                },
                canExecute: this.WhenAnyValue(x => x.LargeOverallError.Value).Select(x => x != null));

            UsedNugets = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.MutagenVersioning),
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    parent.MainVM.NewestMutagenVersion,
                    this.WhenAnyValue(x => x.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    parent.MainVM.NewestSynthesisVersion,
                    (mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth) =>
                    {
                        return new SynthesisNugetVersioning(
                            new NugetVersioning(mutaVersioning, mutaManual, newestMuta),
                            new NugetVersioning(synthVersioning, synthManual, newestSynth));
                    })
                .Replay(1)
                .RefCount();

            var nugetTarget = UsedNugets
                .Select(nuget => nuget.TryGetTarget())
                .Replay(1)
                .RefCount();

            _MutagenVersionDiff = Observable.CombineLatest(
                    parent.MainVM.NewestMutagenVersion,
                    nugetTarget.Select(x => x.Value?.MutagenVersion),
                    (matchVersion, selVersion) => (matchVersion, selVersion))
                .ToGuiProperty(this, nameof(MutagenVersionDiff));

            _SynthesisVersionDiff = Observable.CombineLatest(
                    parent.MainVM.NewestMutagenVersion,
                    nugetTarget.Select(x => x.Value?.SynthesisVersion),
                    (matchVersion, selVersion) => (matchVersion, selVersion))
                .ToGuiProperty(this, nameof(SynthesisVersionDiff));
        }

        public ProfileVM(ConfigurationVM parent, SynthesisProfile settings)
            : this(parent, settings.TargetRelease, id: settings.ID)
        {
            Nickname = settings.Nickname;
            MutagenVersioning = settings.MutagenVersioning;
            ManualMutagenVersion = settings.ManualMutagenVersion;
            SynthesisVersioning = settings.SynthesisVersioning;
            ManualSynthesisVersion = settings.ManualSynthesisVersion;
            Patchers.AddRange(settings.Patchers.Select<PatcherSettings, PatcherVM>(p =>
            {
                return p switch
                {
                    GithubPatcherSettings git => new GitPatcherVM(this, git),
                    CodeSnippetPatcherSettings snippet => new CodeSnippetPatcherVM(this, snippet),
                    SolutionPatcherSettings soln => new SolutionPatcherVM(this, soln),
                    CliPatcherSettings cli => new CliPatcherVM(this, cli),
                    _ => throw new NotImplementedException(),
                };
            }));
        }

        public SynthesisProfile Save()
        {
            return new SynthesisProfile()
            {
                Patchers = Patchers.Items.Select(p => p.Save()).ToList(),
                ID = ID,
                Nickname = Nickname,
                TargetRelease = Release,
                ManualMutagenVersion = ManualMutagenVersion,
                ManualSynthesisVersion = ManualSynthesisVersion,
                MutagenVersioning = MutagenVersioning,
                SynthesisVersioning = SynthesisVersioning
            };
        }

        private void SetPatcherForInitialConfiguration(PatcherVM patcher)
        {
            patcher.Profile.Patchers.Add(patcher);
            Config.SelectedPatcher = patcher;
        }

        private void SetInitializer(PatcherInitVM initializer)
        {
            Config.NewPatcher = initializer;
        }
    }
}
