using Mutagen.Bethesda;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views
{
    public class ProfileListingViewBase : NoggogUserControl<ProfileDisplayVM> { }

    /// <summary>
    /// Interaction logic for ProfileListingView.xaml
    /// </summary>
    public partial class ProfileListingView : ProfileListingViewBase
    {
        public ProfileListingView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyFallback(x => x.ViewModel.Profile!.Nickname, fallback: string.Empty)
                    .BindToStrict(this, x => x.NameBlock.Text)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel.SwitchToCommand)
                    .BindToStrict(this, x => x.SelectButton.Command)
                    .DisposeWith(dispose);
            });
        }
    }
}
