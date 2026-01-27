using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using DynamicData;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.GUI.Services.Profile.ErrorClassification;
using Synthesis.Bethesda.GUI.Services.Profile.Running;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

public class RunVm : ViewModel
{
    private readonly ILogger _logger;
    private readonly IExecuteGuiRun _executeRun;
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly IClassificationVmFactory _classificationVmFactory;
    private readonly ILifetimeScope _scope;
    public RunDisplayControllerVm RunDisplayControllerVm { get; }
    public IRunReporter Reporter { get; }
    private readonly Dictionary<Guid, PatcherRunVm> _patchers;

    public ProfileVm RunningProfile { get; }

    private readonly CancellationTokenSource _cancel = new();

    [Reactive]
    public Exception? ResultError { get; private set; }

    [Reactive]
    public bool Running { get; private set; } = true;

    public ObservableCollection<GroupRunVm> Groups { get; } = new();

    public ICommand BackCommand { get; }
    public ICommand CancelCommand { get; }

    public ReactiveCommand<Unit, Unit> ShowOverallErrorCommand { get; } = ReactiveCommand.Create(ActionExt.Nothing);

    private readonly ObservableAsPropertyHelper<object?> _detailDisplay;
    public object? DetailDisplay => _detailDisplay.Value;

    private PatcherRunVm? _previousPatcher;

    public delegate RunVm Factory(IEnumerable<GroupVm> groups);
        
    public RunVm(
        ActiveRunVm activeRunVm,
        ProfileManagerVm profileManager,
        RunDisplayControllerVm runDisplayControllerVm,
        ILogger logger,
        IGroupRunVmFactory runVmFactory,
        IActivePanelControllerVm activePanelController,
        IRunReporterWatcher reporterWatcher,
        IRunReporter reporter,
        IExecuteGuiRun executeRun,
        ISchedulerProvider schedulerProvider,
        IClassificationVmFactory classificationVmFactory,
        ILifetimeScope scope,
        IEnumerable<GroupVm> groups,
        ProfileVm profile)
    {
        _logger = logger;
        _executeRun = executeRun;
        _schedulerProvider = schedulerProvider;
        _classificationVmFactory = classificationVmFactory;
        _scope = scope;
        RunDisplayControllerVm = runDisplayControllerVm;
        Reporter = reporter;
        RunningProfile = profile;
        Groups.Add(groups
            .Select(p => runVmFactory.ToRunner(p, _cancel.Token)));
        _patchers = Groups.SelectMany(x => x.Patchers)
            .ToDictionary(x => x.InternalID, x => x);
        if (profile.SelectedPatcher != null
            && _patchers.TryGetValue(profile.SelectedPatcher.InternalID, out var run))
        {
            runDisplayControllerVm.SelectedObject = run;
        }
            
        BackCommand = ReactiveCommand.Create(() =>
            {
                profile.DisplayController.SelectedObject = runDisplayControllerVm.SelectedObject?.SourceVm;
                activePanelController.ActivePanel = profileManager;
                activeRunVm.CurrentRun = null;
            },
            canExecute: this.WhenAnyValue(x => x.Running)
                .Select(running => !running));
        CancelCommand = ReactiveCommand.CreateFromTask(
            execute: Cancel,
            canExecute: this.WhenAnyValue(x => x.Running));

        reporterWatcher.Output
            .Where(x => x.Run == null)
            .Subscribe(i =>
            {
                logger.Information(i.String);
            })
            .DisposeWith(this);
        reporterWatcher.Error
            .Where(x => x.Run == null)
            .Subscribe(i =>
            {
                logger.Error(i.String);
            })
            .DisposeWith(this);
        reporterWatcher.Exceptions
            .Do(ex => logger.Error(ex, "Error while running patcher pipeline"))
            .ObserveOn(schedulerProvider.MainThread)
            .Subscribe(ex =>
            {
                ResultError = ex;
            })
            .DisposeWith(this);
        reporterWatcher.Exceptions
            .Do(ex => logger.Error(ex, "Error while running patcher pipeline"))
            .ObserveOn(schedulerProvider.MainThread)
            .Subscribe(ex =>
            {
                ResultError = ex;
            })
            .DisposeWith(this);
        reporterWatcher.PrepProblem
            .Select(data => (data: (data.Key, data.Run, Error: (Exception?)data.Error, data.Classification), type: "prepping"))
            .Merge(reporterWatcher.RunProblem
                .Select(data => (data, type: "running")))
            .Do(i =>
            {
                logger
                    .ForContext(nameof(IPatcherNameVm.Name), i.data.Run)
                    .Error(i.data.Error, $"Error while {i.type}: {i.data.Error}");
            })
            .ObserveOn(schedulerProvider.MainThread)
            .Subscribe(i =>
            {
                var vm = _patchers[i.data.Key];
                if (i.data.Error == null)
                {
                    vm.State = GetResponse<RunState>.Fail(RunState.Error);
                }
                else
                {
                    vm.State = GetResponse<RunState>.Fail(RunState.Error, i.data.Error);
                }
                // Set the error classification if present, wrapping it with a VM if needed
                if (i.data.Classification != null)
                {
                    vm.ErrorClassification = _classificationVmFactory.CreateVm(i.data.Classification, _scope);
                }
                runDisplayControllerVm.SelectedObject = vm;
            })
            .DisposeWith(this);
        reporterWatcher.Starting
            .Do(i =>
            {
                logger
                    .ForContext(nameof(IPatcherNameVm.Name), i.Run)
                    .Information($"Starting");
            })
            .ObserveOn(schedulerProvider.MainThread)
            .Subscribe(i =>
            {
                var vm = _patchers[i.Key];
                vm.State = GetResponse<RunState>.Succeed(RunState.Started);

                // Handle automatic selection advancement
                if (_previousPatcher == runDisplayControllerVm.SelectedObject
                    && (runDisplayControllerVm.SelectedObject?.AutoScrolling ?? true))
                {
                    runDisplayControllerVm.SelectedObject = vm;
                }
                _previousPatcher = vm;
            })
            .DisposeWith(this);
        reporterWatcher.RunSuccessful
            .Do(i =>
            {
                var vm = _patchers[i.Key];
                logger
                    .ForContext(nameof(IPatcherNameVm.Name), i.Run)
                    .Information("Finished {RunTime}", vm.RunTime);
            })
            .ObserveOn(schedulerProvider.MainThread)
            .Subscribe(i =>
            {
                var vm = _patchers[i.Key];
                vm.State = GetResponse<RunState>.Succeed(RunState.Finished);
            })
            .DisposeWith(this);

        // Clear selected patcher on showing error
        this.ShowOverallErrorCommand.StartingExecution()
            .Subscribe(_ => runDisplayControllerVm.SelectedObject = null)
            .DisposeWith(this);

        _detailDisplay = Observable.Merge(
                runDisplayControllerVm.WhenAnyValue(x => x.SelectedObject)
                    .Select(i => i as object),
                this.ShowOverallErrorCommand.EndingExecution()
                    .Select(_ => ResultError == null ? null : new ErrorVM("Patching Error", ResultError.ToString())))
            .ToGuiProperty(this, nameof(DetailDisplay), default, schedulerProvider.MainThread, deferSubscription: true);
    }

    public async Task Run()
    {
        await Observable.Return(Unit.Default)
            .ObserveOn(_schedulerProvider.MainThread)
            .Do(_ => Running = true)
            .ObserveOn(_schedulerProvider.TaskPool)
            .DoTask(async (_) =>
            {
                try
                {
                    await _executeRun.Run(
                        groupRuns: Groups.Select(vm => vm.Run),
                        persistenceMode: RunningProfile.SelectedPersistenceMode,
                        localize: RunningProfile.Localize,
                        utf8InEmbeddedStrings: RunningProfile.UseUtf8InEmbedded,
                        headerVersionOverride: RunningProfile.HeaderVersionOverride,
                        formIDRangeMode: RunningProfile.FormIDRangeMode,
                        targetLanguage: RunningProfile.TargetLanguage,
                        masterFile: RunningProfile.MasterFile,
                        masterStyleFallbackEnabled: RunningProfile.MasterStyleFallbackEnabled,
                        masterStyle: RunningProfile.MasterStyle,
                        splitIfMaxMastersExceeded: RunningProfile.SplitIfMaxMastersExceeded,
                        cancel: _cancel.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
                catch (CliUnsuccessfulRunException)
                {
                }
                catch (Exception ex)
                {
                    Reporter.ReportOverallProblem(ex);
                }
            })
            .ObserveOn(_schedulerProvider.MainThread)
            .Do(_ =>
            {
                Running = false;
            });
        _logger.Information("Finished patcher run");
    }

    public async Task Cancel()
    {
        _cancel.Cancel();
        await this.WhenAnyValue(x => x.Running)
            .Where(x => !x)
            .FirstAsync();
        foreach (var p in _patchers.Values)
        {
            if (p.State.Value == RunState.Started)
            {
                p.State = GetResponse<RunState>.Succeed(RunState.NotStarted);
            }
        }
    }
}