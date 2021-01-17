using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

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
                // Focus and select new item on add
                this.WhenAnyValue(x => x.ViewModel!.AddCommand)
                    .Select(x => x.EndingExecution())
                    .Switch()
                    .Delay(TimeSpan.FromMilliseconds(50), RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        var item = this.ViewModel?.Values.LastOrDefault();
                        if (item == null) return;
                        this.SettingsListBox.SelectedItem = item;
                        var listBoxItem = this.SettingsListBox
                            .ItemContainerGenerator
                            .ContainerFromItem(item) as ListBoxItem;
                        if (listBoxItem == null) return;
                        listBoxItem.GetChildOfType<WatermarkTextBox>()?.Focus();
                    })
                    .DisposeWith(disposable);
            });
        }
    }
}
