using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
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
using System.Reactive.Linq;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.GUI.Views
{
    public class TopProfileSelectorViewBase : NoggogUserControl<MainVM> { }

    /// <summary>
    /// Interaction logic for TopProfileSelectorView.xaml
    /// </summary>
    public partial class TopProfileSelectorView : TopProfileSelectorViewBase
    {
        public TopProfileSelectorView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.WhenAnyFallback(x => x.ViewModel!.Configuration.SelectedProfile!.Nickname, string.Empty)
                    .BindToStrict(this, x => x.ProfileNameBlock.Text)
                    .DisposeWith(dispose);

                this.WhenAnyFallback(x => x.ViewModel!.Configuration.SelectedProfile!.Release, GameRelease.SkyrimSE)
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(gameRelease =>
                    {
                        return ImageUtility.BitmapImageFromResource(ResourceConstants.AssemblyName, ResourceConstants.GetIcon(gameRelease));
                    })
                    .ObserveOnGui()
                    .BindToStrict(this, x => x.GameIconImage.Source)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.OpenProfilesPageCommand)
                    .BindToStrict(this, x => x.OpenProfilesPageButton.Command)
                    .DisposeWith(dispose);

                this.WhenAnyFallback(x => x.ViewModel!.Configuration.SelectedProfile!.UpdateProfileNugetVersionCommand)
                    .Cast<ICommand>()
                    .BindToStrict(this, x => x.UpdateButton.Command)
                    .DisposeWith(dispose);
                this.WhenAnyFallback(x => x.ViewModel!.Configuration.SelectedProfile!.UpdateProfileNugetVersionCommand)
                    .Select(x => x.CanExecute ?? Observable.Return(false))
                    .Switch()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.UpdateButton.Visibility)
                    .DisposeWith(dispose);
            });
        }
    }
}
