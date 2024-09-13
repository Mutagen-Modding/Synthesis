using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Views;

public partial class SolutionSourceConfigView
{
    public SolutionSourceConfigView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(x => x.ViewModel!.SolutionPathInput.Picker)
                .BindTo(this, view => view.SolutionPathPicker.PickerVM)
                .DisposeWith(disposable);
            
            // Hide project picker if sln invalid
            var hasProjs = this.WhenAnyValue(x => x.ViewModel!.AvailableProjects.Count)
                .Select(x => x > 0)
                .Replay(1)
                .RefCount();
            var projOpacity = hasProjs
                .Select(x => x ? 1.0d : 0.2d);
            hasProjs.BindTo(this, x => x.ProjectLabel.IsEnabled)
                .DisposeWith(disposable);
            hasProjs.BindTo(this, x => x.ProjectsPickerBox.IsEnabled)
                .DisposeWith(disposable);
            projOpacity.BindTo(this, x => x.ProjectLabel.Opacity)
                .DisposeWith(disposable);
            projOpacity.BindTo(this, x => x.ProjectsPickerBox.Opacity)
                .DisposeWith(disposable);

            // Set project picker tooltips
            this.WhenAnyValue(x => x.ViewModel!.SelectedProjectInput.Picker.ErrorState)
                .Select(e =>
                {
                    if (e.Succeeded) return "Project in the solution to run";
                    return $"Project in the solution to run was invalid: {e.Reason}";
                })
                .BindTo(this, x => x.ProjectsPickerBox.ToolTip)
                .DisposeWith(disposable);

            // Set up open solution button
            this.WhenAnyValue(x => x.ViewModel!.OpenSolutionCommand)
                .BindTo(this, x => x.OpenSolutionButton.Command)
                .DisposeWith(disposable);
        });
    }
}