using Noggog.WPF;
using ReactiveUI;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views
{
    public class ModKeySettingsViewBase : NoggogUserControl<ModKeySettingsVM> { }

    /// <summary>
    /// Interaction logic for ModKeySettingsView.xaml
    /// </summary>
    public partial class ModKeySettingsView : ModKeySettingsViewBase
    {
        public ModKeySettingsView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Meta.DisplayName)
                    .BindToStrict(this, x => x.SettingsNameBox.Text)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, x => x.Value, x => x.ModKeyPicker.ModKey)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DetectedLoadOrder)
                    .BindToStrict(this, x => x.ModKeyPicker.SearchableMods)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.IsFocused)
                    .Select((focused) => focused ? double.NaN : 200d)
                    .BindToStrict(this, x => x.ModKeyPicker.MaxHeight)
                    .DisposeWith(disposable);
            });
        }
    }
}
