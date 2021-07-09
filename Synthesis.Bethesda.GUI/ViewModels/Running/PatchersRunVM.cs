using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Serilog;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;

namespace Synthesis.Bethesda.GUI
{
    public class PatchersRunVM : ViewModel
    {
        private readonly ILogger _Logger;
        private readonly IRunner _Runner;
        public ConfigurationVM Config { get; }

        public ProfileVM RunningProfile { get; }

        private readonly CancellationTokenSource _cancel = new();

        [Reactive]
        public Exception? ResultError { get; private set; }

        [Reactive]
        public bool Running { get; private set; } = true;

        public SourceCache<PatcherRunVM, int> Patchers { get; } = new SourceCache<PatcherRunVM, int>(p => p.Config.InternalID);
        public IObservableCollection<PatcherRunVM> PatchersDisplay { get; }

        [Reactive]
        public PatcherRunVM? SelectedPatcher { get; set; }

        public ICommand BackCommand { get; }
        public ICommand CancelCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowOverallErrorCommand { get; } = ReactiveCommand.Create(ActionExt.Nothing);

        private readonly RxReporter<int> _reporter = new();

        private readonly ObservableAsPropertyHelper<object?> _DetailDisplay;
        public object? DetailDisplay => _DetailDisplay.Value;

        private PatcherRunVM? _previousPatcher;

        public delegate PatchersRunVM Factory(ConfigurationVM configuration, ProfileVM profile, ILogger logger);
        
        public PatchersRunVM(
            ConfigurationVM configuration,
            ILogger logger,
            IActivePanelControllerVm activePanelController,
            ProfileVM profile,
            IRunner runner)
        {
            _Logger = logger;
            _Runner = runner;
            Config = configuration;
            RunningProfile = profile;
            Patchers.AddOrUpdate(RunningProfile.Patchers.Items
                .Where(x => x.IsOn)
                .Select(p => p.ToRunner(this)));
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

            _reporter.Overall
                .ObserveOnGui()
                .Subscribe(ex =>
                {
                    logger.Error(ex, "Error while running patcher pipeline");
                    ResultError = ex;
                })
                .DisposeWith(this);
            _reporter.PrepProblem
                .Select(data => (data, type: "prepping"))
                .Merge(_reporter.RunProblem
                    .Select(data => (data, type: "running")))
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = Patchers.Get(i.data.Key);
                    vm.State = GetResponse<RunState>.Fail(RunState.Error, i.data.Error);
                    SelectedPatcher = vm;
                    logger
                        .ForContext(nameof(PatcherVM.DisplayName), i.data.Run.Name)
                        .Error(i.data.Error, $"Error while {i.type}");
                })
                .DisposeWith(this);
            _reporter.Starting
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = Patchers.Get(i.Key);
                    vm.State = GetResponse<RunState>.Succeed(RunState.Started);
                    logger
                        .ForContext(nameof(PatcherVM.DisplayName), i.Run.Name)
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
            _reporter.RunSuccessful
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = Patchers.Get(i.Key);
                    vm.State = GetResponse<RunState>.Succeed(RunState.Finished);
                    logger
                        .ForContext(nameof(PatcherVM.DisplayName), i.Run.Name)
                        .Information("Finished {RunTime}", vm.RunTime);
                })
                .DisposeWith(this);
            _reporter.Output
                .Subscribe(s =>
                {
                    logger
                        .ForContextIfNotNull(nameof(PatcherVM.DisplayName), s.Run?.Name)
                        .Information(s.String);
                })
                .DisposeWith(this);
            _reporter.Error
                .Subscribe(s =>
                {
                    logger
                        .ForContextIfNotNull(nameof(PatcherVM.DisplayName), s.Run?.Name)
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
            _Logger.Information("Starting patcher run.");
            await Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .DoTask(async (_) =>
                {
                    try
                    {
                        Running = true;
                        var output = Path.Combine(RunningProfile.WorkingDirectory, Synthesis.Bethesda.Constants.SynthesisModKey.FileName);
                        var madePatch = await _Runner.Run<int>(
                            workingDirectory: RunningProfile.WorkingDirectory,
                            outputPath: output,
                            cancellation: _cancel.Token,
                            reporter: _reporter,
                            patchers: Patchers.Items.Select(vm => (vm.Config.InternalID, vm.Run)),
                            persistenceMode: RunningProfile.SelectedPersistenceMode,
                            persistencePath: Path.Combine(RunningProfile.ProfileDirectory, "Persistence"));
                        if (!madePatch) return;
                        var dataFolderPath = Path.Combine(RunningProfile.DataFolder, Synthesis.Bethesda.Constants.SynthesisModKey.FileName);
                        File.Copy(output, dataFolderPath, overwrite: true);
                        _Logger.Information($"Exported patch to: {dataFolderPath}");
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _reporter.ReportOverallProblem(ex);
                    }
                })
                .ObserveOnGui()
                .Do(_ =>
                {
                    Running = false;
                });
            _Logger.Information("Finished patcher run.");
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
