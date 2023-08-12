using System.Reactive.Disposables;
using System.Windows.Input;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for EditLinkItemView.xaml
/// </summary>
public partial class EditLinkItemView
{
    public EditLinkItemView()
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
            this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.Init.AddGroupCommand, fallback: default(ICommand))
                .BindTo(this, x => x.AddGroupButton.Command)
                .DisposeWith(disposable);
        });
    }
}