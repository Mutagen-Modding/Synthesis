using Noggog.WPF;
using Noggog;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using System.Reactive.Linq;
using System.Windows.Input;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatchersConfigViewBase : NoggogUserControl<ConfigurationVm> { }

    /// <summary>
    /// Interaction logic for PatchersConfigurationView.xaml
    /// </summary>
    public partial class PatchersConfigView : PatchersConfigViewBase
    {
        public PatchersConfigView()
        {
            InitializeComponent();
            this.WhenActivated((disposable) =>
            {
                this.WhenAnyValue(x => x.ViewModel!.GroupsDisplay)
                    .BindTo(this, x => x.GroupsList.ItemsSource)
                    .DisposeWith(disposable);

                this.Bind(this.ViewModel, vm => vm.SelectedProfile!.DisplayController.SelectedObject, view => view.GroupsList.SelectedValue)
                    .DisposeWith(disposable);

                // Wire up patcher config data context and visibility
                this.WhenAnyValue(x => x.ViewModel!.DisplayedObject)
                    .BindTo(this, x => x.DetailControl.Content)
                    .DisposeWith(disposable);

                // Only show help if zero groups
                this.WhenAnyValue(x => x.ViewModel!.GroupsDisplay.Count)
                    .Select(c => c == 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.AddSomePatchersHelpGrid.Visibility)
                    .DisposeWith(disposable);

                // Show dimmer if in initial configuration
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.Init.NewPatcher)
                    .Select(newPatcher => newPatcher != null ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.InitialConfigurationDimmer.Visibility)
                    .DisposeWith(disposable);

                // Set up go button
                this.WhenAnyValue(x => x.ViewModel!.RunPatchers)
                    .BindTo(this, x => x.GoButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RunPatchers.CanExecute)
                    .Switch()
                    .Select(can => can ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.GoButton.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RunPatchers.CanExecute)
                    .Switch()
                    .CombineLatest(this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.BlockingError, GetResponse<ViewModel>.Succeed(null!)),
                        (can, overall) => !can && overall.Succeeded)
                    .Select(show => show ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.ProcessingRingAnimation.Visibility)
                    .DisposeWith(disposable);

                // Set up large overall error button
                var overallErr = this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.BlockingError, fallback: GetResponse<ViewModel>.Succeed(null!))
                    .Replay(1)
                    .RefCount();
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.GroupsDisplay.Count)
                            .Select(c => c > 0),
                        overallErr.Select(x => x.Succeeded),
                        (hasGroups, succeeded) => hasGroups && !succeeded)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.OverallErrorButton.Visibility)
                    .DisposeWith(disposable);
                overallErr.Select(x => x.Reason)
                    .BindTo(this, x => x.OverallErrorButton.ToolTip)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.GoToErrorCommand)
                    .BindTo(this, x => x.OverallErrorButton.Command)
                    .DisposeWith(disposable);

                Noggog.WPF.Drag.ListBoxDragDrop<GroupVm>(this.GroupsList, () => this.ViewModel?.SelectedProfile?.Groups)
                    .DisposeWith(disposable);

                // Bind top patcher list buttons
                this.WhenAnyValue(x => x.ViewModel!.GroupsDisplay.Count)
                    .Select(c => c == 0 ? Visibility.Hidden : Visibility.Visible)
                    .BindTo(this, x => x.TopAllPatchersControls.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.EnableAllGroupsCommand)
                    .BindTo(this, x => x.EnableAllGroupsButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.DisableAllGroupsCommand)
                    .BindTo(this, x => x.DisableAllGroupsButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.ExpandAllGroupsCommand)
                    .BindTo(this, x => x.ExpandAllGroupsButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.CollapseAllGroupsCommand)
                    .BindTo(this, x => x.CollapseAllGroupsButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.UpdateAllPatchersCommand)
                    .BindTo(this, x => x.UpdateAllPatchersButton.Command)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.UpdateAllPatchersCommand)
                            .Select(x => x.CanExecute)
                            .Switch(),
                        this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.LockSetting.Lock),
                        (hasUpdate, locked) => hasUpdate && !locked)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.UpdateAllPatchersButton.Visibility)
                    .DisposeWith(disposable);
            });
        }
    }
}
