using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class IntSettingsNodeViewBase : NoggogUserControl<IntSettingsNodeVM> { }

    /// <summary>
    /// Interaction logic for IntSettingsNodeView.xaml
    /// </summary>
    public partial class IntSettingsNodeView : IntSettingsNodeViewBase
    {
        public IntSettingsNodeView()
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
