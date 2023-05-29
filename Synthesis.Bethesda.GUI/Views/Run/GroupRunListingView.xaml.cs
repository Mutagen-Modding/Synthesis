using System.Reactive.Disposables;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class GroupRunListingView
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