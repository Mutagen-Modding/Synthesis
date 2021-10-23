using System.Windows.Input;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class OpenProfileSettings
    {
        public ICommand OpenCommand { get; }

        public OpenProfileSettings(
            OpenGlobalSettings openGlobalSettings,
            GlobalSettingsVm globalSettingsVm)
        {
            OpenCommand = ReactiveCommand.Create(() =>
            {
                openGlobalSettings.Open();
                globalSettingsVm.SelectedSettings = GlobalSettingsVm.SettingsPages.Profile;
            });
        }
    }
}