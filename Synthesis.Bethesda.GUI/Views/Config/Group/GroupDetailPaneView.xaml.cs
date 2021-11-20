using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using System.Windows;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GroupDetailPaneViewBase : NoggogUserControl<GroupVm> { }

    public partial class GroupDetailPaneView : GroupDetailPaneViewBase
    {
        public GroupDetailPaneView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Patchers.Count)
                    .Select(x => x == 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.AddSomePatchersHelpGrid.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Patchers.Count)
                    .Select(x => x > 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.SettingsGrid.Visibility)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, x => x.LoadOrder, x => x.BlacklistedMods.SearchableMods)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, x => x.BlacklistedModKeys, x => x.BlacklistedMods.ModKeys)
                    .DisposeWith(disposable);
            });
        }
    }
}