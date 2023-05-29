using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for SolutionConfigView.xaml
/// </summary>
public partial class SolutionConfigView
{
    public SolutionConfigView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Settings.RequiredMods)
                .BindTo(this, x => x.RequiredMods.ModKeys)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.DetectedMods)
                .BindTo(this, x => x.RequiredMods.SearchableMods)
                .DisposeWith(disposable);
        });
    }
}