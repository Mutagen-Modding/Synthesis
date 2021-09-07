using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GroupConfigViewBase : NoggogUserControl<GroupVm> { }

    /// <summary>
    /// Interaction logic for GroupConfigView.xaml
    /// </summary>
    public partial class GroupConfigView : GroupConfigViewBase
    {
        public GroupConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.Bind(this.ViewModel, x => x.Name, x => x.GroupDetailName.Text)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.ModKey)
                    .Select(x => x.Failed)
                    .BindTo(this, x => x.GroupDetailName.InError)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.ModKey)
                    .Select(x => x.Reason)
                    .BindTo(this, x => x.GroupDetailName.ErrorText)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                    .BindTo(this, x => x.DeleteButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
