using System.Windows.Input;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class OpenGlobalSettings
    {
        private readonly GlobalSettingsPaneVm _globalSettingsPaneVm;
        private readonly IActivePanelControllerVm _activePanelControllerVm;
        public ICommand OpenCommand { get; }

        public OpenGlobalSettings(
            GlobalSettingsPaneVm globalSettingsPaneVm,
            IActivePanelControllerVm activePanelControllerVm)
        {
            _globalSettingsPaneVm = globalSettingsPaneVm;
            _activePanelControllerVm = activePanelControllerVm;
            OpenCommand = ReactiveCommand.Create(Open);
        }

        public void Open()
        {
            _globalSettingsPaneVm.SelectedSettings = GlobalSettingsPaneVm.SettingsPages.General;
            _globalSettingsPaneVm.SetPrevious(_activePanelControllerVm.ActivePanel);
            _activePanelControllerVm.ActivePanel = _globalSettingsPaneVm;
        }
    }
}