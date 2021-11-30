using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.GUI.Services.Profile.Running;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public class RunVm : ViewModel
    {
        private readonly ILogger _logger;
        private readonly IExecuteGuiRun _executeRun;
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
            IEnumerable<GroupVm> groups,
            ProfileVm profile)
        {
            _logger = logger;
            _executeRun = executeRun;
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

            reporterWatcher.Overall
                .ObserveOnGui()
                .Subscribe(ex =>
                {
                    logger.Error(ex, "Error while running patcher pipeline");
                    ResultError = ex;
                })
                .DisposeWith(this);
            reporterWatcher.PrepProblem
                .Select(data => (data, type: "prepping"))
                .Merge(reporterWatcher.RunProblem
                    .Select(data => (data, type: "running")))
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = _patchers[i.data.Key];
                    vm.State = GetResponse<RunState>.Fail(RunState.Error, i.data.Error);
                    runDisplayControllerVm.SelectedObject = vm;
                    logger
                        .ForContext(nameof(IPatcherNameVm.Name), i.data.Run)
                        .Error(i.data.Error, $"Error while {i.type}");
                })
                .DisposeWith(this);
            reporterWatcher.Starting
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = _patchers[i.Key];
                    vm.State = GetResponse<RunState>.Succeed(RunState.Started);
                    logger
                        .ForContext(nameof(IPatcherNameVm.Name), i.Run)
                        .Information($"Starting");

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
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = _patchers[i.Key];
                    vm.State = GetResponse<RunState>.Succeed(RunState.Finished);
                    logger
                        .ForContext(nameof(IPatcherNameVm.Name), i.Run)
                        .Information("Finished {RunTime}", vm.RunTime);
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
                .ToGuiProperty(this, nameof(DetailDisplay), default);
        }

        public async Task Run()
        {
            await Observable.Return(Unit.Default)
                .ObserveOnGui()
                .Do(_ => Running = true)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .DoTask(async (_) =>
                {
                    try
                    {
                        await _executeRun.Run(
                            Groups.Select(vm => vm.Run),
                            RunningProfile.SelectedPersistenceMode,
                            _cancel.Token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Reporter.ReportOverallProblem(ex);
                    }
                })
                .ObserveOnGui()
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
}
