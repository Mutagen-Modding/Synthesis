using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using Noggog.WPF.Containers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Groups;

public class GroupVm : ViewModel, ISelected
{
    public IProfileDisplayControllerVm DisplayController { get; }
    public SourceList<PatcherVm> Patchers { get; } = new();

    public SourceListUiFunnel<PatcherVm> PatchersDisplay { get; }

    [Reactive]
    public bool IsOn { get; set; }

    [Reactive]
    public bool Expanded { get; set; }

    [Reactive]
    public string Name { get; set; } = string.Empty;

    private readonly ObservableAsPropertyHelper<GetResponse<ModKey>> _modKey;
    public GetResponse<ModKey> ModKey => _modKey.Value;

    private readonly ObservableAsPropertyHelper<ConfigurationState<ViewModel>> _state;
    public ConfigurationState<ViewModel> State => _state.Value;
        
    public ReactiveCommand<Unit, Unit> UpdateAllPatchersCommand { get; }

    private readonly ObservableAsPropertyHelper<int> _numEnabledPatchers;
    public int NumEnabledPatchers => _numEnabledPatchers.Value;

    private readonly ObservableAsPropertyHelper<bool> _isSelected;
    public bool IsSelected => _isSelected.Value;

    public ReactiveCommand<Unit, Unit> GoToErrorCommand { get; }

    public ReactiveCommand<Unit, Unit> RunPatchersCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand EnableAllPatchersCommand { get; }

    public ICommand DisableAllPatchersCommand { get; }

    public ProfileVm ProfileVm { get; }

    private readonly ObservableAsPropertyHelper<bool> _patchersProcessing;
    public bool PatchersProcessing => _patchersProcessing.Value;
        
    public ErrorDisplayVm ErrorDisplayVm { get; }
        
    public IObservable<IChangeSet<ModKey>> LoadOrder { get; }

    public ObservableCollection<ModKey> BlacklistedModKeys { get; } = new();

    public GroupVm(
        ProfileVm profileVm,
        OverallErrorVm overallErrorVm,
        StartRun startRun,
        IProfileLoadOrder loadOrder,
        IConfirmationPanelControllerVm confirmation,
        IProfileDisplayControllerVm selPatcher,
        ILogger logger)
    {
        ProfileVm = profileVm;
        LoadOrder = loadOrder.LoadOrder.Connect()
            .Transform(x => x.ModKey);
        DisplayController = profileVm.DisplayController;
        _modKey = this.WhenAnyValue(x => x.Name)
            .Select(x =>
            {
                if (!string.IsNullOrWhiteSpace(x)
                    && Mutagen.Bethesda.Plugins.ModKey.TryFromName(x, ModType.Plugin, out var modKey))
                {
                    return modKey;
                }

                return GetResponse<ModKey>.Failure;
            })
            .ToGuiProperty(this, nameof(ModKey), GetResponse<ModKey>.Fail(Mutagen.Bethesda.Plugins.ModKey.Null), deferSubscription: true);

        _isSelected = selPatcher.WhenAnyValue(x => x.SelectedObject)
            .Select(x => x == this)
            // Not GuiProperty, as it interacts with drag/drop oddly
            .ToProperty(this, nameof(IsSelected));

        PatchersDisplay = new SourceListUiFunnel<PatcherVm>(Patchers, this);

        _numEnabledPatchers = Patchers.Connect()
            .ObserveOnGui()
            .FilterOnObservable(group => group.WhenAnyValue(x => x.IsOn),
                scheduler: RxApp.MainThreadScheduler)
            .QueryWhenChanged(q => q)
            .StartWith(Array.Empty<PatcherVm>())
            .Select(x => x.Count)
            .ToGuiProperty(this, nameof(NumEnabledPatchers), deferSubscription: true);

        var onPatchers = Patchers.Connect()
            .ObserveOnGui()
            .FilterOnObservable(p => p.WhenAnyValue(x => x.IsOn), scheduler: RxApp.MainThreadScheduler)
            .RefCount();

        var processingPatchers = onPatchers
            .FilterOnObservable(p => p.WhenAnyValue(x => x.State)
                .Select(x => x.RunnableState.Failed && !x.IsHaltingError))
            .QueryWhenChanged(q => q)
            .StartWith(Array.Empty<PatcherVm>())
            .Replay(1)
            .RefCount();

        _patchersProcessing = processingPatchers
            .Select(x => x.Count > 0)
            .ToGuiProperty(this, nameof(PatchersProcessing), false, deferSubscription: true);

        _state = Observable.CombineLatest(
                this.WhenAnyValue(x => x.NumEnabledPatchers),
                processingPatchers,
                onPatchers
                    .FilterOnObservable(p => p.WhenAnyValue(x => x.State)
                        .Select(x => x.RunnableState.Failed && x.IsHaltingError))
                    .QueryWhenChanged(q => q)
                    .StartWith(Array.Empty<PatcherVm>()),
                this.WhenAnyValue(x => x.ModKey),
                this.WhenAnyValue(x => x.ProfileVm.GlobalError),
                (numEnabledPatchers, processingPatchers, haltedPatchers, modKey, overallBlocking) =>
                {
                    if (modKey.Failed)
                    {
                        return GetResponse<ViewModel>.Fail(this, "Group does not have a valid output name");
                    }
                        
                    if (numEnabledPatchers == 0)
                    {
                        return GetResponse<ViewModel>.Fail(this, "There are no enabled patchers to run.");
                    }

                    var failedPatcher = haltedPatchers.FirstOrDefault() ?? processingPatchers.FirstOrDefault();
                    if (failedPatcher != null)
                    {
                        return new ConfigurationState<ViewModel>(
                            failedPatcher.State.RunnableState.BubbleResult<ViewModel>(failedPatcher))
                        {
                            IsHaltingError = failedPatcher.State.IsHaltingError
                        };
                    }

                    if (overallBlocking.Failed)
                    {
                        return overallBlocking;
                    }

                    return new ConfigurationState<ViewModel>(this);
                })
            .ToGuiProperty(this, nameof(State), new ConfigurationState<ViewModel>(this), deferSubscription: true);
            
        ErrorDisplayVm = new ErrorDisplayVm(this, this.WhenAnyValue(x => x.State));

        GoToErrorCommand = overallErrorVm.CreateCommand(
            this.WhenAnyValue(x => x.State)
                .Select(x =>
                {
                    if (x.IsHaltingError) return x.ToGetResponse();
                    return GetResponse<ViewModel>.Succeed(null!);
                }));
            
        var allCommands = Patchers.Connect()
            .ObserveOnGui()
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

        DeleteCommand = ReactiveCommand.Create(() =>
        {
            confirmation.TargetConfirmation = new ConfirmationActionVm(
                "Confirm",
                $"Are you sure you want to delete the entire Group {Name}, with {Patchers.Count} patchers?",
                Delete);
        });

        EnableAllPatchersCommand = ReactiveCommand.Create(() =>
        {
            foreach (var patcher in Patchers.Items)
            {
                patcher.IsOn = true;
            }
        });

        DisableAllPatchersCommand = ReactiveCommand.Create(() =>
        {
            foreach (var patcher in Patchers.Items)
            {
                patcher.IsOn = false;
            }
        });
    }

    public void Delete()
    {
        ProfileVm.Groups.Remove(this);
        foreach (var patcher in Patchers.Items)
        {
            patcher.Delete();
        }
        Dispose();
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
            Expanded = Expanded,
            BlacklistedMods = BlacklistedModKeys.ToList(),
        };
    }
}