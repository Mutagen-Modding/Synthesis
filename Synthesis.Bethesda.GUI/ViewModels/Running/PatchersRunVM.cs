using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Runner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
            if (parent.SelectedPatcher != null
                && Patchers.TryGetValue(parent.SelectedPatcher.InternalID, out var run))
            {
                SelectedPatcher = run;
            }

            BackCommand = ReactiveCommand.Create(() =>
            {
                parent.SelectedPatcher = SelectedPatcher?.Config;
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
                    ResultError = ex;
                })
                .DisposeWith(this);
            _reporter.PrepProblem
                .Merge(_reporter.RunProblem)
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    if (Patchers.TryGetValue(i.Key, out var vm))
                    {
                        vm.State = GetResponse<RunState>.Fail(RunState.Error, i.Error);
                        SelectedPatcher = vm;
                    }
                })
                .DisposeWith(this);
            _reporter.Starting
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    if (Patchers.TryGetValue(i.Key, out var vm))
                    {
                        vm.State = GetResponse<RunState>.Succeed(RunState.Started);
                    }
                })
                .DisposeWith(this);
            _reporter.RunSuccessful
                .ObserveOnGui()
                .Subscribe(i =>
                {
                    if (Patchers.TryGetValue(i.Key, out var vm))
                    {
                        vm.State = GetResponse<RunState>.Succeed(RunState.Finished);
                    }
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
                        .Select(_ => ResultError == null ? null : new OverallErrorVM(ResultError)))
                .ToGuiProperty(this, nameof(DetailDisplay));
        }

        public async Task Run()
        {
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
                            loadOrder: RunningProfile.LoadOrder.Items,
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
