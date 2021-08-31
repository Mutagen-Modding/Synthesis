using System.Collections.ObjectModel;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public class GroupRunVm : IRunItem
    {
        public IGroupRun Run { get; }
        public RunDisplayControllerVm RunDisplayControllerVm { get; }
        public ObservableCollection<PatcherRunVm> Patchers { get; } = new();

        [Reactive]
        public bool IsSelected { get; set; }
        
        [Reactive]
        public bool AutoScrolling { get; set; }
        
        public ViewModel SourceVm { get; }

        public GroupRunVm(
            GroupVm groupVm,
            IGroupRun run,
            RunDisplayControllerVm runDisplayControllerVm)
        {
            SourceVm = groupVm;
            Run = run;
            RunDisplayControllerVm = runDisplayControllerVm;
        }
    }
}