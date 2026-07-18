using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for Mo2PromptView.xaml
/// </summary>
public partial class Mo2PromptView
{
    public Mo2PromptView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.Bind(ViewModel, x => x.EnableMo2Mode, x => x.EnableMo2ModeBox.IsChecked)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.ConfirmCommand, x => x.ConfirmButton.Command)
                .DisposeWith(dispose);
            this.OneWayBind(ViewModel, x => x.GoToFaqCommand, x => x.FaqLink.Command)
                .DisposeWith(dispose);
        });
    }
}
