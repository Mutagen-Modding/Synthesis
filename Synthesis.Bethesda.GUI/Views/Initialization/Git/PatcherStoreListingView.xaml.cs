using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatcherStoreListingViewBase : NoggogUserControl<PatcherStoreListingVM> { }

    /// <summary>
    /// Interaction logic for PatcherStoreListingView.xaml
    /// </summary>
    public partial class PatcherStoreListingView : PatcherStoreListingViewBase
    {
        public PatcherStoreListingView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Name)
                    .BindTo(this, x => x.Name.Text)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.Raw.Customization!.OneLineDescription, string.Empty)
                    .BindTo(this, x => x.OneLine.Text)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.Raw.Customization!.OneLineDescription, string.Empty)
                    .Select(s => string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.OneLine.Visibility)
                    .DisposeWith(dispose);

                var hoverVis = this.WhenAnyValue(x => x.TopGrid.IsMouseOver)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .Replay(1)
                    .RefCount();
                hoverVis.BindTo(this, v => v.OpenWebsiteButton.Visibility)
                    .DisposeWith(dispose);
                hoverVis.BindTo(this, v => v.AddButton.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.AddCommand)
                    .BindTo(this, v => v.AddButton.Command)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.OpenWebsite)
                    .BindTo(this, v => v.OpenWebsiteButton.Command)
                    .DisposeWith(dispose);
            });
        }
    }
}
