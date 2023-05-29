using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for NoProfileView.xaml
/// </summary>
public partial class NoProfileView
{
    public NoProfileView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, x => x.CategoryOptions, x => x.ReleasePickerView.GameCategoryOptionsControl.ItemsSource)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.SelectedCategory, view => view.ReleasePickerView.GameCategoryOptionsControl.SelectedItem)
                .DisposeWith(disposable);

            this.WhenAnyFallback(x => x.ViewModel!.ReleaseOptions, fallback: default)
                .BindTo(this, x => x.ReleasePickerView.GameReleaseOptionsControl.ItemsSource)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.SelectedRelease, view => view.ReleasePickerView.GameReleaseOptionsControl.SelectedItem)
                .DisposeWith(disposable);
        });
    }
}