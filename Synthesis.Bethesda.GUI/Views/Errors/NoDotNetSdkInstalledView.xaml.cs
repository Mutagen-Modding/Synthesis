using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

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
            this.WhenAnyValue(x => x.ViewModel!.TroubleshootCommand)
                .BindTo(this, x => x.TroubleshootButton.Command)
                .DisposeWith(dispose);
        });
    }
}
