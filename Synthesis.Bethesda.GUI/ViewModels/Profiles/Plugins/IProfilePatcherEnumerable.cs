using System.Collections.Generic;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins
{
    public interface IProfilePatcherEnumerable
    {
        IEnumerable<PatcherVm> Patchers { get; }
    }

    public class ProfilePatcherEnumerable : IProfilePatcherEnumerable
    {
        private readonly IProfilePatchersList _patchersList;
        public IEnumerable<PatcherVm> Patchers => _patchersList.Patchers.Items;

        public ProfilePatcherEnumerable(IProfilePatchersList patchersList)
        {
            _patchersList = patchersList;
        }
    }
}