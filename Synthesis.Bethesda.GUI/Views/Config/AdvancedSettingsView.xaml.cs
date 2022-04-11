using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.Views;

public class AdvancedSettingsViewBase : NoggogUserControl<GlobalSettingsVm> { }
    
public partial class AdvancedSettingsView : AdvancedSettingsViewBase
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
            this.Bind(ViewModel, x => x.DotNetPathOverride, x => x.DotNetPathOverrideBox.Text)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.BuildCores)
                .Select(x => x == 0 ? Environment.ProcessorCount : x)
                .BindTo(this, x => x.ActiveProcessorsText.Text)
                .DisposeWith(dispose);
        });
    }
}