using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views
{
    public class SolutionConfigViewBase : NoggogUserControl<SolutionPatcherVM> { }

    /// <summary>
    /// Interaction logic for SolutionConfigView.xaml
    /// </summary>
    public partial class SolutionConfigView : SolutionConfigViewBase
    {
        public SolutionConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.SolutionPath, view => view.SolutionPathPicker.PickerVM)
                    .DisposeWith(disposable);

                // Hide project picker if sln invalid
                var hasProjs = this.WhenAnyValue(x => x.ViewModel.ProjectsDisplay.Count)
                    .Select(x => x > 0)
                    .Replay(1)
                    .RefCount();
                var projOpacity = hasProjs
                    .Select(x => x ? 1.0d : 0.2d);
                hasProjs.BindToStrict(this, x => x.ProjectLabel.IsEnabled)
                    .DisposeWith(disposable);
                hasProjs.BindToStrict(this, x => x.ProjectsPickerBox.IsEnabled)
                    .DisposeWith(disposable);
                projOpacity.BindToStrict(this, x => x.ProjectLabel.Opacity)
                    .DisposeWith(disposable);
                projOpacity.BindToStrict(this, x => x.ProjectsPickerBox.Opacity)
                    .DisposeWith(disposable);

                // Bind project picker
                // Setting initial values here keeps the VM's property from being bounced to null on
                // initial binding
                this.ProjectsPickerBox.ItemsSource = this.ViewModel.ProjectsDisplay;
                this.ProjectsPickerBox.SelectedItem = this.ViewModel.ProjectSubpath;
                this.BindStrict(this.ViewModel, vm => vm.ProjectSubpath, view => view.ProjectsPickerBox.SelectedItem)
                    .DisposeWith(disposable);

                // Set project picker tooltips
                this.WhenAnyValue(x => x.ViewModel.SelectedProjectPath.ErrorState)
                    .Select(e =>
                    {
                        if (e.Succeeded) return "Project in the solution to run";
                        return $"Project in the solution to run was invalid: {e.Reason}";
                    })
                    .BindToStrict(this, x => x.ProjectsPickerBox.ToolTip)
                    .DisposeWith(disposable);

                // Set up open solution button
                this.WhenAnyValue(x => x.OpenSolutionButton.IsMouseOver)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.OpenSolutionText.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.OpenSolutionButton.IsMouseOver)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.OpenSolutionVSIcon.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.OpenSolutionCommand)
                    .BindToStrict(this, x => x.OpenSolutionButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
