using Noggog.WPF;
using ReactiveUI;
using System.Collections;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    public class EnumerableNumericSettingsNodeViewBase : NoggogUserControl<EnumerableSettingsNodeVM> { }

    /// <summary>
    /// EnumerableInteraction logic for EnumerableNumericSettingsNodeView.xaml
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
                this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                    .BindToStrict(this, x => x.DeleteButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DeleteCommand.CanExecute)
                    .Switch()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.DeleteButton.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.SettingsListBox.SelectedItems)
                    .BindTo(this, x => x.ViewModel!.SelectedValues)
                    .DisposeWith(disposable);
            });
        }
    }
}
