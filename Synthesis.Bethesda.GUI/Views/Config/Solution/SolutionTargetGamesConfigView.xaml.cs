using System.Reactive.Disposables;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Views;

public class SolutionTargetGamesConfigViewBase : NoggogUserControl<SolutionPatcherVm> { }

public partial class SolutionTargetGamesConfigView : SolutionTargetGamesConfigViewBase
{
    public SolutionTargetGamesConfigView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Settings.TargetedGames)
                .BindTo(this, x => x.CategoryTree.ItemsSource)
                .DisposeWith(disposable);
        });
    }
}