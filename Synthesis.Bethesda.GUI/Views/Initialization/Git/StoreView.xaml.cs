using Noggog.WPF;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class StoreViewBase : NoggogUserControl<GitPatcherInitVM> { }

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
                this.WhenAnyValue(x => x.ViewModel.PatcherRepos)
                    .BindToStrict(this, x => x.PatcherReposListBox.ItemsSource)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.SelectedPatcher, v => v.PatcherReposListBox.SelectedItem)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.Search, v => v.SearchBox.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel.Search)
                    .Select(s => string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible)
                    .DistinctUntilChanged()
                    .BindToStrict(this, x => x.ClearSearchButton.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel.ClearSearchCommand)
                    .BindToStrict(this, x => x.ClearSearchButton.Command)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel.OpenPopulationInfoCommand)
                    .BindToStrict(this, view => view.SearchHelp.Command)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel.SelectedPatcher)
                    .BindToStrict(this, v => v.DetailView.DataContext)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel.SelectedPatcher)
                    .Select(x => x == null ? Visibility.Hidden : Visibility.Visible)
                    .BindToStrict(this, v => v.DetailView.Visibility)
                    .DisposeWith(dispose);

                this.BindStrict(this.ViewModel, vm => vm.ShowAll, v => v.ShowAllCheckbox.IsChecked)
                    .DisposeWith(dispose);

            });
        }
    }
}
