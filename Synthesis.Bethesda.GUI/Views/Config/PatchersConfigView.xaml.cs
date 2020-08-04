using Noggog.WPF;
using Noggog;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatchersConfigViewBase : NoggogUserControl<ConfigurationVM> { }

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
                this.WhenAnyValue(x => x.ViewModel.SelectedProfile, x => x.ViewModel.SelectedProfile!.AddGithubPatcherCommand,
                        (p, _) => p?.AddGithubPatcherCommand ?? null)
                    .BindToStrict(this, x => x.AddGithubButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.SelectedProfile, x => x.ViewModel.SelectedProfile!.AddSolutionPatcherCommand,
                        (p, _) => p?.AddSolutionPatcherCommand ?? null)
                    .BindToStrict(this, x => x.AddSolutionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.SelectedProfile, x => x.ViewModel.SelectedProfile!.AddSnippetPatcherCommand,
                        (p, _) => p?.AddSnippetPatcherCommand ?? null)
                    .BindToStrict(this, x => x.AddSnippetButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.PatchersDisplay)
                    .BindToStrict(this, x => x.PatchersList.ItemsSource)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.SelectedPatcher, view => view.PatchersList.SelectedItem)
                    .DisposeWith(disposable);

                // Wire up patcher config data context and visibility
                this.WhenAnyValue(x => x.ViewModel.DisplayedPatcher)
                    .BindToStrict(this, x => x.PatcherConfigView.DataContext)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.DisplayedPatcher)
                    .Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.PatcherConfigView.Visibility)
                    .DisposeWith(disposable);

                // Only show help if zero patchers
                this.WhenAnyValue(x => x.ViewModel.PatchersDisplay.Count)
                    .Select(c => c == 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.AddSomePatchersHelpGrid.Visibility)
                    .DisposeWith(disposable);

                var inInitialConfig = this.WhenAnyValue(x => x.ViewModel.NewPatcher)
                    .Select(x => x != null)
                    .Replay(1)
                    .RefCount();

                // Show dimmer if in initial configuration
                inInitialConfig
                    .Select(initialConfig => initialConfig ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.InitialConfigurationDimmer.Visibility)
                    .DisposeWith(disposable);

                /// Bottom decision button setup
                // Show bottom decision buttons when in configuration
                inInitialConfig
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.InitialConfigurationDecisionGrid.Visibility)
                    .DisposeWith(disposable);

                // Show configuration decision text on button hover
                this.WhenAnyValue(x => x.CancelAdditionButton.IsMouseOver)
                    .Select(over => over ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.CancelAdditionText.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ConfirmAdditionButton.IsMouseOver)
                    .Select(over => over ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, x => x.ConfirmAdditionText.Visibility)
                    .DisposeWith(disposable);

                // Set up discard/confirm clicks
                this.WhenAnyValue(x => x.ViewModel.CancelConfiguration)
                    .BindToStrict(this, x => x.CancelAdditionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.CompleteConfiguration)
                    .BindToStrict(this, x => x.ConfirmAdditionButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
