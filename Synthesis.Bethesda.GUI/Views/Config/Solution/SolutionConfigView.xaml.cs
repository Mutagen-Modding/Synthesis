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
                this.WhenAnyValue(x => x.ViewModel!.SolutionPath)
                    .BindToStrict(this, view => view.SolutionPathPicker.PickerVM)
                    .DisposeWith(disposable);

                // Hide project picker if sln invalid
                var hasProjs = this.WhenAnyValue(x => x.ViewModel!.AvailableProjects.Count)
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
                this.BindStrict(this.ViewModel, vm => vm.ProjectSubpath, view => view.ProjectsPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableProjects, view => view.ProjectsPickerBox.ItemsSource)
                    .DisposeWith(disposable);

                // Set project picker tooltips
                this.WhenAnyValue(x => x.ViewModel!.SelectedProjectPath.ErrorState)
                    .Select(e =>
                    {
                        if (e.Succeeded) return "Project in the solution to run";
                        return $"Project in the solution to run was invalid: {e.Reason}";
                    })
                    .BindToStrict(this, x => x.ProjectsPickerBox.ToolTip)
                    .DisposeWith(disposable);

                // Set up open solution button
                this.WhenAnyValue(x => x.ViewModel!.OpenSolutionCommand)
                    .BindToStrict(this, x => x.OpenSolutionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.VersioningOptions)
                    .BindTo(this, view => view.PreferredVersioningPicker.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.VisibilityOptions)
                    .BindTo(this, view => view.VisibilityOptionPicker.ItemsSource)
                    .DisposeWith(disposable);


                // Bind settings
                this.BindStrict(this.ViewModel, vm => vm.LongDescription, view => view.DescriptionBox.Text)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.ShortDescription, view => view.OneLineDescriptionBox.Text)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.Visibility, view => view.VisibilityOptionPicker.SelectedItem)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.Versioning, view => view.PreferredVersioningPicker.SelectedItem)
                    .DisposeWith(disposable);

                #region Required Mods
                this.BindStrict(this.ViewModel, vm => vm.AddRequiredModInput, view => view.RequiredMods.AddRequiredModBox.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RequiredModsDisplay)
                    .BindToStrict(this, v => v.RequiredMods.RequiredModsList.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.AddRequiredModCommand)
                    .BindToStrict(this, x => x.RequiredMods.AddRequiredModButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.DetectedMods)
                    .BindToStrict(this, v => v.RequiredMods.DetectedModsList.ItemsSource)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.DetectedModsSearch, view => view.RequiredMods.SearchBox.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.ClearSearchCommand)
                    .BindToStrict(this, x => x.RequiredMods.ClearSearchButton.Command)
                    .DisposeWith(disposable);
                #endregion
            });
        }
    }
}
