using System.Windows.Input;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

public class GlobalSettingsPaneVm : ViewModel
{
    public enum SettingsPages
    {
        Profile,
        UiVersion,
        Advanced,
    };

    public ICommand GoBackCommand { get; }
        
    [Reactive] public SettingsPages SelectedSettings { get; set; }

    private ViewModel? _previous;

    public ProfilesDisplayVm Profiles { get; }
    public GlobalSettingsVm GlobalSettingsVm { get; }
    public UiUpdateVm UiUpdateVm { get; }

    public GlobalSettingsPaneVm(
        ProfilesDisplayVm profilesDisplayVm,
        GlobalSettingsVm globalSettingsVm,
        UiUpdateVm uiUpdateVm,
        IActivePanelControllerVm activePanelController)
    {
        GoBackCommand = ReactiveCommand.Create(() =>
        {
            activePanelController.ActivePanel = _previous;
        });
        Profiles = profilesDisplayVm;
        GlobalSettingsVm = globalSettingsVm;
        UiUpdateVm = uiUpdateVm;
    }

    public void SetPrevious(ViewModel? previous)
    {
        if (previous == this) return;
        _previous = previous;
    }
}