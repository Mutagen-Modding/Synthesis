using Noggog.WPF;
using ReactiveUI;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatcherSettingsViewBase : NoggogUserControl<PatcherSettingsVM> { }

    /// <summary>
    /// Interaction logic for PatcherSettingsView.xaml
    /// </summary>
    public partial class PatcherSettingsView : PatcherSettingsViewBase
    {
        public PatcherSettingsView()
        {
            InitializeComponent();
            this.WhenActivated((disposable) =>
            {
                this.WhenAnyValue(x => x.ViewModel!.SettingsConfiguration)
                    .Select(x => x.Style == SettingsStyle.Open ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.OpenSettingsButton.Visibility)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.SettingsConfiguration),
                        this.WhenAnyValue(x => x.ViewModel!.SettingsLoading),
                        (target, loading) => target.Style == SettingsStyle.None && !loading ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.NoSettingsText.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.OpenSettingsCommand)
                    .BindToStrict(this, x => x.OpenSettingsButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SettingsLoading)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ProcessingRing.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ReflectionSettings)
                    .BindToStrict(this, x => x.ReflectionSettingTabs.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.ReflectionSettings.Count)
                    .Select(x => x > 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ReflectionSettingTabs.Visibility)
                    .DisposeWith(disposable);
            });
        }
    }
}
