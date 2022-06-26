using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;

namespace Synthesis.Bethesda.GUI.Views;

public class NoDotNetSdkInstalledViewBase : NoggogUserControl<DotNetNotInstalledVm> { }

/// <summary>
/// Interaction logic for NoDotNetSdkInstalledView.xaml
/// </summary>
public partial class NoDotNetSdkInstalledView : NoDotNetSdkInstalledViewBase
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