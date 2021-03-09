using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class ObjectSettingsNodeViewBase : NoggogUserControl<ObjectSettingsVM> { }

    /// <summary>
    /// Interaction logic for ObjectSettingsNodeView.xaml
    /// </summary>
    public partial class ObjectSettingsNodeView : ObjectSettingsNodeViewBase
    {
        public ObjectSettingsNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Meta.DisplayName)
                    .BindToStrict(this, x => x.SettingNameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Meta.DisplayName)
                    .Select(x => x.IsNullOrWhitespace() ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.SettingNameBlock.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Nodes)
                    .BindToStrict(this, x => x.Nodes.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.FocusSettingCommand)
                    .BindToStrict(this, x => x.SettingNameButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Meta.MainVM.ScrolledToSettings)
                    .WithLatestFrom(this.WhenAnyValue(x => x.ViewModel!.Meta.MainVM.SelectedSettings),
                        (Scrolled, Selected) => (Scrolled, Selected))
                    .Where(x => x.Selected == this.ViewModel)
                    .Delay(TimeSpan.FromMilliseconds(300), RxApp.MainThreadScheduler)
                    .Subscribe(setting =>
                    {
                        if (this.ViewModel == null || this.Nodes.Items == null || this.Nodes.Items.Count == 0) return;
                        var target = setting.Scrolled;
                        while (target != null)
                        {
                            if (this.ViewModel.Nodes.Contains(target))
                            {
                                this.Nodes.ScrollIntoView(target);
                            }
                            target = target.Meta.Parent;
                        }
                    })
                    .DisposeWith(disposable);
            });
        }
    }
}
