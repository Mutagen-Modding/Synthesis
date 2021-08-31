using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views
{
    public class NoProfileViewBase : NoggogUserControl<NewProfileVm> { }

    /// <summary>
    /// Interaction logic for NoProfileView.xaml
    /// </summary>
    public partial class NoProfileView : NoProfileViewBase
    {
        public NoProfileView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.ReleaseOptions)
                    .BindTo(this, x => x.GameReleaseOptionsControl.ItemsSource)
                    .DisposeWith(disposable);
                this.Bind(this.ViewModel, vm => vm.SelectedGame, view => view.GameReleaseOptionsControl.SelectedItem)
                    .DisposeWith(disposable);
            });
        }
    }
}
