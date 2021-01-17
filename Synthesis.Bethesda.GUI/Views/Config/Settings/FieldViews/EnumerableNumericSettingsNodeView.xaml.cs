using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    public class EnumerableNumericSettingsNodeViewBase : NoggogUserControl<EnumerableSettingsNodeVM> { }

    /// <summary>
    /// EnumerableInteraction logic for EnumerableIntSettingsNodeView.xaml
    /// </summary>
    public partial class EnumerableNumericSettingsNodeView : EnumerableNumericSettingsNodeViewBase
    {
        public EnumerableNumericSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.MemberName)
                    .BindToStrict(this, x => x.SettingsNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values.Count)
                    .BindToStrict(this, x => x.SettingsListBox.AlternationCount)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values)
                    .BindToStrict(this, x => x.SettingsListBox.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.AddCommand)
                    .BindToStrict(this, x => x.AddButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
