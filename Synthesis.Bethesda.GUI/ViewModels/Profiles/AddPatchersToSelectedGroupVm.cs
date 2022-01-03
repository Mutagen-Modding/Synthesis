using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly ISelectedGroupControllerVm _selectedGroupControllerVm;

    private readonly ObservableAsPropertyHelper<bool> _canAddPatchers;
    public bool CanAddPatchers => _canAddPatchers.Value;
    
    public AddPatchersToSelectedGroupVm(
        ISelectedGroupControllerVm selectedGroupControllerVm)
    {
        _selectedGroupControllerVm = selectedGroupControllerVm;

        _canAddPatchers = _selectedGroupControllerVm.WhenAnyValue(x => x.SelectedGroup)
            .Select(x => x != null)
            .ToGuiProperty(this, nameof(CanAddPatchers));
    }

    public void AddNewPatchers(params PatcherVm[] patchersToAdd)
    {
        if (_selectedGroupControllerVm.SelectedGroup == null)
        {
            throw new ArgumentNullException(
                nameof(ISelectedGroupControllerVm.SelectedGroup),
                "Selected group unexpectedly null");
        }
        patchersToAdd.ForEach(p =>
        {
            p.IsOn = true;
            p.Group = _selectedGroupControllerVm.SelectedGroup;
        });
        _selectedGroupControllerVm.SelectedGroup.Patchers.AddRange(patchersToAdd);
    }
}