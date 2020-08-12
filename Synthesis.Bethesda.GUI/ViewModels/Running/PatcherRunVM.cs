using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
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
                .ToObservableCollection(this);
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
