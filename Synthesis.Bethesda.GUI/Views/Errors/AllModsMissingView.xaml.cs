using System.Reactive.Disposables;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class AllModsMissingView
{
    public AllModsMissingView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyFallback(x => x.ViewModel!.ErrorString)
                .BindTo(this, x => x.CustomTextBlock.Text)
                .DisposeWith(dispose);
            this.WhenAnyFallback(x => x.ViewModel!.DataFolderPath)
                .BindTo(this, x => x.PluginPathBlock.Text)
                .DisposeWith(dispose);
            this.WhenAnyFallback(x => x.ViewModel!.GoToProfileSettingsCommand)
                .BindTo(this, x => x.ProfileSettingsCommandLink.Command)
                .DisposeWith(dispose);
        });
    }
}