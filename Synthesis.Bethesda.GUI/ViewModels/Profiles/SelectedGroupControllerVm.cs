using System.Linq;
using System.Reactive.Linq;
using DynamicData;
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
        private readonly ObservableAsPropertyHelper<GroupVm?> _selectedGroup;
        public GroupVm? SelectedGroup => _selectedGroup.Value;

        public SelectedGroupControllerVm(
            IProfileGroupsList groupsList,
            IProfileDisplayControllerVm selected)
        {
            _selectedGroup = Observable.CombineLatest(
                    selected.WhenAnyValue(x => x.SelectedObject),
                    groupsList.Groups.Connect()
                        .QueryWhenChanged(q => q),
                    (selected, groups) => selected ?? groups.FirstOrDefault())
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