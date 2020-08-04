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
                this.WhenAnyValue(x => x.ViewModel.Configuration.SelectedProfile, x => x.ViewModel.Configuration.SelectedProfile!.Nickname,
                        (p, _) => p?.Nickname)
                    .BindToStrict(this, x => x.ProfileNameBlock.Text)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel.Configuration.SelectedProfile, x => x.ViewModel.Configuration.SelectedProfile!.Release,
                        (p, _) => p?.Release)
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(gameRelease =>
                    {
                        var path = gameRelease switch
                        {
                            GameRelease.Oblivion => ResourceConstants.OblivionLargeIcon,
                            GameRelease.SkyrimLE => ResourceConstants.SkyrimLeLargeIcon,
                            GameRelease.SkyrimSE => ResourceConstants.SkyrimSseLargeIcon,
                            _ => throw new NotImplementedException()
                        };
                        return ImageUtility.BitmapImageFromResource(ResourceConstants.AssemblyName, path);
                    })
                    .ObserveOnGui()
                    .BindToStrict(this, x => x.GameIconImage.Source)
                    .DisposeWith(dispose);
            });
        }
    }
}
