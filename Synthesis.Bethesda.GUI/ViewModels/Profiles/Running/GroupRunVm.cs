using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public class GroupRunVm : ViewModel, IRunItem
    {
        public IGroupRun Run { get; }
        public RunDisplayControllerVm RunDisplayControllerVm { get; }
        public IEnumerable<PatcherRunVm> Patchers { get; }

        [Reactive]
        public bool IsSelected { get; set; }
        
        [Reactive]
        public bool AutoScrolling { get; set; }
        
        public ViewModel SourceVm { get; }

        private readonly ObservableAsPropertyHelper<bool> _HasStarted;
        public bool HasStarted => _HasStarted.Value;

        public GroupRunVm(
            GroupVm groupVm,
            IGroupRun run,
            RunDisplayControllerVm runDisplayControllerVm,
            IEnumerable<PatcherRunVm> runVms)
        {
            SourceVm = groupVm;
            Run = run;
            RunDisplayControllerVm = runDisplayControllerVm;

            Patchers = runVms;

            _HasStarted = runVms.AsObservableChangeSet()
                .FilterOnObservable(x => x.WhenAnyValue(x => x.State.Value)
                        .Select(x => x != RunState.NotStarted))
                .QueryWhenChanged(q => q.Count > 0)
                .ToGuiProperty(this, nameof(HasStarted));
        }
    }
}