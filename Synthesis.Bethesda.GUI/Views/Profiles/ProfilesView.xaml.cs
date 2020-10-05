using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Synthesis.Bethesda.GUI.Views
{
    public class ProfilesViewBase : NoggogUserControl<ProfilesDisplayVM> { }

    /// <summary>
    /// Interaction logic for ProfilesView.xaml
    /// </summary>
    public partial class ProfilesView : ProfilesViewBase
    {
        public ProfilesView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyValue(x => x.ViewModel!.ProfilesDisplay)
                    .BindToStrict(this, x => x.ProfilesList.ItemsSource)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.GoBackCommand)
                    .BindToStrict(this, x => x.BackButton.Command)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.AddCommand)
                    .BindToStrict(this, x => x.AddButton.Command)
                    .DisposeWith(dispose);

                this.BindStrict(this.ViewModel, vm => vm.DisplayedProfile, view => view.ProfilesList.SelectedItem)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.DisplayObject)
                    .BindToStrict(this, x => x.ProfileDetail.Content)
                    .DisposeWith(dispose);

                // Set up dimmer
                Observable.CombineLatest(
                        this.WhenAnyFallback(x => x.ViewModel!.DisplayedProfile!.Profile, fallback: null),
                        this.WhenAnyValue(x => x.ViewModel!.ProfilesDisplay.Count),
                        (profile, count) => count > 0 && profile == null)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.InitialConfigurationDimmer.Visibility)
                    .DisposeWith(dispose);
            });
        }
    }
}
