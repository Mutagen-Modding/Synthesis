using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class PatcherRunVM : ViewModel
    {
        public IPatcherRun Run { get; }
        public PatcherVM Config { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        [Reactive]
        public GetResponse<RunState> State { get; set; } = GetResponse<RunState>.Succeed(RunState.NotStarted);

        public IObservableCollection<string> OutputLineDisplay { get; }

        private readonly ObservableAsPropertyHelper<string> _RunTime;
        public string RunTime => _RunTime.Value;

        private readonly ObservableAsPropertyHelper<bool> _IsRunning;
        public bool IsRunning => _IsRunning.Value;

        private readonly ObservableAsPropertyHelper<bool> _IsErrored;
        public bool IsErrored => _IsErrored.Value;

        public PatcherRunVM(PatchersRunVM parent, PatcherVM config, IPatcherRun run)
        {
            Run = run;
            Config = config;

            _IsSelected = parent.WhenAnyValue(x => x.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            OutputLineDisplay = Observable.Merge(
                    run.Output,
                    run.Error,
                    this.WhenAnyValue(x => x.State)
                        .Where(x => x.Value == RunState.Error)
                        .Select(x => x.Reason))
                .ToObservableChangeSet()
                .Buffer(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .Where(l => l.Count > 0)
                .FlattenBufferResult()
                .ToObservableCollection(this);

            _IsRunning = this.WhenAnyValue(x => x.State)
                .Select(x => x.Value == RunState.Started)
                .ToGuiProperty(this, nameof(IsRunning));

            _IsErrored = this.WhenAnyValue(x => x.State)
                .Select(x => x.Value == RunState.Error)
                .ToGuiProperty(this, nameof(IsErrored));

            _RunTime = Noggog.ObservableExt.TimePassed(TimeSpan.FromMilliseconds(100), RxApp.MainThreadScheduler)
                .FilterSwitch(this.WhenAnyValue(x => x.IsRunning))
                .Select(time =>
                {
                    if (time.TotalDays > 1)
                    {
                        return $"{time.TotalDays:n1}d";
                    }
                    if (time.TotalHours > 1)
                    {
                        return $"{time.TotalHours:n1}h";
                    }
                    if (time.TotalMinutes > 1)
                    {
                        return $"{time.TotalMinutes:n1}m";
                    }
                    return $"{time.TotalSeconds:n1}s";
                })
                .ToGuiProperty<string>(this, nameof(RunTime), string.Empty);
        }
    }

    public enum RunState
    {
        NotStarted,
        Started,
        Finished,
        Error,
    }
}
