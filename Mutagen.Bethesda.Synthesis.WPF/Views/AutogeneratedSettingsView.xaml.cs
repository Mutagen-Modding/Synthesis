using Noggog.WPF;
using ReactiveUI;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class AutogeneratedSettingsViewBase : NoggogUserControl<AutogeneratedSettingsVM> { }

    /// <summary>
    /// Interaction logic for AutogeneratedSettingsView.xaml
    /// </summary>
    public partial class AutogeneratedSettingsView : AutogeneratedSettingsViewBase
    {
        public AutogeneratedSettingsView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyFallback(x => x.ViewModel!.SettingsLoading)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ProcessingRingGrid.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.SettingsLoading)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ProcessingRing.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.Bundle!.Settings)
                    .BindToStrict(this, x => x.ReflectionSettingTabs.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.Bundle!.Settings!.Count)
                    .Select(x => x > 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ReflectionSettingTabs.Visibility)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.SelectedSettings, view => view.ReflectionSettingTabs.SelectedItem)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyFallback(x => x.ViewModel!.Error),
                        this.WhenAnyFallback(x => x.ViewModel!.SettingsLoading),
                        (err, loading) => !loading && err.Failed)
                    .Select(failed => failed ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ErrorPanel.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.Error.Reason)
                    .BindToStrict(this, x => x.ErrorBox.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
