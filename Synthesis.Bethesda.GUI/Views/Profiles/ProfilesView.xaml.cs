using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for ProfilesView.xaml
/// </summary>
public partial class ProfilesView
{
    public ProfilesView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyValue(x => x.ViewModel!.ProfilesDisplay)
                .BindTo(this, x => x.ProfilesList.ItemsSource)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.AddCommand)
                .BindTo(this, x => x.AddButton.Command)
                .DisposeWith(dispose);

            this.Bind(this.ViewModel, vm => vm.DisplayedProfile, view => view.ProfilesList.SelectedItem)
                .DisposeWith(dispose);

            this.WhenAnyValue(x => x.ViewModel!.DisplayObject)
                .BindTo(this, x => x.ProfileDetail.Content)
                .DisposeWith(dispose);

            // Set up dimmer
            Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.ViewModel!.DisplayedProfile!.Profile, fallback: default(ProfileVm?)),
                    this.WhenAnyValue(x => x.ViewModel!.ProfilesDisplay.Count),
                    (profile, count) => count > 0 && profile == null)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.InitialConfigurationDimmer.Visibility)
                .DisposeWith(dispose);
        });
    }
}