using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;

namespace Synthesis.Bethesda.GUI.Views;

public class InitializationViewBase : NoggogUserControl<IPatcherInitVm> { }

/// <summary>
/// Interaction logic for InitializationView.xaml
/// </summary>
public partial class InitializationView : InitializationViewBase
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