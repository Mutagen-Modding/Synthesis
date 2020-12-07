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
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Wabbajack.Common;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileVM : ViewModel
    {
        public ConfigurationVM Config { get; }
        public GameRelease Release { get; }

        public SourceList<PatcherVM> Patchers { get; } = new SourceList<PatcherVM>();

        public ICommand AddGitPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }
        public ICommand AddSnippetPatcherCommand { get; }
        public ICommand GoToErrorPatcher { get; }
        public IReactiveCommand UpdateProfileNugetVersionCommand { get; }

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
        public NugetVersioningEnum MutagenVersioning { get; set; } = NugetVersioningEnum.Manual;

        [Reactive]
        public string? ManualMutagenVersion { get; set; }

        [Reactive]
        public NugetVersioningEnum SynthesisVersioning { get; set; } = NugetVersioningEnum.Manual;

        [Reactive]
        public string? ManualSynthesisVersion { get; set; }

        public IObservable<SynthesisNugetVersioning> ActiveVersioning { get; }

        public ICommand SetAllToProfileCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateMutagenManualToLatestCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateSynthesisManualToLatestCommand { get; }

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
                .ToGuiProperty<string>(this, nameof(DataFolder), string.Empty);

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
                .Do(x =>
                {
                    if (x.Failed)
                    {
                        Log.Logger.Warning($"Encountered blocking overall error: {x.Reason}");
                    }
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

            ActiveVersioning = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.MutagenVersioning),
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    parent.MainVM.NewestMutagenVersion,
                    this.WhenAnyValue(x => x.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    parent.MainVM.NewestSynthesisVersion,
                    (mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth) =>
                    {
                        return new SynthesisNugetVersioning(
                            new NugetVersioning("Mutagen", mutaVersioning, mutaManual ?? newestMuta ?? string.Empty, newestMuta),
                            new NugetVersioning("Synthesis", synthVersioning, synthManual ?? newestSynth ?? string.Empty, newestSynth));
                    })
                .Do(x => Log.Logger.Information($"Swapped profile {Nickname} to {x}"))
                .ObserveOnGui()
                .Replay(1)
                .RefCount();

            // Set manual if empty
            parent.MainVM.NewestMutagenVersion
                .Subscribe(x =>
                {
                    if (ManualMutagenVersion == null)
                    {
                        ManualMutagenVersion = x;
                    }
                })
                .DisposeWith(this);
            parent.MainVM.NewestSynthesisVersion
                .Subscribe(x =>
                {
                    if (ManualSynthesisVersion == null)
                    {
                        ManualSynthesisVersion = x;
                    }
                })
                .DisposeWith(this);

            SetAllToProfileCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    foreach (var patcher in Patchers.Items)
                    {
                        if (patcher is GitPatcherVM gitPatcher)
                        {
                            gitPatcher.MutagenVersioning = PatcherNugetVersioningEnum.Profile;
                            gitPatcher.SynthesisVersioning = PatcherNugetVersioningEnum.Profile;
                        }
                    }
                });

            UpdateMutagenManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: parent.MainVM.NewestMutagenVersion
                    .ObserveOnGui(),
                canExecute: v =>
                {
                    return Observable.CombineLatest(
                            this.WhenAnyValue(x => x.MutagenVersioning),
                            this.WhenAnyValue(x => x.ManualMutagenVersion),
                            v,
                            (versioning, manual, latest) =>
                            {
                                if (versioning != NugetVersioningEnum.Manual) return false;
                                return latest != null && latest != manual;
                            })
                        .ObserveOnGui();
                },
                execute: v => ManualMutagenVersion = v ?? string.Empty,
                disposable: this.CompositeDisposable);
            UpdateSynthesisManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: parent.MainVM.NewestSynthesisVersion
                    .ObserveOnGui(),
                canExecute: v =>
                {
                    return Observable.CombineLatest(
                            this.WhenAnyValue(x => x.SynthesisVersioning),
                            this.WhenAnyValue(x => x.ManualSynthesisVersion),
                            v,
                            (versioning, manual, latest) =>
                            {
                                if (versioning != NugetVersioningEnum.Manual) return false;
                                return latest != null && latest != manual;
                            })
                        .ObserveOnGui();
                },
                execute: v => ManualSynthesisVersion = v ?? string.Empty,
                disposable: this.CompositeDisposable);

            UpdateProfileNugetVersionCommand = CommandExt.CreateCombinedAny(
                this.UpdateMutagenManualToLatestCommand, 
                this.UpdateSynthesisManualToLatestCommand);
        }

        public ProfileVM(ConfigurationVM parent, SynthesisProfile settings)
            : this(parent, settings.TargetRelease, id: settings.ID)
        {
            Nickname = settings.Nickname;
            MutagenVersioning = settings.MutagenVersioning;
            ManualMutagenVersion = settings.MutagenManualVersion;
            SynthesisVersioning = settings.SynthesisVersioning;
            ManualSynthesisVersion = settings.SynthesisManualVersion;
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
                MutagenManualVersion = ManualMutagenVersion,
                SynthesisManualVersion = ManualSynthesisVersion,
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
