using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Settings;
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

                #region Nuget
                this.WhenAnyValue(x => x.ViewModel!.Profile)
                    .BindToStrict(this, x => x.Nugets.DataContext)
                    .DisposeWith(dispose);

                this.BindStrict(this.ViewModel, vm => vm.Profile!.MutagenVersioning, view => view.Nugets.Mutagen.VersioningTab.SelectedIndex, (e) => (int)e, i => (NugetVersioningEnum)i)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.Profile!.SynthesisVersioning, view => view.Nugets.Synthesis.VersioningTab.SelectedIndex, (e) => (int)e, i => (NugetVersioningEnum)i)
                    .DisposeWith(dispose);

                this.BindStrict(this.ViewModel, vm => vm.Profile!.ManualMutagenVersion, view => view.Nugets.Mutagen.ManualVersionBox.Text)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.Profile!.ManualSynthesisVersion, view => view.Nugets.Synthesis.ManualVersionBox.Text)
                    .DisposeWith(dispose);

                Nugets.Mutagen.TargetVersionText.Visibility = Visibility.Collapsed;
                Nugets.Synthesis.TargetVersionText.Visibility = Visibility.Collapsed;
                Nugets.Mutagen.ListedVersionText.Visibility = Visibility.Collapsed;
                Nugets.Synthesis.ListedVersionText.Visibility = Visibility.Collapsed;
                Nugets.Mutagen.VersionChangeArrow.Visibility = Visibility.Collapsed;
                Nugets.Synthesis.VersionChangeArrow.Visibility = Visibility.Collapsed;
                Nugets.Mutagen.Splitter.Visibility = Visibility.Collapsed;
                Nugets.Synthesis.Splitter.Visibility = Visibility.Collapsed;
                Nugets.Mutagen.ProfileTab.Visibility = Visibility.Collapsed;
                Nugets.Synthesis.ProfileTab.Visibility = Visibility.Collapsed;

                var mutaExtraVisible = this.WhenAnyValue(x => x.ViewModel!.Profile!.MutagenVersioning)
                    .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                    .PublishRefCount();
                mutaExtraVisible
                    .BindToStrict(this, x => x.Nugets.Mutagen.ManualVersionBox.Visibility)
                    .DisposeWith(dispose);
                mutaExtraVisible
                    .BindToStrict(this, x => x.Nugets.Mutagen.Splitter.Visibility)
                    .DisposeWith(dispose);

                var synthExtraVisible = this.WhenAnyValue(x => x.ViewModel!.Profile!.SynthesisVersioning)
                    .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                    .PublishRefCount();
                synthExtraVisible
                    .BindToStrict(this, x => x.Nugets.Synthesis.ManualVersionBox.Visibility)
                    .DisposeWith(dispose);
                synthExtraVisible
                    .BindToStrict(this, x => x.Nugets.Synthesis.Splitter.Visibility)
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.Profile!.ManualMutagenVersion)
                    .Select(x => x.IsNullOrWhitespace())
                    .Subscribe(x => this.Nugets.Mutagen.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Profile!.ManualSynthesisVersion)
                    .Select(x => x.IsNullOrWhitespace())
                    .Subscribe(x => this.Nugets.Synthesis.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(dispose);

                this.WhenAnyValue(x => x.ViewModel!.Profile!.SetAllToProfileCommand)
                    .BindToStrict(this, x => x.ResetPatchersToProfile.Command)
                    .DisposeWith(dispose);
                #endregion
            });
        }
    }
}
