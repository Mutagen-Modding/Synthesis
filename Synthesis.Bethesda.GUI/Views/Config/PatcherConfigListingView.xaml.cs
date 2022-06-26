using Noggog.WPF;
using Noggog;
using ReactiveUI;
using System.Reactive.Linq;
using System.Windows;
using System.Reactive.Disposables;
using System.Windows.Controls;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Views;

public class PatcherConfigListingViewBase : NoggogUserControl<PatcherVm> { }

/// <summary>
/// Interaction logic for PatcherConfigListingView.xaml
/// </summary>
public partial class PatcherConfigListingView : PatcherConfigListingViewBase
{
    public PatcherConfigListingView()
    {
        InitializeComponent();
        this.WhenActivated((disposable) =>
        {
            this.WhenAnyFallback(x => x.ViewModel!.IsSelected)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .ObserveOnGui()
                .BindTo(this, x => x.SelectedGlow.Visibility)
                .DisposeWith(disposable);
            this.Bind(this.ViewModel, vm => vm.IsOn, view => view.OnToggle.IsOn)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.NameVm.Name)
                .BindTo(this, x => x.NameBlock.Text)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.State)
                .BindTo(this, x => x.BlockingIssueDisplayCircle.DataContext)
                .DisposeWith(disposable);

            // Set up tooltip display
            this.WhenAnyFallback(x => x.ViewModel!.State.RunnableState.Reason)
                .Select(s =>
                {
                    if (s.IsNullOrWhitespace()) return s;
                    return s.Split(Environment.NewLine).FirstOrDefault();
                })
                .BindTo(this, x => x.TooltipErrorText.Text)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.IsMouseOver),
                    this.WhenAnyFallback(x => x.ViewModel!.State.RunnableState.Succeeded),
                    (over, succeeded) => over && !succeeded)
                .Subscribe(show =>
                {
                    this.SetValue(ToolTipService.IsEnabledProperty, show);
                })
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.State.IsHaltingError)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.ErrorIcon.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.State)
                .Select(x => !x.IsHaltingError && x.RunnableState.Failed ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.ProcessingRing.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.State)
                .Select(x => x.RunnableState.Succeeded ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.SuccessIcon.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.State.RunnableState.Succeeded)
                .Select(x => x ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.TooltipErrorText.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.State)
                .Select(x =>
                {
                    if (x.IsHaltingError) return "Blocking Error";
                    if (x.RunnableState.Failed) return "Processing";
                    return "Ready";
                })
                .BindTo(this, x => x.StatusTypeText.Text)
                .DisposeWith(disposable);

            // ContextMenu
            this.WhenAnyFallback(x => x.ViewModel!.DeleteCommand)
                .BindTo(this, x => x.DeleteContextMenuButton.Command)
                .DisposeWith(disposable);

            // Update button
            this.WhenAnyFallback(x => x.ViewModel)
                .Select(patcher => (patcher as GitPatcherVm)?.UpdateAllCommand.Command)
                .BindTo(this, x => x.UpdateButton.Command)
                .DisposeWith(disposable);
            var gitPatcher = this.WhenAnyFallback(x => x.ViewModel)
                .Select(p => p as GitPatcherVm)
                .Replay(1)
                .RefCount();
            var hasAnyUpdateCmd = Observable.CombineLatest(
                    gitPatcher
                        .Select(patcher => patcher?.UpdateAllCommand.Command.CanExecute ?? Observable.Return(false))
                        .Switch(),
                    gitPatcher
                        .Select(g =>
                        {
                            if (g == null) return Observable.Return(false);
                            return g.WhenAnyValue(x => x.Locking.Lock);
                        })
                        .Switch(),
                    (hasUpdate, locked) => hasUpdate && !locked)
                .Replay(1)
                .RefCount();
            hasAnyUpdateCmd
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.UpdateButton.Visibility)
                .DisposeWith(disposable);
            hasAnyUpdateCmd
                .Select(x => x ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.PatcherTypeIcon.Visibility)
                .DisposeWith(disposable);
        });
    }
}