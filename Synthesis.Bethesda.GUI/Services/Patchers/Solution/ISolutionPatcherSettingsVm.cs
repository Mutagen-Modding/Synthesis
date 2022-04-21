using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicData;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution;

public interface ISolutionPatcherSettingsVm
{
    string LongDescription { get; set; }
    string ShortDescription { get; set; }
    VisibilityOptions Visibility { get; set; }
    PreferredAutoVersioning Versioning { get; set; }
    ObservableCollection<ModKey> RequiredMods { get; }
    void SetRequiredMods(IEnumerable<ModKey> modKeys);
}

public class SolutionPatcherSettingsVm : ViewModel, ISolutionPatcherSettingsVm
{
    [Reactive]
    public string ShortDescription { get; set; } = string.Empty;

    [Reactive]
    public string LongDescription { get; set; } = string.Empty;

    [Reactive]
    public VisibilityOptions Visibility { get; set; } = DTO.VisibilityOptions.Visible;

    [Reactive]
    public PreferredAutoVersioning Versioning { get; set; }

    public ObservableCollection<ModKey> RequiredMods { get; } = new();

    public void SetRequiredMods(IEnumerable<ModKey> modKeys)
    {
        RequiredMods.SetTo(modKeys);
    }
}