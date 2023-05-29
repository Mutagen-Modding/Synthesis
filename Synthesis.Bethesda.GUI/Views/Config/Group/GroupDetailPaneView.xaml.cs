using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class GroupDetailPaneView
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