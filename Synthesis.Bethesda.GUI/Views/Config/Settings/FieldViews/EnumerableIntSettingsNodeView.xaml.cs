using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class EnumerableIntSettingsNodeViewBase : NoggogUserControl<EnumerableIntSettingsNodeVM> { }

    /// <summary>
    /// EnumerableInteraction logic for EnumerableIntSettingsNodeView.xaml
    /// </summary>
    public partial class EnumerableIntSettingsNodeView : EnumerableIntSettingsNodeViewBase
    {
        public EnumerableIntSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.MemberName)
                    .BindToStrict(this, x => x.SettingNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values)
                    .BindToStrict(this, x => x.ItemsControl.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.AddCommand)
                    .BindToStrict(this, x => x.AddButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values.Count)
                    .BindToStrict(this, x => x.ItemsControl.AlternationCount)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values.Count)
                    .Select(x => x > 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ItemsControl.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Values.Count)
                    .Select(x => x > 0 ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.NoItemsBlock.Visibility)
                    .DisposeWith(disposable);
            });
        }
    }
}
