using System.Windows.Input;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class OpenProfileSettings
    {
        public ICommand OpenCommand { get; }

        public OpenProfileSettings(
            OpenGlobalSettings openGlobalSettings,
            GlobalSettingsPaneVm globalSettingsPaneVm)
        {
            OpenCommand = ReactiveCommand.Create(() =>
            {
                openGlobalSettings.Open();
                globalSettingsPaneVm.SelectedSettings = GlobalSettingsPaneVm.SettingsPages.Profile;
            });
        }
    }
}