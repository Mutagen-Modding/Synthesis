using System.Reactive.Linq;
using DynamicData;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public interface IAddPatchersToSelectedGroupVm
{
    bool CanAddPatchers { get; }
    void AddNewPatchers(params PatcherVm[] patchersToAdd);
}

public class AddPatchersToSelectedGroupVm : ViewModel, IAddPatchersToSelectedGroupVm
{
    private readonly IProfileGroupsList _groupsList;
    private readonly ISelectedGroupControllerVm _selectedGroupControllerVm;

    private readonly ObservableAsPropertyHelper<bool> _canAddPatchers;
    public bool CanAddPatchers => _canAddPatchers.Value;
    
    public AddPatchersToSelectedGroupVm(
        IProfileGroupsList groupsList,
        ISelectedGroupControllerVm selectedGroupControllerVm)
    {
        _groupsList = groupsList;
        _selectedGroupControllerVm = selectedGroupControllerVm;

        _canAddPatchers = _selectedGroupControllerVm.WhenAnyValue(x => x.SelectedGroup)
            .CombineLatest(groupsList.Groups.Connect().QueryWhenChanged())
            .Select(x => (x.First ?? x.Second.LastOrDefault()) != null)
            .ToGuiProperty(this, nameof(CanAddPatchers));
    }

    public void AddNewPatchers(params PatcherVm[] patchersToAdd)
    {
        var group = _selectedGroupControllerVm.SelectedGroup ?? _groupsList.Groups.Items.LastOrDefault();
        if (group == null)
        {
            throw new ArgumentNullException(
                nameof(ISelectedGroupControllerVm.SelectedGroup),
                "Selected group unexpectedly null");
        }
        patchersToAdd.ForEach(p =>
        {
            p.IsOn = true;
            p.GroupTarget.Group = _selectedGroupControllerVm.SelectedGroup;
        });
        group.Patchers.AddRange(patchersToAdd);
    }
}