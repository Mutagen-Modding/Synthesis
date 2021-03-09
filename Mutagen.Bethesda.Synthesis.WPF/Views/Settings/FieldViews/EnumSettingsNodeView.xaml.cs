using Noggog.WPF;
using System.Windows.Controls;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class EnumSettingsNodeViewBase : NoggogUserControl<EnumSettingsVM> { }

    /// <summary>
    /// Interaction logic for EnumSettingsNodeView.xaml
    /// </summary>
    public partial class EnumSettingsNodeView : EnumSettingsNodeViewBase
    {
        public EnumSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.EnumNames)
                    .BindToStrict(this, view => view.Combobox.ItemsSource)
                    .DisposeWith(disposable);
                this.BindStrict(ViewModel, vm => vm.Value, view => view.Combobox.SelectedValue)
                    .DisposeWith(disposable);
            });
        }
    }
}
