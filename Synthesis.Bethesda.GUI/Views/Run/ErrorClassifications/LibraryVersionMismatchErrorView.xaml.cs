using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Errors;

namespace Synthesis.Bethesda.GUI.Views.ErrorClassifications;

public partial class LibraryVersionMismatchErrorView
{
    public LibraryVersionMismatchErrorView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            // Show/hide patcher versioning panel based on whether it's a Git patcher
            this.WhenAnyValue(x => x.ViewModel!.IsGitPatcher)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.PatcherVersioningPanel.Visibility)
                .DisposeWith(disposable);
        });
    }
}
