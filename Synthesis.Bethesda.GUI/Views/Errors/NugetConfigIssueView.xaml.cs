using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views
{
    public class NugetConfigIssueViewBase : NoggogUserControl<NugetConfigErrorVm> { }

    public partial class NugetConfigIssueView : NugetConfigIssueViewBase
    {
        public NugetConfigIssueView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyFallback(x => x.ViewModel!.Error!.RunFix)
                    .BindToStrict(this, x => x.AttemptFixButton.Command)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.Error!.ErrorText)
                    .BindToStrict(this, x => x.CustomTextBlock.Text)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.NugetConfigPath)
                    .Select(x => x.Path)
                    .BindToStrict(this, x => x.ConfigPathBlock.Text)
                    .DisposeWith(dispose);
            });
        }
    }
}