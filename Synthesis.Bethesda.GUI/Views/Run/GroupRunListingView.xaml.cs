using System.Reactive.Disposables;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GroupRunListingViewBase : NoggogUserControl<GroupRunVm> { }

    public partial class GroupRunListingView : GroupRunListingViewBase
    {
        public GroupRunListingView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.Bind(this.ViewModel, vm => vm.RunDisplayControllerVm.SelectedObject, view => view.PatchersList.SelectedValue)
                    .DisposeWith(disposable);
                
                this.WhenAnyValue(x => x.ViewModel!.Patchers)
                    .BindTo(this, x => x.PatchersList.ItemsSource)
                    .DisposeWith(disposable);
            });
        }
    }
}