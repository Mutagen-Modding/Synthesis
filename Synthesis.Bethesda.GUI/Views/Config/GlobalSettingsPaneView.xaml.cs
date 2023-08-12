using System.Reactive.Disposables;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for GlobalSettingsPaneView.xaml
/// </summary>
public partial class GlobalSettingsPaneView
{
    public GlobalSettingsPaneView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.OneWayBind(ViewModel, x => x.Profiles, x => x.ProfilesView.DataContext)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.GlobalSettingsVm, x => x.AdvancedSettingsView.DataContext)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.UiUpdateVm, x => x.UiVersionView.DataContext)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.GoBackCommand, x => x.BackButton.Command)
                .DisposeWith(dispose);
            this.Bind(ViewModel, x => x.SelectedSettings, x => x.TabControl.SelectedIndex,
                    x => (int)x,
                    x => (GlobalSettingsPaneVm.SettingsPages)x)
                .DisposeWith(dispose);
        });
    }
}