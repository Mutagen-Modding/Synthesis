using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views
{
    public class ConfigurationViewBase : NoggogUserControl<ProfileManagerVm> { }

    /// <summary>
    /// Interaction logic for ConfigurationView.xaml
    /// </summary>
    public partial class ConfigurationView : ConfigurationViewBase
    {
        enum Active { Normal, Init, Err }
        
        public ConfigurationView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                var active = Observable.CombineLatest(
                        this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.NewPatcher),
                        this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.EnvironmentErrors.ActiveError),
                        (newPatcher, activeError) =>
                        {
                            if (activeError != null) return Active.Err;
                            if (newPatcher != null) return Active.Init;
                            return Active.Normal;
                        })
                    .StartWith(Active.Normal)
                    .Replay(1)
                    .RefCount();
                active
                    .Select(x => x == Active.Normal ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.Patchers.Visibility)
                    .DisposeWith(dispose);
                active
                    .Select(x => x == Active.Init ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.Initialization.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.NewPatcher)
                    .BindTo(this, x => x.Initialization.ViewModel)
                    .DisposeWith(dispose);
                active
                    .Select(x => x == Active.Err ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.EnvironmentErrors.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.EnvironmentErrors.ActiveError)
                    .BindTo(this, x => x.EnvironmentErrors.ContentPane.Content)
                    .DisposeWith(dispose);
            });
        }
    }
}
