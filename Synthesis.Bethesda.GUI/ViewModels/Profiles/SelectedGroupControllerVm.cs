using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface ISelectedGroupControllerVm
    {
        GroupVm? SelectedGroup { get; }
    }

    public class SelectedGroupControllerVm : ViewModel, ISelectedGroupControllerVm
    {
        private readonly ObservableAsPropertyHelper<GroupVm?> _SelectedGroup;
        public GroupVm? SelectedGroup => _SelectedGroup.Value;

        public SelectedGroupControllerVm(IProfileDisplayControllerVm selected)
        {
            _SelectedGroup = selected.WhenAnyValue(x => x.SelectedObject)
                .Select(x =>
                {
                    if (x is GroupVm group) return group;
                    if (x is PatcherVm patcher) return patcher.Group;
                    return default(GroupVm?);
                })
                .ToGuiProperty(this, nameof(SelectedGroup), default(GroupVm?));
        }
    }
}