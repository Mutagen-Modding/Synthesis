using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GitTargetViewBase : NoggogUserControl<GitPatcherVm> { }

    /// <summary>
    /// Interaction logic for GitTargetView.xaml
    /// </summary>
    public partial class GitTargetView : GitTargetViewBase
    {
        public bool RespectLocking
        {
            get => (bool)GetValue(RespectLockingProperty);
            set => SetValue(RespectLockingProperty, value);
        }
        public static readonly DependencyProperty RespectLockingProperty = DependencyProperty.Register(nameof(RespectLocking), typeof(bool), typeof(GitTargetView),
             new FrameworkPropertyMetadata(default(bool)));

        public GitTargetView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.Bind(this.ViewModel, vm => vm.RemoteRepoPathInput.RemoteRepoPath, view => view.RepositoryPath.Text)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.RepoValidity)
                    .BindError(this.RepositoryPath)
                    .DisposeWith(disposable);

                var processing = Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.RepoValidity),
                        this.WhenAnyValue(x => x.ViewModel!.State),
                        (repo, state) => repo.Succeeded && !state.IsHaltingError && state.RunnableState.Failed);

                processing
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.CloningRing.Visibility)
                    .DisposeWith(disposable);

                // Bind project picker
                this.Bind(this.ViewModel, vm => vm.SelectedProjectInput.ProjectSubpath, view => view.ProjectsPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.OneWayBind(this.ViewModel, vm => vm.AvailableProjects, view => view.ProjectsPickerBox.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RepoClonesValid.Valid)
                    .BindTo(this, view => view.ProjectsPickerBox.IsEnabled)
                    .DisposeWith(disposable);

                // Bind git open commands
                this.WhenAnyValue(x => x.ViewModel!.OpenGitPageCommand)
                    .BindTo(this, x => x.OpenGitButton.Command)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.NavigateToInternalFilesCommand)
                    .BindTo(this, x => x.OpenPatcherInternalFilesButton.Command)
                    .DisposeWith(disposable);

                #region Versioning Lock
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.Locking.Lock),
                        this.WhenAnyValue(x => x.RespectLocking),
                        (locked, respect) => !locked || !respect)
                    .BindTo(this, x => x.RepositoryPath.IsEnabled)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.Locking.Lock),
                        this.WhenAnyValue(x => x.RespectLocking),
                        (locked, respect) => !locked || !respect)
                    .BindTo(this, x => x.ProjectsPickerBox.IsEnabled)
                    .DisposeWith(disposable);
                #endregion
            });
        }
    }
}
