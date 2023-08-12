using System.Reactive.Disposables;
using ReactiveUI;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for VersionDisplay.xaml
/// </summary>
public partial class VersionDisplay
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