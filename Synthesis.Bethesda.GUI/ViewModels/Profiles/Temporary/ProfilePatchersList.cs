using DynamicData;

namespace Synthesis.Bethesda.GUI.Temporary
{
    public interface IRemovePatcherFromProfile
    {
        void Remove(PatcherVM patcher);
    }

    public interface IProfilePatchersList
    {
        SourceList<PatcherVM> Patchers { get; }
    }

    public class ProfilePatchersList : IRemovePatcherFromProfile, IProfilePatchersList
    {
        public SourceList<PatcherVM> Patchers { get; } = new();

        public void Remove(PatcherVM patcher)
        {
            Patchers.Remove(patcher);
        }
    }
}