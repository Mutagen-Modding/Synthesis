using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatcherStoreDetailViewBase : NoggogUserControl<PatcherStoreListingVM> { }

    /// <summary>
    /// Interaction logic for PatcherStoreDetailView.xaml
    /// </summary>
    public partial class PatcherStoreDetailView : PatcherStoreDetailViewBase
    {
        public PatcherStoreDetailView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Name)
                    .BindToStrict(this, x => x.PatcherDetailName.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Repository.Raw.User)
                    .BindToStrict(this, x => x.AuthorsBlock.Text)
                    .DisposeWith(dispose);
                Observable.CombineLatest(
                        this.WhenAnyFallback(x => x.ViewModel!.Raw.Customization!.LongDescription, string.Empty),
                        this.WhenAnyFallback(x => x.ViewModel!.Raw.Customization!.OneLineDescription, string.Empty),
                        (l, s) =>
                        {
                            if (!string.IsNullOrWhiteSpace(l)) return l;
                            if (!string.IsNullOrWhiteSpace(s)) return s;
                            return "No description";
                        })
                    .BindToStrict(this, x => x.DescriptionBox.Text)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.Repository.Stars)
                    .Select(x => x.ToString())
                    .BindToStrict(this, v => v.StarNumberBlock.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Repository.Forks)
                    .Select(x => x.ToString())
                    .BindToStrict(this, v => v.ForkNumberBlock.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Repository.Forks)
                    .Select(f => f == 0 ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, v => v.ForkIcon.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Repository.Forks)
                    .Select(f => f == 0 ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, v => v.ForkNumberBlock.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Repository.Stars)
                    .Select(f => f == 0 ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, v => v.StarIcon.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Repository.Stars)
                    .Select(f => f == 0 ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, v => v.StarNumberBlock.Visibility)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.OpenWebsite)
                    .BindToStrict(this, v => v.OpenWebsiteButton.Command)
                    .DisposeWith(dispose);
            });
        }
    }
}
