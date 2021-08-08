using Noggog.WPF;
using Noggog;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using DynamicData;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatchersConfigViewBase : NoggogUserControl<ConfigurationVm> { }

    /// <summary>
    /// Interaction logic for PatchersConfigurationView.xaml
    /// </summary>
    public partial class PatchersConfigView : PatchersConfigViewBase
    {
        public PatchersConfigView()
        {
            InitializeComponent();
            this.WhenActivated((disposable) =>
            {
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.AddGitPatcherCommand, fallback: default(ICommand))
                    .BindTo(this, x => x.AddGitButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.AddSolutionPatcherCommand, fallback: default(ICommand))
                    .BindTo(this, x => x.AddSolutionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.AddCliPatcherCommand, fallback: default(ICommand))
                    .BindTo(this, x => x.AddCliButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.PatchersDisplay)
                    .BindTo(this, x => x.PatchersList.ItemsSource)
                    .DisposeWith(disposable);

                this.Bind(this.ViewModel, vm => vm.SelectedProfile!.DisplayController.SelectedObject, view => view.PatchersList.SelectedItem)
                    .DisposeWith(disposable);

                // Wire up patcher config data context and visibility
                this.WhenAnyValue(x => x.ViewModel!.DisplayedObject)
                    .BindTo(this, x => x.DetailControl.Content)
                    .DisposeWith(disposable);

                // Only show help if zero patchers
                this.WhenAnyValue(x => x.ViewModel!.PatchersDisplay.Count)
                    .Select(c => c == 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.AddSomePatchersHelpGrid.Visibility)
                    .DisposeWith(disposable);

                // Show dimmer if in initial configuration
                this.WhenAnyValue(x => x.ViewModel!.Init.NewPatcher)
                    .Select(newPatcher => newPatcher != null ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.InitialConfigurationDimmer.Visibility)
                    .DisposeWith(disposable);

                // Set up go button
                this.WhenAnyValue(x => x.ViewModel!.RunPatchers)
                    .BindTo(this, x => x.GoButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RunPatchers.CanExecute)
                    .Switch()
                    .Select(can => can ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.GoButton.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RunPatchers.CanExecute)
                    .Switch()
                    .CombineLatest(this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.LargeOverallError, GetResponse<PatcherVm>.Succeed(null!)),
                        (can, overall) => !can && overall.Succeeded)
                    .Select(show => show ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.ProcessingRingAnimation.Visibility)
                    .DisposeWith(disposable);

                // Set up large overall error button
                var overallErr = this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.LargeOverallError, fallback: GetResponse<PatcherVm>.Succeed(null!))
                    .Replay(1)
                    .RefCount();
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.PatchersDisplay.Count)
                            .Select(c => c > 0),
                        overallErr.Select(x => x.Succeeded),
                        (hasPatchers, succeeded) => hasPatchers && !succeeded)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.OverallErrorButton.Visibility)
                    .DisposeWith(disposable);
                overallErr.Select(x => x.Reason)
                    .BindTo(this, x => x.OverallErrorButton.ToolTip)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.GoToErrorCommand)
                    .BindTo(this, x => x.OverallErrorButton.Command)
                    .DisposeWith(disposable);

                Noggog.WPF.Drag.ListBoxDragDrop<PatcherVm>(this.PatchersList, () => this.ViewModel?.SelectedProfile?.Patchers)
                    .DisposeWith(disposable);

                // Bind top patcher list buttons
                this.WhenAnyValue(x => x.ViewModel!.PatchersDisplay.Count)
                    .Select(c => c == 0 ? Visibility.Hidden : Visibility.Visible)
                    .BindTo(this, x => x.TopAllPatchersControls.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.EnableAllPatchersCommand)
                    .BindTo(this, x => x.EnableAllPatchersButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.DisableAllPatchersCommand)
                    .BindTo(this, x => x.DisableAllPatchersButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.UpdateAllPatchersCommand)
                    .BindTo(this, x => x.UpdateAllPatchersButton.Command)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.UpdateAllPatchersCommand)
                            .Select(x => x.CanExecute)
                            .Switch(),
                        this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.LockSetting.Lock),
                        (hasUpdate, locked) => hasUpdate && !locked)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.UpdateAllPatchersButton.Visibility)
                    .DisposeWith(disposable);
            });
        }
    }
}
