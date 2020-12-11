using Noggog.WPF;
using System;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive.Linq;
using Noggog;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GitConfigViewBase : NoggogUserControl<GitPatcherVM> { }

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
                this.BindStrict(this.ViewModel, vm => vm.RemoteRepoPath, view => view.RepositoryPath.Text)
                    .DisposeWith(disposable);

                var processing = Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.RepoValidity),
                        this.WhenAnyValue(x => x.ViewModel!.State),
                        (repo, state) => repo.Succeeded && !state.IsHaltingError && state.RunnableState.Failed);

                this.WhenAnyValue(x => x.ViewModel!.RepoValidity)
                    .BindError(this.RepositoryPath)
                    .DisposeWith(disposable);

                processing
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.CloningRing.Visibility)
                    .DisposeWith(disposable);

                // Bind project picker
                this.BindStrict(this.ViewModel, vm => vm.ProjectSubpath, view => view.ProjectsPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableProjects, view => view.ProjectsPickerBox.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RepoClonesValid)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, view => view.ProjectsPickerBox.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RepoClonesValid)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, view => view.ProjectTitle.Visibility)
                    .DisposeWith(disposable);

                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.RepoClonesValid),
                        this.WhenAnyValue(x => x.ViewModel!.SelectedProjectPath.ErrorState),
                        (driver, proj) => driver && proj.Succeeded)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, view => view.AdvancedSettingsArea.Visibility)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.PatcherVersioning, view => view.PatcherVersioning.TabControl.SelectedIndex, (e) => (int)e, i => (PatcherVersioningEnum)i)
                    .DisposeWith(disposable);

                // Bind tag picker
                this.BindStrict(this.ViewModel, vm => vm.TargetTag, view => view.PatcherVersioning.TagPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.LatestTag, view => view.PatcherVersioning.LatestTagCheck.IsChecked)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableTags, view => view.PatcherVersioning.TagPickerBox.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.LatestTag)
                    .Select(x => !x)
                    .BindToStrict(this, x => x.PatcherVersioning.TagPickerBox.IsEnabled)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.TargetCommit, view => view.PatcherVersioning.CommitShaBox.Text)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.AttemptedCheckout),
                        this.WhenAnyValue(x => x.ViewModel!.RunnableData),
                        this.WhenAnyValue(x => x.ViewModel!.PatcherVersioning),
                        (attempted, data, patcher) => attempted && data == null && patcher == PatcherVersioningEnum.Commit)
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .Subscribe(x => this.PatcherVersioning.CommitShaBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.TargetBranchName, view => view.PatcherVersioning.BranchNameBox.Text)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.FollowDefaultBranch, view => view.PatcherVersioning.DefaultBranchCheck.IsChecked)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.FollowDefaultBranch)
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
                this.WhenAnyValue(x => x.ViewModel!.RunnableData)
                    .Select(x => x == null ? string.Empty : x.CommitDate.ToString())
                    .BindToStrict(this, view => view.PatcherVersionDateText.Text)
                    .DisposeWith(disposable);

                // Bind git open commands
                this.WhenAnyValue(x => x.ViewModel!.OpenGitPageCommand)
                    .BindToStrict(this, x => x.OpenGitButton.Command)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.NavigateToInternalFilesCommand)
                    .BindToStrict(this, x => x.OpenPatcherInternalFilesButton.Command)
                    .DisposeWith(disposable);

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
            });
        }
    }
}
