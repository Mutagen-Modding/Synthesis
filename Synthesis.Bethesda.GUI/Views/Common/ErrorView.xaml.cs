using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for OverallErrorView.xaml
/// </summary>
public partial class ErrorView
{
    public ErrorView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyFallback(x => x.ViewModel!.Title)
                .BindTo(this, x => x.TitleBlock.Text)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.String)
                .BindTo(this, x => x.ErrorOutputBox.Text)
                .DisposeWith(disposable);
        });
    }
}