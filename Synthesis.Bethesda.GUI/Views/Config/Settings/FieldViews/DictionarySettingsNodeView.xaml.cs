using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class DictionarySettingsNodeViewBase : NoggogUserControl<DictionarySettingsVM> { }

    /// <summary>
    /// Interaction logic for DictionarySettingsNodeView.xaml
    /// </summary>
    public partial class DictionarySettingsNodeView : DictionarySettingsNodeViewBase
    {
        public DictionarySettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.FocusSettingCommand)
                    .BindToStrict(this, x => x.SettingNameButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Meta.DisplayName)
                    .BindToStrict(this, x => x.SettingsNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Items)
                    .BindToStrict(this, x => x.TabControl.ItemsSource)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.Selected, v => v.TabControl.SelectedItem)
                    .DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel!.AddCommand)
                    .BindToStrict(this, x => x.AddButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                    .BindToStrict(this, x => x.DeleteButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ConfirmCommand)
                    .BindToStrict(this, x => x.ConfirmButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.MidDelete)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ConfirmButton.Visibility)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.AddPaneText, v => v.AddNewPaneBox.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
