using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GlobalSettingsPaneViewBase : NoggogUserControl<GlobalSettingsPaneVm> { }

    /// <summary>
    /// Interaction logic for GlobalSettingsPaneView.xaml
    /// </summary>
    public partial class GlobalSettingsPaneView : GlobalSettingsPaneViewBase
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
                this.OneWayBind(ViewModel, x => x.GoBackCommand, x => x.BackButton.Command)
                    .DisposeWith(dispose);
                this.Bind(ViewModel, x => x.SelectedSettings, x => x.TabControl.SelectedIndex,
                        x => (int)x,
                        x => (GlobalSettingsPaneVm.SettingsPages)x)
                    .DisposeWith(dispose);
            });
        }
    }
}
