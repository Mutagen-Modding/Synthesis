using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Mutagen.Bethesda.Synthesis.WPF
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
                this.WhenAnyValue(x => x.ViewModel!.Meta.DisplayName)
                    .BindToStrict(this, x => x.SettingNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Items)
                    .BindToStrict(this, x => x.TabControl.ItemsSource)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.Selected, v => v.TabControl.SelectedItem)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.FocusSettingCommand)
                    .BindToStrict(this, x => x.SettingNameButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
