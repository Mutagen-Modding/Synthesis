using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IProfileNameVm
    {
        string Name { get; set; }
    }

    public class ProfileNameVm : IProfileNameProvider, IProfileNameVm
    {
        [Reactive]
        public string Name { get; set; }

        public ProfileNameVm(ISynthesisProfileSettings settings)
        {
            Name = settings.Nickname;
        }
    }
}