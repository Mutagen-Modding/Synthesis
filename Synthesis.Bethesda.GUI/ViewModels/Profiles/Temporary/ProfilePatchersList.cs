using DynamicData;

namespace Synthesis.Bethesda.GUI.Temporary
{
    public interface IRemovePatcherFromProfile
    {
        void Remove(PatcherVM patcher);
    }

    public class ProfilePatchersList : IRemovePatcherFromProfile
    {
        public SourceList<PatcherVM> Patchers { get; } = new();

        public void Remove(PatcherVM patcher)
        {
            Patchers.Remove(patcher);
        }
    }
}