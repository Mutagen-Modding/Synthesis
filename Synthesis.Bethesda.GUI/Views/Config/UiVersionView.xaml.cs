using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class UiVersionView
{
    public UiVersionView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.OneWayBind(this.ViewModel, vm => vm.SynthesisVersion, view => view.CurrentVersionText.Text)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.NewestSynthesisVersion)
                .Select(x => x ?? "No update")
                .BindTo(this, v => v.UpdateText.Text)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.GoToUpdatePageCommand, v => v.DownloadUpdateButton.Command)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.GoToWikiCommand, v => v.ReadMoreLink.Command)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.HelpText, v => v.UpdateBlurbText.Text)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.HasUpdate)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, v => v.DownloadUpdateButton.Visibility)
                .DisposeWith(dispose);
        });
    }
}