using System.Windows.Input;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class OpenGlobalSettings
    {
        private readonly GlobalSettingsVm _globalSettingsVm;
        private readonly IActivePanelControllerVm _activePanelControllerVm;
        public ICommand OpenCommand { get; }

        public OpenGlobalSettings(
            GlobalSettingsVm globalSettingsVm,
            IActivePanelControllerVm activePanelControllerVm)
        {
            _globalSettingsVm = globalSettingsVm;
            _activePanelControllerVm = activePanelControllerVm;
            OpenCommand = ReactiveCommand.Create(Open);
        }

        public void Open()
        {
            _globalSettingsVm.SetPrevious(_activePanelControllerVm.ActivePanel);
            _activePanelControllerVm.ActivePanel = _globalSettingsVm;
        }
    }
}