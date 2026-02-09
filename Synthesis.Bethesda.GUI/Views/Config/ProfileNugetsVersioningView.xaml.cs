using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.Views;

public partial class ProfileNugetsVersioningView
{
    public ProfileNugetsVersioningView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            // Hide elements not applicable to profile versioning
            Mutagen.ProfileTab.Visibility = Visibility.Collapsed;
            Synthesis.ProfileTab.Visibility = Visibility.Collapsed;
            Mutagen.TargetVersionText.Visibility = Visibility.Collapsed;
            Synthesis.TargetVersionText.Visibility = Visibility.Collapsed;
            Mutagen.ListedVersionText.Visibility = Visibility.Collapsed;
            Synthesis.ListedVersionText.Visibility = Visibility.Collapsed;
            Mutagen.VersionChangeArrow.Visibility = Visibility.Collapsed;
            Synthesis.VersionChangeArrow.Visibility = Visibility.Collapsed;
            Mutagen.Splitter.Visibility = Visibility.Collapsed;
            Synthesis.Splitter.Visibility = Visibility.Collapsed;

            // Bind versioning tab selection
            this.Bind(this.ViewModel,
                    vm => vm.Versioning.MutagenVersioning,
                    view => view.Mutagen.VersioningTab.SelectedIndex,
                    e => (int)e,
                    i => (NugetVersioningEnum)i)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel,
                    vm => vm.Versioning.SynthesisVersioning,
                    view => view.Synthesis.VersioningTab.SelectedIndex,
                    e => (int)e,
                    i => (NugetVersioningEnum)i)
                .DisposeWith(disposable);

            // Bind manual version text boxes
            this.Bind(this.ViewModel,
                    vm => vm.Versioning.ManualMutagenVersion,
                    view => view.Mutagen.ManualVersionBox.Text)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel,
                    vm => vm.Versioning.ManualSynthesisVersion,
                    view => view.Synthesis.ManualVersionBox.Text)
                .DisposeWith(disposable);

            // Error state for manual version boxes
            this.WhenAnyValue(x => x.ViewModel!.Versioning.ManualMutagenVersion)
                .Select(x => x.IsNullOrWhitespace())
                .Subscribe(x => Mutagen.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Versioning.ManualSynthesisVersion)
                .Select(x => x.IsNullOrWhitespace())
                .Subscribe(x => Synthesis.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);

            // Manual version box visibility based on versioning selection
            this.WhenAnyValue(x => x.ViewModel!.Versioning.MutagenVersioning)
                .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Mutagen.ManualVersionBox.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Versioning.SynthesisVersioning)
                .Select(x => x == NugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Synthesis.ManualVersionBox.Visibility)
                .DisposeWith(disposable);

            // Update button commands
            this.WhenAnyValue(x => x.ViewModel!.Versioning.UpdateMutagenManualToLatestCommand)
                .BindTo(this, x => x.Mutagen.UpdateButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.Versioning.UpdateSynthesisManualToLatestCommand)
                .BindTo(this, x => x.Synthesis.UpdateButton.Command)
                .DisposeWith(disposable);

            // Update button visibility
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.Versioning.MutagenVersioning),
                    this.WhenAnyValue(x => x.ViewModel!.Versioning.UpdateMutagenManualToLatestCommand)
                        .Select(x => x.CanExecute)
                        .Switch(),
                    this.WhenAnyValue(x => x.ViewModel!.LockSetting.Lock),
                    (versioning, can, locked) => !locked && versioning == NugetVersioningEnum.Manual && can)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Mutagen.UpdateButton.Visibility)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.Versioning.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ViewModel!.Versioning.UpdateSynthesisManualToLatestCommand)
                        .Select(x => x.CanExecute)
                        .Switch(),
                    this.WhenAnyValue(x => x.ViewModel!.LockSetting.Lock),
                    (versioning, can, locked) => !locked && versioning == NugetVersioningEnum.Manual && can)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Synthesis.UpdateButton.Visibility)
                .DisposeWith(disposable);

            // Lock toggle
            this.Bind(this.ViewModel,
                    vm => vm.LockSetting.Lock,
                    view => view.LockToggle.IsChecked)
                .DisposeWith(disposable);

            // Disable nuget controls if locked (but not the lock toggle itself)
            this.WhenAnyValue(x => x.ViewModel!.LockSetting.Lock)
                .Select(x => !x)
                .BindTo(this, x => x.Mutagen.IsEnabled)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.LockSetting.Lock)
                .Select(x => !x)
                .BindTo(this, x => x.Synthesis.IsEnabled)
                .DisposeWith(disposable);
        });
    }
}
