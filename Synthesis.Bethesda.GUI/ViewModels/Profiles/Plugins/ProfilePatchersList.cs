using DynamicData;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;

namespace Synthesis.Bethesda.GUI.Profiles.Plugins
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