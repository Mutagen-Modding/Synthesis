using System.Windows.Input;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class GlobalSettingsPaneVm : ViewModel
    {
        public enum SettingsPages
        {
            General,
            Profile
        };

        public ICommand GoBackCommand { get; }
        
        [Reactive] public SettingsPages SelectedSettings { get; set; }

        private ViewModel? _previous;

        public ProfilesDisplayVm Profiles { get; }
        public GlobalSettingsVm GlobalSettingsVm { get; }

        public GlobalSettingsPaneVm(
            ProfilesDisplayVm profilesDisplayVm,
            GlobalSettingsVm globalSettingsVm,
            IActivePanelControllerVm activePanelController)
        {
            GoBackCommand = ReactiveCommand.Create(() =>
            {
                activePanelController.ActivePanel = _previous;
            });
            Profiles = profilesDisplayVm;
            GlobalSettingsVm = globalSettingsVm;
        }

        public void SetPrevious(ViewModel? previous)
        {
            _previous = previous;
        }
    }
}