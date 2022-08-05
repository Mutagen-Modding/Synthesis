using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Views;

public class SolutionConfigViewBase : NoggogUserControl<SolutionPatcherVm> { }

/// <summary>
/// Interaction logic for SolutionConfigView.xaml
/// </summary>
public partial class SolutionConfigView : SolutionConfigViewBase
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