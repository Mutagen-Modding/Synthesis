using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using System.Windows;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GroupConfigListingViewBase : NoggogUserControl<GroupVm> { }

    /// <summary>
    /// Interaction logic for GroupConfigListingView.xaml
    /// </summary>
    public partial class GroupConfigListingView : GroupConfigListingViewBase
    {
        public GroupConfigListingView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Name)
                    .BindTo(this, x => x.ExportNameBlock.Text)
                    .DisposeWith(disposable);
                this.Bind(this.ViewModel, vm => vm.IsOn, view => view.OnToggle.IsOn)
                    .DisposeWith(disposable);
                this.Bind(this.ViewModel, vm => vm.Expanded, view => view.ExpandStateButton.IsChecked)
                    .DisposeWith(disposable);

                this.WhenAnyFallback(x => x.ViewModel!.PatchersDisplay, fallback: default)
                    .BindTo(this, x => x.PatchersList.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Expanded)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.PatchersList.Visibility)
                    .DisposeWith(disposable);

                this.WhenAnyFallback(x => x.ViewModel!.NumEnabledPatchers, 0)
                    .BindTo(this, x => x.ActiveCountText.Text)
                    .DisposeWith(disposable);
                var EnabledVsTotal = Observable.CombineLatest(
                        this.WhenAnyFallback(x => x.ViewModel!.NumEnabledPatchers, 0),
                        this.WhenAnyFallback(x => x.ViewModel!.PatchersDisplay.Count, 0),
                        (NumEnabled, Total) => (NumEnabled, Total))
                    .Replay(1).RefCount();
                EnabledVsTotal
                    .Select(x => x.Total == x.NumEnabled ? string.Empty : x.Total.ToString())
                    .BindTo(this, x => x.TotalCountText.Text)
                    .DisposeWith(disposable);
                EnabledVsTotal
                    .Select(x => x.Total == x.NumEnabled ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.OutOfSlashText.Visibility)
                    .DisposeWith(disposable);

                this.Bind(this.ViewModel, vm => vm!.DisplayController.SelectedObject, view => view.PatchersList.SelectedValue)
                    .DisposeWith(disposable);

                this.WhenAnyFallback(x => x.ViewModel!.State.RunnableState.Reason)
                    .BindTo(this, x => x.OverallErrorButton.ToolTip)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.GoToErrorCommand)
                    .BindTo(this, x => x.OverallErrorButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.GoToErrorCommand.CanExecute)
                    .Switch()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.OverallErrorButton.Visibility)
                    .DisposeWith(disposable);

                // Set up go button
                this.WhenAnyValue(x => x.ViewModel!.RunPatchersCommand)
                    .BindTo(this, x => x.GoButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.State, new ConfigurationState<ViewModel>(null!))
                    .Select(err => !err.IsHaltingError ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.GoButton.Visibility)
                    .DisposeWith(disposable);

                this.WhenAnyFallback(x => x.ViewModel!.PatchersProcessing)
                    .Select(processing => processing ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.ProcessingCircle.Visibility)
                    .DisposeWith(disposable);

                Drag.ListBoxDragDrop<PatcherVm>(this.PatchersList, onlyWithinSameBox: false)
                    .DisposeWith(disposable);

                // ContextMenu
                this.WhenAnyFallback(x => x.ViewModel!.DeleteCommand)
                    .BindTo(this, x => x.DeleteContextMenuButton.Command)
                    .DisposeWith(disposable);

                // Enable/Disable all buttons
                this.WhenAnyValue(x => x.ViewModel!.EnableAllPatchersCommand)
                    .BindTo(this, x => x.EnableAllPatchersButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DisableAllPatchersCommand)
                    .BindTo(this, x => x.DisableAllPatchersButton.Command)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.Expanded)
                    .Select(x => x ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.PatcherCountPanel.Visibility)
                    .DisposeWith(disposable);
            });
        }
    }
}
