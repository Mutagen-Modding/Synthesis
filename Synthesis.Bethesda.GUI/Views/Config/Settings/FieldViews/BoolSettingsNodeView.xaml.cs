using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class BoolSettingsNodeViewBase : NoggogUserControl<BoolSettingsVM> { }

    /// <summary>
    /// Interaction logic for BoolSettingsNodeView.xaml
    /// </summary>
    public partial class BoolSettingsNodeView : BoolSettingsNodeViewBase
    {
        public BoolSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.MemberName)
                    .BindToStrict(this, x => x.SettingNameBlock.Text)
                    .DisposeWith(disposable);
                this.BindStrict(ViewModel, vm => vm.Value, view => view.Checkbox.IsChecked)
                    .DisposeWith(disposable);
            });
        }
    }
}
