using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class EnumerableModKeySettingsNodeViewBase : NoggogUserControl<EnumerableModKeySettingsVM> { }

    /// <summary>
    /// Interaction logic for EnumerableModKeySettingsNodeView.xaml
    /// </summary>
    public partial class EnumerableModKeySettingsNodeView : EnumerableModKeySettingsNodeViewBase
    {
        public EnumerableModKeySettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.MemberName)
                    .BindToStrict(this, x => x.SettingsNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values)
                    .BindToStrict(this, x => x.RequiredModsPicker.ModKeys)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DetectedLoadOrder)
                    .BindToStrict(this, x => x.RequiredModsPicker.SearchableMods)
                    .DisposeWith(disposable);
            });
        }
    }
}
