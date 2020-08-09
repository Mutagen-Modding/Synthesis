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
    public class RunningPatcherVM : ViewModel
    {
        public IPatcherRun Run { get; }
        public PatcherVM Config { get; }


        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        [Reactive]
        public GetResponse<RunState> State { get; set; } = GetResponse<RunState>.Succeed(RunState.NotStarted);

        public RunningPatcherVM(RunningPatchersVM parent, PatcherVM config, IPatcherRun run)
        {
            Run = run;
            Config = config;

            _IsSelected = parent.WhenAnyValue(x => x.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));
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
