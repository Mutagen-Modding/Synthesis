using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for ProfileDetailView.xaml
/// </summary>
public partial class ProfileDetailView
{
    public ProfileDetailView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.Bind(this.ViewModel, vm => vm.Profile.NameVm.Name, view => view.ProfileDetailName.Text)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.DeleteCommand)
                .BindTo(this, x => x.DeleteButton.Command)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.SwitchToCommand)
                .BindTo(this, x => x.SelectButton.Command)
                .DisposeWith(disposable);

            this.WhenAnyFallback(x => x.ViewModel!.Profile!.Release, GameRelease.SkyrimSE)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(gameRelease =>
                {
                    return ImageUtility.BitmapImageFromResource(ResourceConstants.AssemblyName, ResourceConstants.GetIcon(gameRelease));
                })
                .ObserveOnGui()
                .BindTo(this, x => x.GameIconImage.Source)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.OpenInternalProfileFolderCommand)
                .BindTo(this, x => x.ProfileInternalFilesButton.Command)
                .DisposeWith(disposable);

            #region Nuget
            this.WhenAnyValue(x => x.ViewModel!.Profile)
                .BindTo(this, x => x.Nugets.DataContext)
                .DisposeWith(disposable);

            this.Bind(this.ViewModel, vm => vm.Profile!.Versioning.MutagenVersioning, view => view.Nugets.Mutagen.VersioningTab.SelectedIndex, (e) => (int)e, i => (NugetVersioningEnum)i)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.Profile!.Versioning.SynthesisVersioning, view => view.Nugets.Synthesis.VersioningTab.SelectedIndex, (e) => (int)e, i => (NugetVersioningEnum)i)
                .DisposeWith(disposable);

            this.Bind(this.ViewModel, vm => vm.Profile!.Versioning.ManualMutagenVersion, view => view.Nugets.Mutagen.ManualVersionBox.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Profile.Versioning.ManualMutagenVersion)
                .Select(x => x.IsNullOrWhitespace())
                .BindTo(this, x => x.Nugets.Mutagen.ManualVersionBox.InError);
            this.Bind(this.ViewModel, vm => vm.Profile!.Versioning.ManualSynthesisVersion, view => view.Nugets.Synthesis.ManualVersionBox.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Profile.Versioning.ManualSynthesisVersion)
                .Select(x => x.IsNullOrWhitespace())
                .BindTo(this, x => x.Nugets.Synthesis.ManualVersionBox.InError);

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

            var mutaExtraVisible = this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.MutagenVersioning)
                .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                .Replay(1)
                .RefCount();
            mutaExtraVisible
                .BindTo(this, x => x.Nugets.Mutagen.ManualVersionBox.Visibility)
                .DisposeWith(disposable);
            mutaExtraVisible
                .BindTo(this, x => x.Nugets.Mutagen.Splitter.Visibility)
                .DisposeWith(disposable);

            var synthExtraVisible = this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.SynthesisVersioning)
                .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                .Replay(1)
                .RefCount();
            synthExtraVisible
                .BindTo(this, x => x.Nugets.Synthesis.ManualVersionBox.Visibility)
                .DisposeWith(disposable);
            synthExtraVisible
                .BindTo(this, x => x.Nugets.Synthesis.Splitter.Visibility)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.ManualMutagenVersion)
                .Select(x => x.IsNullOrWhitespace())
                .Subscribe(x => this.Nugets.Mutagen.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.ManualSynthesisVersion)
                .Select(x => x.IsNullOrWhitespace())
                .Subscribe(x => this.Nugets.Synthesis.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.Profile!.SetAllToProfileCommand)
                .BindTo(this, x => x.ResetPatchersToProfile.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.UpdateMutagenManualToLatestCommand)
                .BindTo(this, x => x.Nugets.Mutagen.UpdateButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.UpdateSynthesisManualToLatestCommand)
                .BindTo(this, x => x.Nugets.Synthesis.UpdateButton.Command)
                .DisposeWith(disposable);

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.MutagenVersioning),
                    this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.UpdateMutagenManualToLatestCommand)
                        .Select(x => x.CanExecute)
                        .Switch(),
                    this.WhenAnyValue(x => x.ViewModel!.Profile!.LockSetting.Lock),
                    (versioning, can, locked) =>
                    {
                        return !locked && versioning == NugetVersioningEnum.Manual && can;
                    })
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Nugets.Mutagen.UpdateButton.Visibility)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ViewModel!.Profile!.Versioning.UpdateSynthesisManualToLatestCommand)
                        .Select(x => x.CanExecute)
                        .Switch(),
                    this.WhenAnyValue(x => x.ViewModel!.Profile!.LockSetting.Lock),
                    (versioning, can, locked) =>
                    {
                        return !locked && versioning == NugetVersioningEnum.Manual && can;
                    })
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Nugets.Synthesis.UpdateButton.Visibility)
                .DisposeWith(disposable);

            this.Bind(this.ViewModel, vm => vm.Profile!.ConsiderPrereleaseNugets, view => view.PrereleaseCheckbox.IsChecked)
                .DisposeWith(disposable);
            #endregion

            this.Bind(this.ViewModel, x => x!.Profile!.Overrides.DataPathOverride, x => x.DataFolderOverrideBox.Text,
                    vmToViewConverter: vm => vm ?? string.Empty,
                    viewToVmConverter: view => view.IsNullOrWhitespace() ? null : view)
                .DisposeWith(disposable);

            this.Bind(this.ViewModel, x => x.Profile!.LockSetting.Lock, x => x.LockToCurrentVersioning.IsChecked)
                .DisposeWith(disposable);

            #region Version Locking
            this.WhenAnyValue(x => x.ViewModel!.Profile!.LockSetting.Lock)
                .Select(x => !x)
                .BindTo(this, x => x.Nugets.IsEnabled)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Profile!.LockSetting.Lock)
                .Select(x => !x)
                .BindTo(this, x => x.ResetVersioningGrid.IsEnabled)
                .DisposeWith(disposable);
            #endregion

            this.WhenAnyValue(x => x.ViewModel!.Profile!.ExportCommand)
                .BindTo(this, x => x.ExportButton.Command)
                .DisposeWith(disposable);

            this.Bind(ViewModel, x => x.Profile!.IgnoreMissingMods, x => x.IgnoreMissingModsCheckbox.IsChecked)
                .DisposeWith(disposable);

            this.Bind(ViewModel, x => x.Profile!.Localize, x => x.Localize.IsChecked)
                .DisposeWith(disposable);

            this.Bind(ViewModel, x => x.Profile!.MasterFile, x => x.MasterCheckbox.IsChecked)
                .DisposeWith(disposable);

            this.Bind(ViewModel, x => x.Profile!.UseUtf8InEmbedded, x => x.Utf8ForEmbeddedStrings.IsChecked)
                .DisposeWith(disposable);

            this.Bind(ViewModel, x => x.Profile!.HeaderVersionOverride, x => x.HeaderVersionOverride.Value)
                .DisposeWith(disposable);

            this.Bind(ViewModel, x => x.Profile!.FormIDRangeMode, x => x.LowerFormIDRangeCombobox.SelectedItem)
                .DisposeWith(disposable);

            //this.WhenAnyValue(x => x.ViewModel!.PersistenceModes)
            //    .BindTo(this, x => x.PersistenceStyleSelector.ItemsSource)
            //    .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.Profile!.SelectedPersistenceMode, v => v.PersistenceStyleSelector.SelectedItem)
                .DisposeWith(disposable);
        });
    }
}