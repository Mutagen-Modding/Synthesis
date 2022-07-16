using System.Windows.Input;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

public class OpenGlobalSettings
{
    private readonly GlobalSettingsPaneVm _globalSettingsPaneVm;
    private readonly IActivePanelControllerVm _activePanelControllerVm;
        
    public ICommand OpenProfilesPageCommand { get; }
    public ICommand OpenGlobalSettingsCommand { get; }
    public ICommand OpenUiVersionPageCommand { get; }

    public OpenGlobalSettings(
        GlobalSettingsPaneVm globalSettingsPaneVm,
        IActivePanelControllerVm activePanelControllerVm)
    {
        _globalSettingsPaneVm = globalSettingsPaneVm;
        _activePanelControllerVm = activePanelControllerVm;

        OpenGlobalSettingsCommand = ReactiveCommand.Create(() => Open(GlobalSettingsPaneVm.SettingsPages.Advanced));
        OpenProfilesPageCommand = ReactiveCommand.Create(() => Open(GlobalSettingsPaneVm.SettingsPages.Profile));
        OpenUiVersionPageCommand = ReactiveCommand.Create(() => Open(GlobalSettingsPaneVm.SettingsPages.UiVersion));
    }

    public void Open(GlobalSettingsPaneVm.SettingsPages page)
    {
        _globalSettingsPaneVm.SelectedSettings = page;
        _globalSettingsPaneVm.SetPrevious(_activePanelControllerVm.ActivePanel?.ViewModel);
        _activePanelControllerVm.ActivePanel = _globalSettingsPaneVm;
    }
}