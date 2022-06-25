using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;

namespace Synthesis.Bethesda.GUI.Views;

public class StoreViewBase : NoggogUserControl<GitPatcherInitVm> { }

/// <summary>
/// Interaction logic for StoreView.xaml
/// </summary>
public partial class StoreView : StoreViewBase
{
    public StoreView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyValue(x => x.ViewModel!.PatcherRepos)
                .BindTo(this, x => x.PatcherReposListBox.ItemsSource)
                .DisposeWith(dispose);
            this.Bind(this.ViewModel, vm => vm.SelectedPatcher, v => v.PatcherReposListBox.SelectedItem)
                .DisposeWith(dispose);
            this.Bind(this.ViewModel, vm => vm.Search, v => v.SearchBox.Text)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.Search)
                .Select(s => string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible)
                .DistinctUntilChanged()
                .BindTo(this, x => x.ClearSearchButton.Visibility)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.ClearSearchCommand)
                .BindTo(this, x => x.ClearSearchButton.Command)
                .DisposeWith(dispose);

            this.WhenAnyValue(x => x.ViewModel!.OpenPopulationInfoCommand)
                .BindTo(this, view => view.SearchHelp.Command)
                .DisposeWith(dispose);

            this.WhenAnyValue(x => x.ViewModel!.SelectedPatcher)
                .BindTo(this, v => v.DetailView.DataContext)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.SelectedPatcher)
                .Select(x => x == null ? Visibility.Hidden : Visibility.Visible)
                .BindTo(this, v => v.DetailView.Visibility)
                .DisposeWith(dispose);

            this.Bind(this.ViewModel, vm => vm.InitializationSettingsVm.ShowUnlisted, v => v.ShowUnlistedCheckbox.IsChecked)
                .DisposeWith(dispose);

            this.Bind(this.ViewModel, vm => vm.InitializationSettingsVm.ShowInstalled, v => v.InstalledCheckbox.IsChecked)
                .DisposeWith(dispose);

        });
    }
}