using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for MainView.xaml
/// </summary>
public partial class WindowView
{
    public WindowView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.ActivePanel)
                .BindTo(this, x => x.ContentPane.Content)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.ActiveConfirmation)
                .Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.ConfirmationOverlay.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.InitialLoading)
                .ObserveOnGui()
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.InitialLoading.Visibility)
                .DisposeWith(disposable);
        });
    }
}