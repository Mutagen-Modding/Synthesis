using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class NewProfileViewBase : NoggogUserControl<NewProfileVM> { }

    /// <summary>
    /// Interaction logic for NewProfileView.xaml
    /// </summary>
    public partial class NewProfileView : NewProfileViewBase
    {
        public NewProfileView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.BindStrict(this.ViewModel, vm => vm.Nickname, view => view.PatcherDetailName.Text)
                    .DisposeWith(dispose);

                this.WhenAnyFallback(x => x.ViewModel.ReleaseOptions, fallback: default)
                    .BindToStrict(this, x => x.GameReleaseOptionsControl.ItemsSource)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.SelectedGame, view => view.GameReleaseOptionsControl.SelectedItem)
                    .DisposeWith(dispose);
            });
        }
    }
}
