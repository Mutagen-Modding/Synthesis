using System.Reactive.Disposables;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Views;

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

            this.WhenAnyValue(x => x.ViewModel!.Run.ModKey.Name)
                .BindTo(this, x => x.GroupName.Text)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.HasStarted)
                .BindTo(this, x => x.GroupFrame.IsOn)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.RunTimeString)
                .BindTo(this, x => x.RunningTimeBlock.Text)
                .DisposeWith(disposable);
        });
    }
}