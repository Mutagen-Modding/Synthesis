using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class DoubleSettingsNodeViewBase : NoggogUserControl<DoubleSettingsNodeVM> { }

    /// <summary>
    /// Doubleeraction logic for DoubleSettingsNodeView.xaml
    /// </summary>
    public partial class DoubleSettingsNodeView : DoubleSettingsNodeViewBase
    {
        public DoubleSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.MemberName)
                    .BindToStrict(this, x => x.SettingNameBlock.Text)
                    .DisposeWith(disposable);
                this.BindStrict(ViewModel, vm => vm.Value, view => view.Spinner.Value)
                    .DisposeWith(disposable);
            });
        }
    }
}
