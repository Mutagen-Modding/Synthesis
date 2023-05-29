using System.Reactive.Disposables;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class SolutionTargetGamesConfigView
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