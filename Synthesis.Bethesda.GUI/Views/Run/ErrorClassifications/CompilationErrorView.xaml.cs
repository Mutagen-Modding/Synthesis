using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Errors;

namespace Synthesis.Bethesda.GUI.Views.ErrorClassifications;

public partial class CompilationErrorView
{
    public CompilationErrorView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.IsGitPatcher)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.PatcherVersioningPanel.Visibility)
                .DisposeWith(disposable);
        });
    }
}
