using System.Reactive.Disposables;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views;

public class VersionDisplayBase : NoggogUserControl<MainVm> { }

/// <summary>
/// Interaction logic for VersionDisplay.xaml
/// </summary>
public partial class VersionDisplay : VersionDisplayBase
{
    public VersionDisplay()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyValue(x => x.ViewModel!.SynthesisVersion)
                .BindTo(this, v => v.VersionButton.Content)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.OpenUiVersionPageCommand)
                .BindTo(this, v => v.VersionButton.Command)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.UiUpdateVm.HasUpdate)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, v => v.DownloadUpdateButton.Visibility)
                .DisposeWith(dispose);
            this.OneWayBind(this.ViewModel, vm => vm.OpenUiVersionPageCommand, v => v.DownloadUpdateButton.Command)
                .DisposeWith(dispose);
        });
    }
}