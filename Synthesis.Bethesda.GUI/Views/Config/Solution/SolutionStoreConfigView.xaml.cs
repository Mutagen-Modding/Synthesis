using System.Reactive.Disposables;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class SolutionStoreConfigView
{
    public SolutionStoreConfigView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.VersioningOptions)
                .BindTo(this, view => view.PreferredVersioningPicker.ItemsSource)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.VisibilityOptions)
                .BindTo(this, view => view.VisibilityOptionPicker.ItemsSource)
                .DisposeWith(disposable);

            // Bind settings
            this.Bind(this.ViewModel, vm => vm.Settings.LongDescription, view => view.DescriptionBox.Text)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.Settings.ShortDescription, view => view.OneLineDescriptionBox.Text)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.Settings.Visibility, view => view.VisibilityOptionPicker.SelectedItem)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.Settings.Versioning, view => view.PreferredVersioningPicker.SelectedItem)
                .DisposeWith(disposable);
        });
    }
}