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
                this.WhenAnyValue(x => x.ViewModel)
                    .BindTo(this, x => x.ConfigDetailPane.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
