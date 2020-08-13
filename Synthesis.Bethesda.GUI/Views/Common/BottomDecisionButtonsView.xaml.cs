using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI.Views
{
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
                // Show configuration decision text on button hover
                this.WhenAnyValue(x => x.CancelAdditionButton.IsMouseOver)
                    .Select(over => over ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.CancelAdditionText.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ConfirmAdditionButton.IsMouseOver)
                    .Select(over => over ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.ConfirmAdditionText.Visibility)
                    .DisposeWith(dispose);
            });
        }
    }
}
