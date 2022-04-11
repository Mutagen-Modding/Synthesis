using System.Reactive.Disposables;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;

namespace Synthesis.Bethesda.GUI.Views;

public class AllModsMissingViewBase : NoggogUserControl<AllModsMissingErrorVm> { }

public partial class AllModsMissingView : AllModsMissingViewBase
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