using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GitTargetViewBase : NoggogUserControl<GitPatcherVM> { }

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
                this.BindStrict(this.ViewModel, vm => vm.RemoteRepoPath, view => view.RepositoryPath.Text)
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
                    .BindToStrict(this, x => x.CloningRing.Visibility)
                    .DisposeWith(disposable);

                // Bind project picker
                this.BindStrict(this.ViewModel, vm => vm.ProjectSubpath, view => view.ProjectsPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableProjects, view => view.ProjectsPickerBox.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RepoClonesValid)
                    .BindToStrict(this, view => view.ProjectsPickerBox.IsEnabled)
                    .DisposeWith(disposable);

                // Bind git open commands
                this.WhenAnyValue(x => x.ViewModel!.OpenGitPageCommand)
                    .BindToStrict(this, x => x.OpenGitButton.Command)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.NavigateToInternalFilesCommand)
                    .BindToStrict(this, x => x.OpenPatcherInternalFilesButton.Command)
                    .DisposeWith(disposable);

                #region Versioning Lock
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.Profile.LockUpgrades),
                        this.WhenAnyValue(x => x.RespectLocking),
                        (locked, respect) => !locked || !respect)
                    .BindToStrict(this, x => x.RepositoryPath.IsEnabled)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.Profile.LockUpgrades),
                        this.WhenAnyValue(x => x.RespectLocking),
                        (locked, respect) => !locked || !respect)
                    .BindToStrict(this, x => x.ProjectsPickerBox.IsEnabled)
                    .DisposeWith(disposable);
                #endregion
            });
        }
    }
}
