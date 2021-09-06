using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Groups
{
    public class GroupVm : ViewModel
    {
        public IProfileDisplayControllerVm DisplayController { get; }
        public SourceList<PatcherVm> Patchers { get; } = new();

        public IObservableCollection<PatcherVm> PatchersDisplay { get; }

        [Reactive]
        public bool IsOn { get; set; }

        [Reactive]
        public bool Expanded { get; set; }

        [Reactive]
        public string Name { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<GetResponse<ModKey>> _ModKey;
        public GetResponse<ModKey> ModKey => _ModKey.Value;

        private readonly ObservableAsPropertyHelper<ConfigurationState<ViewModel>> _State;
        public ConfigurationState<ViewModel> State => _State.Value;
        
        public ReactiveCommand<Unit, Unit> UpdateAllPatchersCommand { get; }

        private readonly ObservableAsPropertyHelper<int> _NumEnabledPatchers;
        public int NumEnabledPatchers => _NumEnabledPatchers.Value;

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        public ReactiveCommand<Unit, Unit> GoToErrorCommand { get; }

        public ReactiveCommand<Unit, Unit> RunPatchersCommand { get; }

        public GroupVm(
            ProfileVm profileVm,
            OverallErrorVm overallErrorVm,
            StartRun startRun,
            IProfileDisplayControllerVm selPatcher,
            ILogger logger)
        {
            DisplayController = profileVm.DisplayController;
            _ModKey = this.WhenAnyValue(x => x.Name)
                .Select(x =>
                {
                    if (!string.IsNullOrWhiteSpace(x)
                        && Mutagen.Bethesda.Plugins.ModKey.TryFromName(x, ModType.Plugin, out var modKey))
                    {
                        return modKey;
                    }

                    return GetResponse<ModKey>.Failure;
                })
                .ToGuiProperty(this, nameof(ModKey), GetResponse<ModKey>.Fail(Mutagen.Bethesda.Plugins.ModKey.Null));

            _IsSelected = selPatcher.WhenAnyValue(x => x.SelectedObject)
                .Select(x => x == this)
                // Not GuiProperty, as it interacts with drag/drop oddly
                .ToProperty(this, nameof(IsSelected));
            
            PatchersDisplay = Patchers.Connect()
                .ObserveOnGui()
                .ToObservableCollection(this);

            _NumEnabledPatchers = Patchers.Connect()
                .ObserveOnGui()
                .FilterOnObservable(group => group.WhenAnyValue(x => x.IsOn),
                    scheduler: RxApp.MainThreadScheduler)
                .QueryWhenChanged(q => q)
                .StartWith(Noggog.ListExt.Empty<PatcherVm>())
                .Select(x => x.Count)
                .ToGuiProperty(this, nameof(NumEnabledPatchers));
            
            _State = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.NumEnabledPatchers),
                    Patchers.Connect()
                        .ObserveOnGui()
                        .FilterOnObservable(p => Observable.CombineLatest(
                                p.WhenAnyValue(x => x.IsOn),
                                p.WhenAnyValue(x => x.State.RunnableState.Failed),
                                (on, failed) => on && failed),
                            scheduler: RxApp.MainThreadScheduler)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<PatcherVm>()),
                    (numEnabledPatchers, failedPatchers) =>
                    {
                        if (numEnabledPatchers == 0)
                            return GetResponse<ViewModel>.Fail(this, "There are no enabled patchers to run.");

                        if (failedPatchers.Count > 0)
                        {
                            var errPatcher = failedPatchers.First();
                            var ret = new ConfigurationState<ViewModel>(errPatcher.State.RunnableState.BubbleResult<ViewModel>(errPatcher))
                            {
                                IsHaltingError = errPatcher.State.IsHaltingError
                            };
                            return ret;
                        }

                        return new ConfigurationState<ViewModel>(this);
                    })
                .ToGuiProperty(this, nameof(State), new ConfigurationState<ViewModel>(this));

            GoToErrorCommand = overallErrorVm.CreateCommand(
                this.WhenAnyValue(x => x.State)
                    .Select(x =>
                    {
                        if (x.IsHaltingError) return x.ToGetResponse();
                        return GetResponse<ViewModel>.Succeed(null!);
                    }));
            
            var allCommands = Patchers.Connect()
                .Transform(x => x as GitPatcherVm)
                .ChangeNotNull()
                .Transform(x => CommandVM.Factory(x.UpdateAllCommand.Command))
                .AsObservableList();
            UpdateAllPatchersCommand = ReactiveCommand.CreateFromTask(
                canExecute: allCommands.Connect()
                    .AutoRefresh(x => x.CanExecute)
                    .Filter(p => p.CanExecute)
                    .QueryWhenChanged(q => q.Count > 0),
                execute: () =>
                {
                    return Task.WhenAll(allCommands.Items
                        .Select(async cmd =>
                        {
                            try
                            {
                                if (cmd.CanExecute)
                                {
                                    await cmd.Command.Execute();
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "Error updating a patcher");
                            }
                        }));
                });

            RunPatchersCommand = ReactiveCommand.Create(() =>
                {
                    startRun.Start(this);
                },
                canExecute: this.WhenAnyValue(x => x.State).Select(x => x.RunnableState.Succeeded));
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var patcher in Patchers.Items)
            {
                patcher.Dispose();
            }
        }

        public void Remove(PatcherVm patcher)
        {
            Patchers.Remove(patcher);
        }

        public PatcherGroupSettings Save()
        {
            return new PatcherGroupSettings()
            {
                Name = Name,
                On = IsOn,
                Patchers = Patchers.Items.Select(p => p.Save()).ToList(),
                Expanded = Expanded
            };
        }
    }
}