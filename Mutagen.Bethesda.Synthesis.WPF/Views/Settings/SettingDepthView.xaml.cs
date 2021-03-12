using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class SettingDepthViewBase : NoggogUserControl<SettingsNodeVM> { }

    /// <summary>
    /// Interaction logic for SettingDepthView.xaml
    /// </summary>
    public partial class SettingDepthView : SettingDepthViewBase
    {
        public SettingDepthView()
        {
            InitializeComponent();
            this.WhenActivated((disposable) =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Parents.Value)
                    .BindToStrict(this, x => x.ParentSettingList.ItemsSource)
                    .DisposeWith(disposable);
            });
        }
    }
}
