using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views
{
    public class NewProfileViewBase : NoggogUserControl<NewProfileVm> { }

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
                this.Bind(this.ViewModel, vm => vm.Nickname, view => view.PatcherDetailName.Text)
                    .DisposeWith(dispose);

                this.OneWayBind(ViewModel, x => x.CategoryOptions, x => x.GameCategoryOptionsControl.ItemsSource)
                    .DisposeWith(dispose);
                this.Bind(this.ViewModel, vm => vm.SelectedCategory, view => view.GameCategoryOptionsControl.SelectedItem)
                    .DisposeWith(dispose);

                this.WhenAnyFallback(x => x.ViewModel!.ReleaseOptions, fallback: default)
                    .BindTo(this, x => x.GameReleaseOptionsControl.ItemsSource)
                    .DisposeWith(dispose);
                this.Bind(this.ViewModel, vm => vm.SelectedRelease, view => view.GameReleaseOptionsControl.SelectedItem)
                    .DisposeWith(dispose);
            });
        }
    }
}
