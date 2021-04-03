using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Mutagen.Bethesda.Synthesis.WPF
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
                this.WhenAnyValue(x => x.ViewModel!.Meta.DisplayName)
                    .BindToStrict(this, x => x.SettingNameBlock.Text)
                    .DisposeWith(disposable);
                this.BindStrict(ViewModel, vm => vm.Value, view => view.Checkbox.IsChecked)
                    .DisposeWith(disposable);
            });
        }
    }
}
