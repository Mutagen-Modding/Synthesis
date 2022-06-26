using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;

namespace Synthesis.Bethesda.GUI.Views;

public class MissingPluginsFileViewBase : NoggogUserControl<PluginsTxtMissingVm> { }

public partial class MissingPluginsFileView : MissingPluginsFileViewBase
{
    public MissingPluginsFileView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyFallback(x => x.ViewModel!.ErrorString)
                .BindTo(this, x => x.CustomTextBlock.Text)
                .DisposeWith(dispose);
            this.WhenAnyFallback(x => x.ViewModel!.PluginFilePath)
                .Select(x => x.Path)
                .BindTo(this, x => x.PluginPathBlock.Text)
                .DisposeWith(dispose);
        });
    }
}