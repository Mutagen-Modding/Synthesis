using Noggog.WPF;
using System.Windows;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization;

namespace Synthesis.Bethesda.GUI.Views
{
    public class InitializationViewBase : NoggogUserControl<PatcherInitVm> { }

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
                    .BindToStrict(this, x => x.ConfigDetailPane.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
