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
                    .Select(count => count > 0 ? Visibility.Visible : Visibility.Hidden)
                    .Replay(1)
                    .RefCount();
                hasProjs.BindToStrict(this, x => x.ProjectLabel.Visibility)
                    .DisposeWith(disposable);
                hasProjs.BindToStrict(this, x => x.ProjectsPickerBox.Visibility)
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
            });
        }
    }
}
