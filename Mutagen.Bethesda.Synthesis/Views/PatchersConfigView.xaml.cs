using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.Views
{
    public class PatchersConfigViewBase : NoggogUserControl<MainVM> { }

    /// <summary>
    /// Interaction logic for PatchersConfigurationView.xaml
    /// </summary>
    public partial class PatchersConfigView : PatchersConfigViewBase
    {
        public PatchersConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel.AddGithubPatcherCommand)
                    .BindToStrict(this, x => x.AddGithubButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.AddSolutionPatcherCommand)
                    .BindToStrict(this, x => x.AddSolutionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.AddSnippetPatcherCommand)
                    .BindToStrict(this, x => x.AddSnippetButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.PatchersDisplay)
                    .BindToStrict(this, x => x.PatchersList.ItemsSource)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.SelectedPatcher, view => view.PatchersList.SelectedItem)
                    .DisposeWith(disposable);
            });
        }
    }
}
