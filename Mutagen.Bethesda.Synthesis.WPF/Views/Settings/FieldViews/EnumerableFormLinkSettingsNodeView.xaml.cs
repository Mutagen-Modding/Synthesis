using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class EnumerableFormLinkSettingsNodeViewBase : NoggogUserControl<EnumerableFormLinkSettingsVM> { }

    /// <summary>
    /// Interaction logic for EnumerableFormLinkSettingsNodeView.xaml
    /// </summary>
    public partial class EnumerableFormLinkSettingsNodeView : EnumerableFormLinkSettingsNodeViewBase
    {
        public EnumerableFormLinkSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Meta.DisplayName)
                    .BindToStrict(this, x => x.SettingNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.FocusSettingCommand)
                    .BindToStrict(this, x => x.SettingNameButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values)
                    .BindToStrict(this, x => x.FormPicker.FormKeys)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.LinkCache)
                    .BindToStrict(this, x => x.FormPicker.LinkCache)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ScopedTypes)
                    .BindToStrict(this, x => x.FormPicker.ScopedTypes)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.IsFocused)
                    .Select((focused) => focused ? double.NaN : 200d)
                    .BindToStrict(this, x => x.FormPicker.Height)
                    .DisposeWith(disposable);
            });
        }
    }
}
