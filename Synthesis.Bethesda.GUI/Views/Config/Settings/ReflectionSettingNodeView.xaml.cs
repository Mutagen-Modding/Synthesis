using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class ReflectionSettingNodeViewBase : NoggogUserControl<ReflectionSettingsVM> { }

    /// <summary>
    /// Interaction logic for ReflectionSettingNodeView.xaml
    /// </summary>
    public partial class ReflectionSettingNodeView : ReflectionSettingNodeViewBase
    {
        public ReflectionSettingNodeView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                //this.WhenAnyValue(x => x.ViewModel!.Path)
                //    .BindToStrict(this, x => x.PathBlock.Text)
                //    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Nodes)
                    .BindToStrict(this, x => x.Nodes.ItemsSource)
                    .DisposeWith(disposable);
            });
        }
    }
}
