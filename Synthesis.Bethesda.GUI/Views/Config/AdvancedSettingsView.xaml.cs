using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class AdvancedSettingsView
{
    public AdvancedSettingsView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.Bind(ViewModel, x => x.BuildCorePercentage, x => x.ProcessorPercentSlider.Value)
                .DisposeWith(dispose);
            this.Bind(ViewModel, x => x.Shortcircuit, x => x.ShortCircuitBox.IsChecked)
                .DisposeWith(dispose);
            this.Bind(ViewModel, x => x.Mo2Compatibility, x => x.Mo2CompatibilityBox.IsChecked)
                .DisposeWith(dispose);
            this.Bind(ViewModel, x => x.DotNetPathOverride, x => x.DotNetPathOverrideBox.Text)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.BuildCores)
                .Select(x => x == 0 ? Environment.ProcessorCount : x)
                .BindTo(this, x => x.ActiveProcessorsText.Text)
                .DisposeWith(dispose);
            this.Bind(ViewModel, x => x.SpecifyTargetFramework, x => x.SpecifyTargetFrameworkCheck.IsChecked)
                .DisposeWith(dispose);

            // Hide Num Processors panel when MO2 Compatibility is enabled
            this.WhenAnyValue(x => x.ViewModel!.Mo2Compatibility)
                .Select(mo2 => mo2 ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.NumProcessorsPanel.Visibility)
                .DisposeWith(dispose);
        });
    }
}