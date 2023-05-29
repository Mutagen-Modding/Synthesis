using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for NoDotNetSdkInstalledView.xaml
/// </summary>
public partial class NoDotNetSdkInstalledView
{
    public NoDotNetSdkInstalledView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyValue(x => x.ViewModel!.DownloadCommand)
                .BindTo(this, x => x.DownloadButton.Command)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.CustomDisplayString)
                .BindTo(this, x => x.CustomTextBlock.Text)
                .DisposeWith(dispose);
        });
    }
}