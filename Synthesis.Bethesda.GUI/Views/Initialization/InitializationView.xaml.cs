using Noggog.WPF;
using System.Windows;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views
{
    public class InitializationViewBase : NoggogUserControl<PatcherInitVM> { }

    /// <summary>
    /// Interaction logic for InitializationView.xaml
    /// </summary>
    public partial class InitializationView : InitializationViewBase
    {
        public InitializationView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.DisplayName, view => view.PatcherDetailName.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindToStrict(this, x => x.PatcherIconDisplay.DataContext)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindToStrict(this, x => x.ConfigDetailPane.Content)
                    .DisposeWith(disposable);

                /// Bottom decision button setup
                // Show bottom decision buttons when in configuration
                this.WhenAnyValue(x => x.ViewModel.OnCompletionPage)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.InitialConfigurationDecisionGrid.Visibility)
                    .DisposeWith(disposable);

                // Set up discard/confirm clicks
                this.WhenAnyValue(x => x.ViewModel.Profile.Config.CancelConfiguration)
                    .BindToStrict(this, x => x.InitialConfigurationDecisionGrid.CancelAdditionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.Profile.Config.CompleteConfiguration)
                    .BindToStrict(this, x => x.InitialConfigurationDecisionGrid.ConfirmAdditionButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
