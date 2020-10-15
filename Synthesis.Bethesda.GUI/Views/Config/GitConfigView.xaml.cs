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
                processing
                    .Select(x => x ?  Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.OpenGitButton.Visibility)
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

                this.BindStrict(this.ViewModel, vm => vm.PatcherVersioning, view => view.PatcherVersioningTab.SelectedIndex, (e) => (int)e, i => (PatcherVersioningEnum)i)
                    .DisposeWith(disposable);

                // Bind tag picker
                this.BindStrict(this.ViewModel, vm => vm.TargetTag, view => view.TagPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableTags, view => view.TagPickerBox.ItemsSource)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.TargetCommit, view => view.CommitShaBox.Text)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.RunnableData),
                        this.WhenAnyValue(x => x.ViewModel!.PatcherVersioning),
                        (data, patcher) => data == null && patcher == PatcherVersioningEnum.Commit)
                    .Subscribe(x => this.CommitShaBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.TargetBranchName, view => view.BranchNameBox.Text)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.RunnableData),
                        this.WhenAnyValue(x => x.ViewModel!.PatcherVersioning),
                        (data, patcher) => data == null && patcher == PatcherVersioningEnum.Branch)
                    .Subscribe(x => this.BranchNameBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RunnableData)
                    .Select(x => x == null ? string.Empty : x.CommitDate.ToString())
                    .BindToStrict(this, view => view.PatcherVersionDateText.Text)
                    .DisposeWith(disposable);

                // Bind git open commands
                this.WhenAnyValue(x => x.ViewModel!.OpenGitPageCommand)
                    .BindToStrict(this, x => x.OpenGitButton.Command)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.MutagenVersioning, view => view.MutagenVersioningTab.SelectedIndex, (e) => (int)e, i => (MutagenVersioningEnum)i)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.ManualMutagenVersion, view => view.MutagenManualVersionBox.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.MutagenVersioning)
                    .Select(x => x == MutagenVersioningEnum.Manual ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.MutagenManualVersionBox.Visibility)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.SynthesisVersioning, view => view.SynthesisVersioningTab.SelectedIndex, (e) => (int)e, i => (SynthesisVersioningEnum)i)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.ManualSynthesisVersion, view => view.SynthesisManualVersionBox.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SynthesisVersioning)
                    .Select(x => x == SynthesisVersioningEnum.Manual ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.SynthesisManualVersionBox.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.UsedMutagenVersion)
                    .Select(x =>
                    {
                        if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                        if (x.SelectedVersion != null && x.MatchVersion != null) return $"{x.MatchVersion} -> {x.SelectedVersion}";
                        return x.SelectedVersion ?? x.MatchVersion;
                    })
                    .BindToStrict(this, x => x.MutagenVersionText.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.UsedSynthesisVersion)
                    .Select(x =>
                    {
                        if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                        if (x.SelectedVersion != null && x.MatchVersion != null) return $"{x.MatchVersion} -> {x.SelectedVersion}";
                        return x.SelectedVersion ?? x.MatchVersion;
                    })
                    .BindToStrict(this, x => x.SynthesisVersionText.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
