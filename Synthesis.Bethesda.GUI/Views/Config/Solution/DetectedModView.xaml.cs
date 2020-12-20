using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    public class DetectedModViewBase : NoggogUserControl<DetectedModVM> { }

    /// <summary>
    /// Interaction logic for DetectedModView.xaml
    /// </summary>
    public partial class DetectedModView : DetectedModViewBase
    {
        public DetectedModView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.ModKey.FileName)
                    .BindToStrict(this, v => v.TitleBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.AddAsRequiredModCommand)
                    .BindToStrict(this, v => v.AddButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
