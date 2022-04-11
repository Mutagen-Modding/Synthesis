using System;
using System.Collections.Generic;
using DynamicData;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution;

public interface ISolutionPatcherSettingsVm
{
    string LongDescription { get; set; }
    string ShortDescription { get; set; }
    VisibilityOptions Visibility { get; set; }
    PreferredAutoVersioning Versioning { get; set; }
        
    IObservable<IChangeSet<ModKey>> RequiredMods { get; }
    void SetRequiredMods(IEnumerable<ModKey> modKeys);
}