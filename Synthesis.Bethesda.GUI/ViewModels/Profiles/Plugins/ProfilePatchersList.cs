using DynamicData;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins
{
    public interface IRemovePatcherFromProfile
    {
        void Remove(PatcherVm patcher);
    }

    public interface IProfilePatchersList
    {
        SourceList<PatcherVm> Patchers { get; }
    }

    public class ProfilePatchersList : IRemovePatcherFromProfile, IProfilePatchersList
    {
        public SourceList<PatcherVm> Patchers { get; } = new();

        public void Remove(PatcherVm patcher)
        {
            Patchers.Remove(patcher);
        }
    }
}