using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views;

public class BottomDecisionButtonsViewBase : NoggogUserControl<object> { }

/// <summary>
/// Interaction logic for BottomDecisionButtonsView.xaml
/// </summary>
public partial class BottomDecisionButtonsView : BottomDecisionButtonsViewBase
{
    public BottomDecisionButtonsView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyValue(x => x.ConfirmAdditionButton.IsMouseOver)
                .Select(over => over ? Visibility.Visible : Visibility.Hidden)
                .BindTo(this, x => x.ConfirmAdditionText.Visibility)
                .DisposeWith(dispose);
        });
    }
}