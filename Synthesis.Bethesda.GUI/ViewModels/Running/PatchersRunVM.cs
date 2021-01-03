using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class PatchersRunVM : ViewModel
    {
        public ConfigurationVM Config { get; }

        public ProfileVM RunningProfile { get; }

        private CancellationTokenSource _cancel = new CancellationTokenSource();

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

        private readonly RxReporter<int> _reporter = new RxReporter<int>();

        private readonly ObservableAsPropertyHelper<object?> _DetailDisplay;
        public object? DetailDisplay => _DetailDisplay.Value;

        public PatchersRunVM(ConfigurationVM parent, ProfileVM profile)
        {
            Config = parent;
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
                profile.DisplayedObject = SelectedPatcher?.Config;
                parent.MainVM.ActivePanel = parent;
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
                    Log.Logger.Error(ex, "Error while running patcher pipeline");
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
                    Log.Logger
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
                    Log.Logger
                        .ForContext(nameof(PatcherVM.DisplayName), i.Run.Name)
                        .Information($"Starting");
                })
                .DisposeWith(this);
            _reporter.RunSuccessful
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    var vm = Patchers.Get(i.Key);
                    vm.State = GetResponse<RunState>.Succeed(RunState.Finished);
                    Log.Logger
                        .ForContext(nameof(PatcherVM.DisplayName), i.Run.Name)
                        .Information("Finished {RunTime}", vm.RunTime);
                })
                .DisposeWith(this);
            _reporter.Output
                .Subscribe(s =>
                {
                    Log.Logger
                        .ForContextIfNotNull(nameof(PatcherVM.DisplayName), s.Run?.Name)
                        .Information(s.String);
                })
                .DisposeWith(this);
            _reporter.Error
                .Subscribe(s =>
                {
                    Log.Logger
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
            Log.Logger.Information("Starting patcher run.");
            await Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .DoTask(async (_) =>
                {
                    try
                    {
                        Running = true;
                        var output = Path.Combine(RunningProfile.WorkingDirectory, Synthesis.Bethesda.Constants.SynthesisModKey.FileName);
                        var madePatch = await Runner.Run<int>(
                            workingDirectory: RunningProfile.WorkingDirectory,
                            outputPath: output,
                            dataFolder: RunningProfile.DataFolder,
                            release: RunningProfile.Release,
                            loadOrder: RunningProfile.LoadOrder.Items.Select(x => x.Listing),
                            cancellation: _cancel.Token,
                            reporter: _reporter,
                            patchers: Patchers.Items.Select(vm => (vm.Config.InternalID, vm.Run)));
                        if (!madePatch) return;
                        var dataFolderPath = Path.Combine(RunningProfile.DataFolder, Synthesis.Bethesda.Constants.SynthesisModKey.FileName);
                        File.Copy(output, dataFolderPath, overwrite: true);
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
            Log.Logger.Information("Finished patcher run.");
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
