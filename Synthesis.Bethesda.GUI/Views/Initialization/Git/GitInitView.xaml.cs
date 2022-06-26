using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;

namespace Synthesis.Bethesda.GUI.Views;

public class GitInitViewBase : NoggogUserControl<GitPatcherInitVm> { }

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
            this.Bind(this.ViewModel, vm => vm.SelectedTab, view => view.TabControl.SelectedIndex,
                    vmToViewConverter: (e) => (int)e,
                    viewToVmConverter: (i) => (GitPatcherInitVm.TabType)i)
                .DisposeWith(dispose);

            // Set up discard/confirm clicks
            this.WhenAnyValue(x => x.ViewModel!.CancelConfiguration)
                .BindTo(this, x => x.CancelAdditionButton.Command)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.CompleteConfiguration)
                .BindTo(this, x => x.ConfirmButton.ConfirmAdditionButton.Command)
                .DisposeWith(dispose);
        });
    }
}