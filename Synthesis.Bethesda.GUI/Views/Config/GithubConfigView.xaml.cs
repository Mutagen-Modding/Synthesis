using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
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
    public class GithubConfigViewBase : NoggogUserControl<GithubPatcherVM> { }

    /// <summary>
    /// Interaction logic for GithubConfigView.xaml
    /// </summary>
    public partial class GithubConfigView : GithubConfigViewBase
    {
        public GithubConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.RepoPath, view => view.RepositoryPath.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
