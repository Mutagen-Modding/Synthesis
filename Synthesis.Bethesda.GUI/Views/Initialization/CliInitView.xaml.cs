using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for CliInitView.xaml
/// </summary>
public partial class CliInitView
{
    public CliInitView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.NameVm.Name)
                .Select(x => x.IsNullOrWhitespace() ? "Patcher Name" : x)
                .BindTo(this, view => view.PatcherDetailName.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.NameVm.Name)
                .Select(x => x.IsNullOrWhitespace() ? 0.6d : 1d)
                .BindTo(this, view => view.PatcherDetailName.Opacity)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel)
                .BindTo(this, x => x.PatcherIconDisplay.DataContext)
                .DisposeWith(disposable);

            // Set up discard/confirm clicks
            this.WhenAnyValue(x => x.ViewModel!.CancelConfiguration)
                .BindTo(this, x => x.CancelAdditionButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.CompleteConfiguration)
                .BindTo(this, x => x.ConfirmButton.ConfirmAdditionButton.Command)
                .DisposeWith(disposable);
        });
    }
}