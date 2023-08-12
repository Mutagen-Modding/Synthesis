using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for PatcherRunListingView.xaml
/// </summary>
public partial class PatcherRunListingView
{
    public PatcherRunListingView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Name)
                .BindTo(this, x => x.NameBlock.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.IsSelected)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.SelectedGlow.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.RunTimeString)
                .BindTo(this, x => x.RunningTimeBlock.Text)
                .DisposeWith(disposable);
        });
    }
}