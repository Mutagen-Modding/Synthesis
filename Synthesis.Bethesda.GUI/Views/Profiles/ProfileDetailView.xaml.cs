using Mutagen.Bethesda;
using Noggog;
using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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
                .ObserveOn(RxSchedulers.TaskpoolScheduler)
                .Select(gameRelease =>
                {
                    return ImageUtility.BitmapImageFromResource(ResourceConstants.AssemblyName, ResourceConstants.GetIcon(gameRelease));
                })
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .BindTo(this, x => x.GameIconImage.Source)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.OpenInternalProfileFolderCommand)
                .BindTo(this, x => x.ProfileInternalFilesButton.Command)
                .DisposeWith(disposable);

            #region Nuget
            this.WhenAnyValue(x => x.ViewModel!.Profile!.SetAllToProfileCommand)
                .BindTo(this, x => x.ResetPatchersToProfile.Command)
                .DisposeWith(disposable);

            this.Bind(this.ViewModel, vm => vm.Profile!.ConsiderPrereleaseNugets, view => view.PrereleaseCheckbox.IsChecked)
                .DisposeWith(disposable);
            #endregion

            this.Bind(this.ViewModel, x => x!.Profile!.Overrides.DataPathOverride, x => x.DataFolderOverrideBox.Text,
                    vmToViewConverter: vm => vm ?? string.Empty,
                    viewToVmConverter: view => view.IsNullOrWhitespace() ? null : view)
                .DisposeWith(disposable);

            #region Version Locking
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

            this.Bind(ViewModel, x => x.Profile!.MasterStyleFallbackEnabled, x => x.MasterStyleFallback.IsChecked)
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

            this.Bind(ViewModel, x => x.Profile!.SplitIfMaxMastersExceeded, x => x.SplitIfMaxMastersExceededCheckbox.IsChecked)
                .DisposeWith(disposable);

            this.Bind(ViewModel, x => x.Profile!.UpdateLoadOrderAfterRun, x => x.UpdateLoadOrderAfterRunCheckbox.IsChecked)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.Profile!.SplitIfMaxMastersExceeded)
                .BindTo(this, x => x.UpdateLoadOrderAfterRunCheckbox.IsEnabled)
                .DisposeWith(disposable);

            // Documentation links
            this.WhenAnyValue(x => x.ViewModel!.Profile!.OpenPatchSettingsDocsCommand)
                .BindTo(this, x => x.PatchSettingsDocsButton.Command)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.Profile!.OpenCompactionDocsCommand)
                .BindTo(this, x => x.CompactionDocsButton.Command)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.Profile!.OpenMasterOverflowDocsCommand)
                .BindTo(this, x => x.MasterOverflowDocsButton.Command)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.Profile!.OpenLanguageDocsCommand)
                .BindTo(this, x => x.LanguageDocsButton.Command)
                .DisposeWith(disposable);
        });
    }
}