using Mutagen.Bethesda;
using Noggog;
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
    public class ProfileDetailViewBase : NoggogUserControl<ProfileDisplayVM> { }

    /// <summary>
    /// Interaction logic for ProfileDetailView.xaml
    /// </summary>
    public partial class ProfileDetailView : ProfileDetailViewBase
    {
        public ProfileDetailView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.BindStrict(this.ViewModel, vm => vm.Profile!.Nickname, view => view.ProfileDetailName.Text)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                    .BindToStrict(this, x => x.DeleteButton.Command)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.SwitchToCommand)
                    .BindToStrict(this, x => x.SelectButton.Command)
                    .DisposeWith(dispose);

                this.WhenAnyFallback(x => x.ViewModel!.Profile!.Release, GameRelease.SkyrimSE)
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(gameRelease =>
                    {
                        return ImageUtility.BitmapImageFromResource(ResourceConstants.AssemblyName, ResourceConstants.GetIcon(gameRelease));
                    })
                    .ObserveOnGui()
                    .BindToStrict(this, x => x.GameIconImage.Source)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.OpenInternalProfileFolderCommand)
                    .BindToStrict(this, x => x.ProfileInternalFilesButton.Command)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.Profile)
                    .BindToStrict(this, x => x.NugetVersioning.DataContext)
                    .DisposeWith(dispose);
            });
        }
    }
}
