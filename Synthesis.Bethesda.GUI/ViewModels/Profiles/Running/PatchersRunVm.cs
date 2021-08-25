using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public class PatchersRunVm : ViewModel
    {
        private readonly ILogger _Logger;
        public IRunReporter Reporter { get; }
        private readonly IExecuteRun _executeRun;
        public ConfigurationVm Config { get; }

        public ProfileVm RunningProfile { get; }

        private readonly CancellationTokenSource _cancel = new();

        [Reactive]
        public Exception? ResultError { get; private set; }

        [Reactive]
        public bool Running { get; private set; } = true;

        public SourceCache<PatcherRunVm, Guid> Patchers { get; } = new(p => p.Config.InternalID);
        public IObservableCollection<PatcherRunVm> PatchersDisplay { get; }

        [Reactive]
        public PatcherRunVm? SelectedPatcher { get; set; }

        public ICommand BackCommand { get; }
        public ICommand CancelCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowOverallErrorCommand { get; } = ReactiveCommand.Create(ActionExt.Nothing);

        private readonly ObservableAsPropertyHelper<object?> _DetailDisplay;
        public object? DetailDisplay => _DetailDisplay.Value;

        private PatcherRunVm? _previousPatcher;

        public delegate PatchersRunVm Factory(ConfigurationVm configuration, ProfileVm profile);
        
        public PatchersRunVm(
            ConfigurationVm configuration,
            ILogger logger,
            IPatcherRunnerFactory runnerFactory,
            IActivePanelControllerVm activePanelController,
            IRunReporterWatcher reporterWatcher,
            IRunReporter reporter,
            ProfileVm profile,
            IExecuteRun executeRun)
        {
            _Logger = logger;
            Reporter = reporter;
            _executeRun = executeRun;
            Config = configuration;
            RunningProfile = profile;
            Patchers.AddOrUpdate(RunningProfile.Patchers.Items
                .Where(x => x.IsOn)
                .Select(p => runnerFactory.ToRunner(this, p)));
            PatchersDisplay = Patchers.Connect()
                .ToObservableCollection(this);
            if (profile.SelectedPatcher != null
                && Patchers.TryGetValue(profile.SelectedPatcher.InternalID, out var run))
            {
                SelectedPatcher = run;
            }
            
            BackCommand = ReactiveCommand.Create(() =>
            {
                profile.DisplayController.SelectedObject = SelectedPatcher?.Config;
                activePanelController.ActivePanel = configuration;
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
                    var vm = Patchers.Get(i.data.Key);
                    vm.State = GetResponse<RunState>.Fail(RunState.Error, i.data.Error);
                    SelectedPatcher = vm;
                    logger
                        .ForContext(nameof(IPatcherNameVm.Name), i.data.Run)
                        .Error(i.data.Error, $"Error while {i.type}");
                })
                .DisposeWith(this);
            reporterWatcher.Starting
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = Patchers.Get(i.Key);
                    vm.State = GetResponse<RunState>.Succeed(RunState.Started);
                    logger
                        .ForContext(nameof(IPatcherNameVm.Name), i.Run)
                        .Information($"Starting");

                    // Handle automatic selection advancement
                    if (_previousPatcher == SelectedPatcher
                        && (SelectedPatcher?.AutoScrolling ?? true))
                    {
                        SelectedPatcher = vm;
                    }
                    _previousPatcher = vm;
                })
                .DisposeWith(this);
            reporterWatcher.RunSuccessful
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = Patchers.Get(i.Key);
                    vm.State = GetResponse<RunState>.Succeed(RunState.Finished);
                    logger
                        .ForContext(nameof(IPatcherNameVm.Name), i.Run)
                        .Information("Finished {RunTime}", vm.RunTime);
                })
                .DisposeWith(this);
            reporterWatcher.Output
                .Subscribe(s =>
                {
                    logger
                        .ForContextIfNotNull(nameof(IPatcherNameVm.Name), s.Run)
                        .Information(s.String);
                })
                .DisposeWith(this);
            reporterWatcher.Error
                .Subscribe(s =>
                {
                    logger
                        .ForContextIfNotNull(nameof(IPatcherNameVm.Name), s.Run)
                        .Error(s.String);
                })
                .DisposeWith(this);

            // Clear selected patcher on showing error
            this.ShowOverallErrorCommand.StartingExecution()
                .Subscribe(_ => this.SelectedPatcher = null)
                .DisposeWith(this);

            _DetailDisplay = Observable.Merge(
                    this.WhenAnyValue(x => x.SelectedPatcher)
                        .Select(i => i as object),
                    this.ShowOverallErrorCommand.EndingExecution()
                        .Select(_ => ResultError == null ? null : new ErrorVM("Patching Error", ResultError.ToString())))
                .ToGuiProperty(this, nameof(DetailDisplay), default);
        }

        public async Task Run()
        {
            _Logger.Information("Starting patcher run");
            await Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .DoTask(async (_) =>
                {
                    try
                    {
                        Running = true;
                        var madePatch = await _executeRun.Run(
                            cancel: _cancel.Token,
                            patchers: Patchers.Items.Select(vm => vm.Run).ToArray(),
                            persistenceMode: RunningProfile.SelectedPersistenceMode,
                            persistencePath: Path.Combine(RunningProfile.ProfileDirectory, "Persistence"));
                        if (!madePatch) return;
                        var dataFolderPath = Path.Combine(RunningProfile.DataFolder, Synthesis.Bethesda.Constants.SynthesisModKey.FileName);
                        File.Copy(output, dataFolderPath, overwrite: true);
                        _Logger.Information("Exported patch to: {DataFolderPath}", dataFolderPath);
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
            _Logger.Information("Finished patcher run");
        }

        private async Task Cancel()
        {
            _cancel.Cancel();
            await this.WhenAnyValue(x => x.Running)
                .Where(x => !x)
                .FirstAsync();
            foreach (var p in Patchers.Items)
            {
                if (p.State.Value == RunState.Started)
                {
                    p.State = GetResponse<RunState>.Succeed(RunState.NotStarted);
                }
            }
        }
    }
}
