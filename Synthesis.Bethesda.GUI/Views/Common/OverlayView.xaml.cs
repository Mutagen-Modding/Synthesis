using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for OverlayView.xaml
/// </summary>
public partial class OverlayView
{
    public OverlayView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.ActiveConfirmation, x => x.ViewModel!.ActiveConfirmation!.Title,
                    (c, _) => c?.Title)
                .BindTo(this, x => x.TitleBlock.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.ActiveConfirmation, x => x.ViewModel!.ActiveConfirmation!.Description,
                    (c, _) => c?.Description)
                .BindTo(this, x => x.DescriptionBlock.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Confirmation.ConfirmActionCommand)
                .BindTo(this, x => x.AcceptButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Confirmation.DiscardActionCommand)
                .BindTo(this, x => x.CancelButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Confirmation.TargetConfirmation)
                .BindTo(this, x => x.CustomContent.Content)
                .DisposeWith(disposable);
        });
    }
}