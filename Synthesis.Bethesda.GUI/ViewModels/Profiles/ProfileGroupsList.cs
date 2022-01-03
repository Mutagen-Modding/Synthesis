using System.Collections.Generic;
using System.Linq;
using DynamicData;
using Noggog.WPF;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IProfileGroupsList
    {
        SourceList<GroupVm> Groups { get; }
    }

    public class ProfileGroupsList : ViewModel, IProfileGroupsList, IProfilePatcherEnumerable
    {
        public SourceList<GroupVm> Groups { get; } = new();
    
        IEnumerable<PatcherVm> IProfilePatcherEnumerable.Patchers => Groups.Items.SelectMany(x => x.Patchers.Items);
    }
}