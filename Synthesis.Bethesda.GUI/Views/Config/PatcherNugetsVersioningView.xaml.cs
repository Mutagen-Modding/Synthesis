using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Views;

public partial class PatcherNugetsVersioningView
{
    public PatcherNugetsVersioningView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            // Bind versioning tab selection
            this.Bind(this.ViewModel,
                    vm => vm.NugetTargeting.MutagenVersioning,
                    view => view.Mutagen.VersioningTab.SelectedIndex,
                    e => (int)e,
                    i => (PatcherNugetVersioningEnum)i)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel,
                    vm => vm.NugetTargeting.SynthesisVersioning,
                    view => view.Synthesis.VersioningTab.SelectedIndex,
                    e => (int)e,
                    i => (PatcherNugetVersioningEnum)i)
                .DisposeWith(disposable);

            // Bind manual version text boxes
            this.Bind(this.ViewModel,
                    vm => vm.NugetTargeting.ManualMutagenVersion,
                    view => view.Mutagen.ManualVersionBox.Text)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel,
                    vm => vm.NugetTargeting.ManualSynthesisVersion,
                    view => view.Synthesis.ManualVersionBox.Text)
                .DisposeWith(disposable);

            // Error state for manual version boxes
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.ManualMutagenVersion)
                .Select(x => x.IsNullOrWhitespace())
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x => Mutagen.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.ManualSynthesisVersion)
                .Select(x => x.IsNullOrWhitespace())
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(x => Synthesis.ManualVersionBox.SetValue(ControlsHelper.InErrorProperty, x))
                .DisposeWith(disposable);

            // Manual version box visibility based on versioning selection
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.MutagenVersioning)
                .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Mutagen.ManualVersionBox.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.SynthesisVersioning)
                .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Synthesis.ManualVersionBox.Visibility)
                .DisposeWith(disposable);

            // Version change arrow visibility
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.NugetDiff.MutagenVersionDiff),
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.MutagenVersioning),
                    (diff, vers) =>
                    {
                        if (vers == PatcherNugetVersioningEnum.Match) return false;
                        if (vers == PatcherNugetVersioningEnum.Latest && diff.MatchVersion == null) return false;
                        return true;
                    })
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .BindTo(this, x => x.Mutagen.VersionChangeArrow.Visibility)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.NugetDiff.SynthesisVersionDiff),
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.SynthesisVersioning),
                    (diff, vers) =>
                    {
                        if (vers == PatcherNugetVersioningEnum.Match) return false;
                        if (vers == PatcherNugetVersioningEnum.Latest && diff.MatchVersion == null) return false;
                        return true;
                    })
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .BindTo(this, x => x.Synthesis.VersionChangeArrow.Visibility)
                .DisposeWith(disposable);

            // Listed version text
            this.WhenAnyValue(x => x.ViewModel!.NugetDiff.MutagenVersionDiff)
                .Select(x => x.MatchVersion)
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .BindTo(this, x => x.Mutagen.ListedVersionText.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.NugetDiff.SynthesisVersionDiff)
                .Select(x => x.MatchVersion)
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .BindTo(this, x => x.Synthesis.ListedVersionText.Text)
                .DisposeWith(disposable);

            // Listed version visibility
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.NugetDiff.MutagenVersionDiff),
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.MutagenVersioning),
                    (diff, vers) =>
                    {
                        if (vers == PatcherNugetVersioningEnum.Match) return false;
                        if (vers == PatcherNugetVersioningEnum.Latest && diff.SelectedVersion == null) return false;
                        return true;
                    })
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .BindTo(this, x => x.Mutagen.ListedVersionText.Visibility)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.NugetDiff.SynthesisVersionDiff),
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.SynthesisVersioning),
                    (diff, vers) =>
                    {
                        if (vers == PatcherNugetVersioningEnum.Match) return false;
                        if (vers == PatcherNugetVersioningEnum.Latest && diff.SelectedVersion == null) return false;
                        return true;
                    })
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .BindTo(this, x => x.Synthesis.ListedVersionText.Visibility)
                .DisposeWith(disposable);

            // Target version text
            this.WhenAnyValue(x => x.ViewModel!.NugetDiff.MutagenVersionDiff)
                .Select(x =>
                {
                    if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                    if (x.SelectedVersion != null && x.MatchVersion != null) return x.SelectedVersion;
                    return x.SelectedVersion ?? x.MatchVersion;
                })
                .NotNull()
                .BindTo(this, x => x.Mutagen.TargetVersionText.Text)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.NugetDiff.SynthesisVersionDiff)
                .Select(x =>
                {
                    if (object.Equals(x.MatchVersion, x.SelectedVersion)) return x.MatchVersion;
                    if (x.SelectedVersion != null && x.MatchVersion != null) return x.SelectedVersion;
                    return x.SelectedVersion ?? x.MatchVersion;
                })
                .NotNull()
                .BindTo(this, x => x.Synthesis.TargetVersionText.Text)
                .DisposeWith(disposable);

            // Target version visibility
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.MutagenVersioning)
                .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.Mutagen.TargetVersionText.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.SynthesisVersioning)
                .Select(x => x == PatcherNugetVersioningEnum.Manual ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.Synthesis.TargetVersionText.Visibility)
                .DisposeWith(disposable);

            // Update button commands
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.UpdateMutagenManualToLatestCommand)
                .BindTo(this, x => x.Mutagen.UpdateButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.UpdateSynthesisManualToLatestCommand)
                .BindTo(this, x => x.Synthesis.UpdateButton.Command)
                .DisposeWith(disposable);

            // Update button visibility
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.MutagenVersioning),
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.UpdateMutagenManualToLatestCommand)
                        .Select(x => x.CanExecute)
                        .Switch(),
                    (versioning, can) => versioning == PatcherNugetVersioningEnum.Manual && can)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Mutagen.UpdateButton.Visibility)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ViewModel!.NugetTargeting.UpdateSynthesisManualToLatestCommand)
                        .Select(x => x.CanExecute)
                        .Switch(),
                    (versioning, can) => versioning == PatcherNugetVersioningEnum.Manual && can)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.Synthesis.UpdateButton.Visibility)
                .DisposeWith(disposable);

            // Disable controls if locked
            this.WhenAnyValue(x => x.ViewModel!.Locking.Lock)
                .Select(x => !x)
                .BindTo(this, x => x.IsEnabled)
                .DisposeWith(disposable);
        });
    }
}
