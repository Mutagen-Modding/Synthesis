using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GitInitViewBase : NoggogUserControl<GitPatcherInitVM> { }

    /// <summary>
    /// Interaction logic for GitInitView.xaml
    /// </summary>
    public partial class GitInitView : GitInitViewBase
    {
        public GitInitView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.BindStrict(this.ViewModel, vm => vm.SelectedTab, view => view.TabControl.SelectedIndex,
                        vmToViewConverter: (e) => (int)e,
                        viewToVmConverter: (i) => (GitPatcherInitVM.TabType)i)
                    .DisposeWith(dispose);

                // Set up discard/confirm clicks
                this.WhenAnyValue(x => x.ViewModel!.Profile.Config.CancelConfiguration)
                    .BindToStrict(this, x => x.CancelAdditionButton.Command)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Profile.Config.CompleteConfiguration)
                    .BindToStrict(this, x => x.ConfirmButton.ConfirmAdditionButton.Command)
                    .DisposeWith(dispose);
            });
        }
    }
}
