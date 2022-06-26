using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

public interface IProfilePatcherEnumerable
{
    IEnumerable<PatcherVm> Patchers { get; }
}