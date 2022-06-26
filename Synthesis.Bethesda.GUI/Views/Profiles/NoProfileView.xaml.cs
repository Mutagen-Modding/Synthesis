using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views;

public class NoProfileViewBase : NoggogUserControl<NewProfileVm> { }

/// <summary>
/// Interaction logic for NoProfileView.xaml
/// </summary>
public partial class NoProfileView : NoProfileViewBase
{
    public NoProfileView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, x => x.CategoryOptions, x => x.GameCategoryOptionsControl.ItemsSource)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.SelectedCategory, view => view.GameCategoryOptionsControl.SelectedItem)
                .DisposeWith(disposable);
            this.OneWayBind(ViewModel, x => x.ReleaseOptions, x => x.GameReleaseOptionsControl.ItemsSource)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.SelectedRelease, view => view.GameReleaseOptionsControl.SelectedItem)
                .DisposeWith(disposable);
                
            this.WhenAnyValue(x => x.ViewModel!.SelectedCategory)
                .Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.WhichReleaseLabel.Visibility)
                .DisposeWith(disposable);
        });
    }
}