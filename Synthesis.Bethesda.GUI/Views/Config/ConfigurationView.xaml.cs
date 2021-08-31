using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views
{
    public class ConfigurationViewBase : NoggogUserControl<ConfigurationVm> { }

    /// <summary>
    /// Interaction logic for ConfigurationView.xaml
    /// </summary>
    public partial class ConfigurationView : ConfigurationViewBase
    {
        public ConfigurationView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.NewPatcher)
                    .Select(x => x == null ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.Patchers.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.NewPatcher)
                    .Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.Initialization.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.NewPatcher)
                    .BindTo(this, x => x.Initialization.ViewModel)
                    .DisposeWith(dispose);
            });
        }
    }
}
