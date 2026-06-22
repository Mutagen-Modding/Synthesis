using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive.Linq;
using Noggog;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for GitConfigView.xaml
/// </summary>
public partial class GitConfigView
{
    public GitConfigView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            var isRepoValid = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.RepoClonesValid.Valid),
                    this.WhenAnyValue(x => x.ViewModel!.SelectedProjectInput.Picker.ErrorState),
                    (driver, proj) => driver && proj.Succeeded)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .Replay(1)
                .RefCount();
            isRepoValid
                .BindTo(this, view => view.PatcherVersioning.Visibility)
                .DisposeWith(disposable);
            isRepoValid
                .BindTo(this, view => view.Nugets.Visibility)
                .DisposeWith(disposable);

            #region Patcher Versioning
            this.Bind(this.ViewModel, vm => vm.PatcherTargeting.PatcherVersioning, view => view.PatcherVersioning.TabControl.SelectedIndex, (e) => (int)e, i => (PatcherVersioningEnum)i)
                .DisposeWith(disposable);

            // Bind tag picker
            this.Bind(this.ViewModel, vm => vm.PatcherTargeting.TargetTag, view => view.PatcherVersioning.TagPickerBox.SelectedItem)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.PatcherTargeting.TagAutoUpdate, view => view.PatcherVersioning.LatestTagCheck.IsChecked)
                .DisposeWith(disposable);
            this.OneWayBind(this.ViewModel, vm => vm.AvailableTags, view => view.PatcherVersioning.TagPickerBox.ItemsSource)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.PatcherTargeting.TagAutoUpdate)
                .Select(x => !x)
                .BindTo(this, x => x.PatcherVersioning.TagPickerBox.IsEnabled)
                .DisposeWith(disposable);

            this.Bind(this.ViewModel, vm => vm.PatcherTargeting.TargetCommit, view => view.PatcherVersioning.CurrentCommit.Text)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.AttemptedCheckout),
                    this.WhenAnyValue(x => x.ViewModel!.RunnableData),
                    (attempted, data) => attempted && data == null)
                .Throttle(TimeSpan.FromMilliseconds(150), RxSchedulers.MainThreadScheduler)
                .Subscribe(x => this.PatcherVersioning.CurrentCommit.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.PatcherTargeting.TargetBranchName, view => view.PatcherVersioning.BranchNameBox.Text)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.PatcherTargeting.BranchAutoUpdate, view => view.PatcherVersioning.AutoBranchCheck.IsChecked)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.PatcherTargeting.BranchFollowMain, view => view.PatcherVersioning.DefaultBranchCheck.IsChecked)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.PatcherTargeting.BranchFollowMain)
                .Select(x => !x)
                .BindTo(this, x => x.PatcherVersioning.BranchNameBox.IsEnabled)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.AttemptedCheckout),
                    this.WhenAnyValue(x => x.ViewModel!.RunnableData),
                    this.WhenAnyValue(x => x.ViewModel!.PatcherTargeting.PatcherVersioning),
                    (attempted, data, patcher) => attempted && data == null && patcher == PatcherVersioningEnum.Branch)
                .Throttle(TimeSpan.FromMilliseconds(150), RxSchedulers.MainThreadScheduler)
                .Subscribe(x => this.PatcherVersioning.BranchNameBox.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);

            // Bind right side stat text
            this.WhenAnyValue(x => x.ViewModel!.RunnableData)
                .Select(x => x == null ? string.Empty : x.CommitDate.ToShortDateString())
                .BindTo(this, view => view.PatcherVersioning.DateText.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.RunnableData)
                .Select(x => x == null ? string.Empty : x.CommitDate.ToShortTimeString())
                .BindTo(this, view => view.PatcherVersioning.TimeText.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.PatcherTargeting.TargetCommit)
                .Select(x =>
                {
                    if (x.IsNullOrWhitespace())
                    {
                        return "No target";
                    }
                    return x.Length < 7 ? x : x.Substring(0, 7);
                })
                .BindTo(this, view => view.PatcherVersioning.ShaText.Text)
                .DisposeWith(disposable);

            // Bind update buttons
            this.WhenAnyFallback(x => x.ViewModel!.PatcherTargeting.UpdateToTagCommand)
                .Select(x => x?.CanExecute ?? Observable.Return(false))
                .Switch()
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.PatcherVersioning.UpdateTagButton.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.PatcherTargeting.UpdateToBranchCommand)
                .Select(x => x?.CanExecute ?? Observable.Return(false))
                .Switch()
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.PatcherVersioning.UpdateBranchButton.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.PatcherTargeting.UpdateToTagCommand)
                .BindTo(this, x => x.PatcherVersioning.UpdateTagButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.PatcherTargeting.UpdateToBranchCommand)
                .BindTo(this, x => x.PatcherVersioning.UpdateBranchButton.Command)
                .DisposeWith(disposable);
            #endregion

            this.WhenAnyFallback(x => x.ViewModel!.PatcherSettings)
                .BindTo(this, x => x.PatcherSettings.DataContext)
                .DisposeWith(disposable);

            #region Status Block
            this.WhenAnyFallback(x => x.ViewModel!.StatusDisplay.Text)
                .Throttle(TimeSpan.FromMilliseconds(50), RxSchedulers.MainThreadScheduler)
                .BindTo(this, x => x.StatusBlock.Text)
                .DisposeWith(disposable);
            #endregion

            #region Versioning Lock
            this.WhenAnyValue(x => x.ViewModel!.Locking.Lock)
                .Select(x => !x)
                .BindTo(this, x => x.PatcherVersioning.IsEnabled)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.SetToLastSuccessfulRunCommand)
                .BindTo(this, x => x.SetToLastRunButton.Command)
                .DisposeWith(disposable);
            #endregion

            this.WhenAnyValue(x => x.ViewModel!.DeleteUserDataCommand)
                .BindTo(this, x => x.DeleteUserDataButton.Command)
                .DisposeWith(disposable);
        });
    }
}