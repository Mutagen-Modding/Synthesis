using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    public class SettingsNodeViewBase : NoggogUserControl<SettingsNodeVM> { }

    /// <summary>
    /// Interaction logic for SettingsNodeView.xaml
    /// </summary>
    public partial class SettingsNodeView : SettingsNodeViewBase
    {
        public SettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
            });
        }
    }
}
