using System.Windows.Input;
using ReactiveUI;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public class OpenProfileSettings
    {
        public ICommand OpenCommand { get; }

        public OpenProfileSettings(
            ConfigurationVm configuration,
            IProfileFactory profileFactory,
            IActivePanelControllerVm activePanelControllerVm)
        {
            OpenCommand = ReactiveCommand.Create(() =>
            {
                activePanelControllerVm.ActivePanel =
                    new ProfilesDisplayVm(configuration, profileFactory, activePanelControllerVm);
            });
        }
    }
}