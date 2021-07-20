using Noggog.WPF;
using System;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive.Linq;
using Noggog;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GitConfigViewBase : NoggogUserControl<GitPatcherVm> { }

    /// <summary>
    /// Interaction logic for GitConfigView.xaml
    /// </summary>
    public partial class GitConfigView : GitConfigViewBase
    {
        public GitConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                var isRepoValid = Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.RepoClonesValid),
                        this.WhenAnyValue(x => x.ViewModel!.SelectedProjectPath.ErrorState),
                        (driver, proj) => driver && proj.Succeeded)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .Replay(1)
                    .RefCount();
                isRepoValid
                    .BindToStrict(this, view => view.PatcherVersioning.Visibility)
                    .DisposeWith(disposable);
                isRepoValid
                    .BindToStrict(this, view => view.Nugets.Visibility)
                    .DisposeWith(disposable);

                #region Patcher Versioning
                this.BindStrict(this.ViewModel, vm => vm.PatcherVersioning, view => view.PatcherVersioning.TabControl.SelectedIndex, (e) => (int)e, i => (PatcherVersioningEnum)i)
                    .DisposeWith(disposable);

                // Bind tag picker
                this.BindStrict(this.ViewModel, vm => vm.TargetTag, view => view.PatcherVersioning.TagPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.TagAutoUpdate, view => view.PatcherVersioning.LatestTagCheck.IsChecked)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableTags, view => view.PatcherVersioning.TagPickerBox.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.TagAutoUpdate)
                    .Select(x => !x)
                    .BindToStrict(this, x => x.PatcherVersioning.TagPickerBox.IsEnabled)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.TargetCommit, view => view.PatcherVersioning.CurrentCommit.Text)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.AttemptedCheckout),
                        this.WhenAnyValue(x => x.ViewModel!.RunnableData),
                        (attempted, data) => attempted && data == null)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .Subscribe(x => this.PatcherVersioning.CurrentCommit.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.TargetBranchName, view => view.PatcherVersioning.BranchNameBox.Text)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.BranchAutoUpdate, view => view.PatcherVersioning.AutoBranchCheck.IsChecked)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.BranchFollowMain, view => view.PatcherVersioning.DefaultBranchCheck.IsChecked)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.BranchFollowMain)
                    .Select(x => !x)
                    .BindToStrict(this, x => x.PatcherVersioning.BranchNameBox.IsEnabled)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.AttemptedCheckout),
                        this.WhenAnyValue(x => x.ViewModel!.RunnableData),
                        this.WhenAnyValue(x => x.ViewModel!.PatcherVersioning),
                        (attempted, data, patcher) => attempted && data == null && patcher == PatcherVersioningEnum.Branch)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .Subscribe(x => this.PatcherVersioning.BranchNameBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);

                // Bind right side stat text
                this.WhenAnyValue(x => x.ViewModel!.RunnableData)
                    .Select(x => x == null ? string.Empty : x.CommitDate.ToShortDateString())
                    .BindToStrict(this, view => view.PatcherVersioning.DateText.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RunnableData)
                    .Select(x => x == null ? string.Empty : x.CommitDate.ToShortTimeString())
                    .BindToStrict(this, view => view.PatcherVersioning.TimeText.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.TargetCommit)
                    .Select(x =>
                    {
                        if (x.IsNullOrWhitespace())
                        {
                            return "No target";
                        }
                        return x.Length < 7 ? x : x.Substring(0, 7);
                    })
                    .BindToStrict(this, view => view.PatcherVersioning.ShaText.Text)
                    .DisposeWith(disposable);

                // Bind update buttons
                this.WhenAnyFallback(x => x.ViewModel!.UpdateToTagCommand)
                    .Select(x => x?.CanExecute ?? Observable.Return(false))
                    .Switch()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.PatcherVersioning.UpdateTagButton.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.UpdateToBranchCommand)
                    .Select(x => x?.CanExecute ?? Observable.Return(false))
                    .Switch()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.PatcherVersioning.UpdateBranchButton.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.UpdateToTagCommand)
                    .BindToStrict(this, x => x.PatcherVersioning.UpdateTagButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.UpdateToBranchCommand)
                    .BindToStrict(this, x => x.PatcherVersioning.UpdateBranchButton.Command)
                    .DisposeWith(disposable);
                #endregion

                #region Nuget
                this.BindStrict(this.ViewModel, vm => vm.MutagenVersioning, view => view.Nugets.Mutagen.VersioningTab.SelectedIndex, (e) => (int)e, i => (PatcherNugetVersioningEnum)i)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.SynthesisVersioning, view => view.Nugets.Synthesis.VersioningTab.SelectedIndex, (e) => (int)e, i => (PatcherNugetVersioningEnum)i)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.ManualMutagenVersion, view => view.Nugets.Mutagen.ManualVersionBox.Text)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.ManualSynthesisVersion, view => view.Nugets.Synthesis.ManualVersionBox.Text)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.MutagenVersioning)
                    .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.Nugets.Mutagen.ManualVersionBox.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersioning)
                    .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.Nugets.Synthesis.ManualVersionBox.Visibility)
                    .DisposeWith(disposable);

                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.MutagenVersionDiff),
                        this.WhenAnyValue(x => x.ViewModel!.MutagenVersioning),
                        (diff, vers) =>
                        {
                            if (vers == PatcherNugetVersioningEnum.Match) return false;
                            if (vers == PatcherNugetVersioningEnum.Latest && diff.MatchVersion == null) return false;
                            return true;
                        })
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .BindToStrict(this, x => x.Nugets.Mutagen.VersionChangeArrow.Visibility)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.SynthesisVersionDiff),
                        this.WhenAnyValue(x => x.ViewModel!.SynthesisVersioning),
                        (diff, vers) =>
                        {
                            if (vers == PatcherNugetVersioningEnum.Match) return false;
                            if (vers == PatcherNugetVersioningEnum.Latest && diff.MatchVersion == null) return false;
                            return true;
                        })
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .BindToStrict(this, x => x.Nugets.Synthesis.VersionChangeArrow.Visibility)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.ManualSynthesisVersion)
                    .Select(x => x.IsNullOrWhitespace())
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .Subscribe(x => this.Nugets.Synthesis.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ManualMutagenVersion)
                    .Select(x => x.IsNullOrWhitespace())
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .Subscribe(x => this.Nugets.Mutagen.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.MutagenVersionDiff)
                    .Select(x => x.MatchVersion)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .BindToStrict(this, x => x.Nugets.Mutagen.ListedVersionText.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersionDiff)
                    .Select(x => x.MatchVersion)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .BindToStrict(this, x => x.Nugets.Synthesis.ListedVersionText.Text)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.MutagenVersionDiff),
                        this.WhenAnyValue(x => x.ViewModel!.MutagenVersioning),
                        (diff, vers) =>
                        {
                            if (vers == PatcherNugetVersioningEnum.Match) return false;
                            if (vers == PatcherNugetVersioningEnum.Latest && diff.SelectedVersion == null) return false;
                            return true;
                        })
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .BindToStrict(this, x => x.Nugets.Mutagen.ListedVersionText.Visibility)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.SynthesisVersionDiff),
                        this.WhenAnyValue(x => x.ViewModel!.SynthesisVersioning),
                        (diff, vers) =>
                        {
                            if (vers == PatcherNugetVersioningEnum.Match) return false;
                            if (vers == PatcherNugetVersioningEnum.Latest && diff.SelectedVersion == null) return false;
                            return true;
                        })
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .BindToStrict(this, x => x.Nugets.Synthesis.ListedVersionText.Visibility)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.MutagenVersionDiff)
                    .Select(x =>
                    {
                        if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                        if (x.SelectedVersion != null && x.MatchVersion != null) return x.SelectedVersion;
                        return x.SelectedVersion ?? x.MatchVersion;
                    })
                    .NotNull()
                    .BindToStrict(this, x => x.Nugets.Mutagen.TargetVersionText.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersionDiff)
                    .Select(x =>
                    {
                        if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                        if (x.SelectedVersion != null && x.MatchVersion != null) return x.SelectedVersion;
                        return x.SelectedVersion ?? x.MatchVersion;
                    })
                    .NotNull()
                    .BindToStrict(this, x => x.Nugets.Synthesis.TargetVersionText.Text)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.MutagenVersioning)
                    .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.Nugets.Mutagen.TargetVersionText.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersioning)
                    .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.Nugets.Synthesis.TargetVersionText.Visibility)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.UpdateMutagenManualToLatestCommand)
                    .BindToStrict(this, x => x.Nugets.Mutagen.UpdateButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.UpdateSynthesisManualToLatestCommand)
                    .BindToStrict(this, x => x.Nugets.Synthesis.UpdateButton.Command)
                    .DisposeWith(disposable);

                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.MutagenVersioning),
                        this.WhenAnyValue(x => x.ViewModel!.UpdateMutagenManualToLatestCommand)
                            .Select(x => x.CanExecute)
                            .Switch(),
                        (versioning, can) =>
                        {
                            return versioning == PatcherNugetVersioningEnum.Manual && can;
                        })
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.Nugets.Mutagen.UpdateButton.Visibility)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.SynthesisVersioning),
                        this.WhenAnyValue(x => x.ViewModel!.UpdateSynthesisManualToLatestCommand)
                            .Select(x => x.CanExecute)
                            .Switch(),
                        (versioning, can) =>
                        {
                            return versioning == PatcherNugetVersioningEnum.Manual && can;
                        })
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.Nugets.Synthesis.UpdateButton.Visibility)
                    .DisposeWith(disposable);
                #endregion

                this.WhenAnyFallback(x => x.ViewModel!.PatcherSettings)
                    .BindToStrict(this, x => x.PatcherSettings.DataContext)
                    .DisposeWith(disposable);

                #region Status Block
                this.WhenAnyFallback(x => x.ViewModel!.StatusDisplay.Text)
                    .Throttle(TimeSpan.FromMilliseconds(50), RxApp.MainThreadScheduler)
                    .BindToStrict(this, x => x.StatusBlock.Text)
                    .DisposeWith(disposable);
                #endregion

                #region Versioning
                this.WhenAnyValue(x => x.ViewModel!.Locking.Lock)
                    .Select(x => !x)
                    .BindToStrict(this, x => x.PatcherVersioning.IsEnabled)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Locking.Lock)
                    .Select(x => !x)
                    .BindToStrict(this, x => x.Nugets.IsEnabled)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Locking.Lock)
                    .Select(x => !x)
                    .BindToStrict(this, x => x.Nugets.IsEnabled)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SetToLastSuccessfulRunCommand)
                    .BindToStrict(this, x => x.SetToLastRunButton.Command)
                    .DisposeWith(disposable);
                #endregion
            });
        }
    }
}
