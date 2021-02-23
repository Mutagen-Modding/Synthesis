using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class StaticEnumDictionaryViewBase : NoggogUserControl<EnumDictionarySettingsVM> { }

    /// <summary>
    /// Interaction logic for StaticEnumDictionaryView.xaml
    /// </summary>
    public partial class StaticEnumDictionaryView : StaticEnumDictionaryViewBase
    {
        public StaticEnumDictionaryView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.MemberName)
                    .BindToStrict(this, x => x.SettingsNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Items)
                    .BindToStrict(this, x => x.TabControl.ItemsSource)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.Selected, v => v.TabControl.SelectedItem)
                    .DisposeWith(disposable);
            });
        }
    }
}
