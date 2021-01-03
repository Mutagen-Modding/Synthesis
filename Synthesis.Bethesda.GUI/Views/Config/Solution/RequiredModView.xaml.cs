using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Synthesis.Bethesda.GUI.Views
{
    public class RequiredModViewBase : NoggogUserControl<RequiredModVM> { }

    /// <summary>
    /// Interaction logic for RequiredModView.xaml
    /// </summary>
    public partial class RequiredModView : RequiredModViewBase
    {
        public RequiredModView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.ModKey.FileName)
                    .BindToStrict(this, v => v.TitleBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RemoveAsRequiredModCommand)
                    .BindToStrict(this, v => v.AddButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
