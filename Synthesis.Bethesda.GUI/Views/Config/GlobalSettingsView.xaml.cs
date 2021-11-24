using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GlobalSettingsViewBase : NoggogUserControl<GlobalSettingsVm> { }

    /// <summary>
    /// Interaction logic for GlobalSettingsView.xaml
    /// </summary>
    public partial class GlobalSettingsView : GlobalSettingsViewBase
    {
        public GlobalSettingsView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.OneWayBind(ViewModel, x => x.Profiles, x => x.ProfilesView.DataContext)
                    .DisposeWith(dispose);
                this.OneWayBind(ViewModel, x => x.GoBackCommand, x => x.BackButton.Command)
                    .DisposeWith(dispose);
                this.Bind(ViewModel, x => x.SelectedSettings, x => x.TabControl.SelectedIndex,
                        x => (int)x,
                        x => (GlobalSettingsVm.SettingsPages)x)
                    .DisposeWith(dispose);
                this.Bind(ViewModel, x => x.BuildCorePercentage, x => x.ProcessorPercentSlider.Value)
                    .DisposeWith(dispose);
                this.Bind(ViewModel, x => x.ShortcircuitBuilds, x => x.ShortCircuitBuildsBox.IsChecked)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.BuildCores)
                    .Select(x => x == 0 ? Environment.ProcessorCount : x)
                    .BindTo(this, x => x.ActiveProcessorsText.Text)
                    .DisposeWith(dispose);
            });
        }
    }
}
