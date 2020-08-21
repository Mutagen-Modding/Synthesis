using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Wabbajack.Common;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileVM : ViewModel
    {
        public ConfigurationVM Config { get; }
        public GameRelease Release { get; }

        public SourceList<PatcherVM> Patchers { get; } = new SourceList<PatcherVM>();

        public ICommand AddGithubPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }
        public ICommand AddSnippetPatcherCommand { get; }

        public string ID { get; private set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<string> _WorkingDirectory;
        public string WorkingDirectory => _WorkingDirectory.Value;

        private readonly ObservableAsPropertyHelper<string> _DataFolder;
        public string DataFolder => _DataFolder.Value;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _BlockingError;
        public ErrorResponse BlockingError => _BlockingError.Value;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _LargeOverallError;
        public ErrorResponse LargeOverallError => _LargeOverallError.Value;

        public IObservableList<LoadOrderListing> LoadOrder { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsActive;
        public bool IsActive => _IsActive.Value;

        public ProfileVM(ConfigurationVM parent, GameRelease? release = null, string? id = null)
        {
            ID = id ?? Guid.NewGuid().ToString();
            Config = parent;
            Release = release ?? GameRelease.Oblivion;
            AddGithubPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new GithubPatcherVM(this)));
            AddSolutionPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new SolutionPatcherVM(this)));
            AddCliPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new CliPatcherVM(this)));
            AddSnippetPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new CodeSnippetPatcherVM(this)));
            _WorkingDirectory = this.WhenAnyValue(x => x.Config.WorkingDirectory)
                .Select(dir => Path.Combine(dir, ID, "Workspace"))
                .ToGuiProperty<string>(this, nameof(WorkingDirectory));

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
                    (Release, DataFolder) => (Release, DataFolder))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    try
                    {
                        if (x.DataFolder.Failed) return x.DataFolder.Bubble<IEnumerable<LoadOrderListing>>(_ => Enumerable.Empty<LoadOrderListing>());
                        var lo = Mutagen.Bethesda.LoadOrder.GetUsualLoadOrder(x.Release, x.DataFolder.Value, throwOnMissingMods: true);
                        return GetResponse<IEnumerable<LoadOrderListing>>.Succeed(lo);
                    }
                    catch (MissingModException ex)
                    {
                        return GetResponse<IEnumerable<LoadOrderListing>>.Fail(
                            Enumerable.Empty<LoadOrderListing>(),
                            $"Mod on the load order was missing from the data folder: {ex.ModPath}");
                    }
                    catch (FileNotFoundException)
                    {
                        return GetResponse<IEnumerable<LoadOrderListing>>.Fail(
                            Enumerable.Empty<LoadOrderListing>(),
                            $"Could not locate load order for target game.");
                    }
                    catch (Exception ex)
                    {
                        return GetResponse<IEnumerable<LoadOrderListing>>.Fail(
                            Enumerable.Empty<LoadOrderListing>(),
                            ex);
                    }
                })
                .Replay(1)
                .RefCount();

            LoadOrder = loadOrderResult
                .Select(x => x.Value.AsObservableChangeSet())
                .Switch()
                .AsObservableList();

            _LargeOverallError = Observable.CombineLatest(
                    dataFolderResult,
                    loadOrderResult,
                    Patchers.Connect()
                        .AutoRefresh(x => x.IsOn)
                        .Filter(p => p.IsOn)
                        .AutoRefresh(x => x.State)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<PatcherVM>()),
                    (dataFolder, loadOrder, coll) =>
                    {
                        if (coll.Count == 0) return (ErrorResponse)ErrorResponse.Fail("There are no enabled patchers to run.");
                        if (!dataFolder.Succeeded) return dataFolder;
                        if (!loadOrder.Succeeded) return loadOrder;
                        var blockingError = coll.FirstOrDefault(p => p.State.IsHaltingError);
                        if (blockingError != null)
                        {
                            return ErrorResponse.Fail($"\"{blockingError.Nickname}\" has a blocking error");
                        }
                        return ErrorResponse.Success;
                    })
                .ToGuiProperty<ErrorResponse>(this, nameof(LargeOverallError), ErrorResponse.Fail("Uninitialized"));

            _BlockingError = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.LargeOverallError),
                    Patchers.Connect()
                        .AutoRefresh(x => x.IsOn)
                        .Filter(p => p.IsOn)
                        .AutoRefresh(x => x.State)
                        .Transform(p => p.State, transformOnRefresh: true)
                        .QueryWhenChanged(errs =>
                        {
                            var blocking = errs.Cast<ConfigurationStateVM?>().FirstOrDefault<ConfigurationStateVM?>(e => (!e?.RunnableState.Succeeded) ?? false);
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
        }

        public ProfileVM(ConfigurationVM parent, SynthesisProfile settings)
            : this(parent, settings.TargetRelease, id: settings.ID)
        {
            Nickname = settings.Nickname;
            Patchers.AddRange(settings.Patchers.Select<PatcherSettings, PatcherVM>(p =>
            {
                return p switch
                {
                    GithubPatcherSettings gitHub => new GithubPatcherVM(this, gitHub),
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
            };
        }

        private void SetPatcherForInitialConfiguration(PatcherVM patcher)
        {
            var initializer = patcher.CreateInitializer();
            if (initializer != null)
            {
                Config.NewPatcher = initializer;
            }
            else
            {
                patcher.Profile.Patchers.Add(patcher);
                Config.SelectedPatcher = patcher;
            }
        }
    }
}
