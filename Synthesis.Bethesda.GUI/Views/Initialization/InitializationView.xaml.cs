using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for InitializationView.xaml
/// </summary>
public partial class InitializationView
{
    public InitializationView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .BindTo(this, x => x.ConfigDetailPane.Content)
                .DisposeWith(disposable);
        });
    }
}