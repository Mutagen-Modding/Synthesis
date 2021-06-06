using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive;
using System;
using Noggog;
using System.Windows.Controls.Primitives;
using System.Linq;
using DynamicData;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Views
{
    public class SolutionInitViewBase : NoggogUserControl<SolutionPatcherInitVM> { }

    /// <summary>
    /// Interaction logic for SolutionInitView.xaml
    /// </summary>
    public partial class SolutionInitView : SolutionInitViewBase
    {
        public SolutionInitView()
        {
            InitializeComponent();
            this.WhenActivated(dispose =>
            {
                // Hide help box if not in initialization
                UtilityBindings.HelpWiring(this.ViewModel!.Profile.Config, this.HelpButton, this.HelpText)
                    .DisposeWith(dispose);

                this.BindStrict(this.ViewModel, vm => vm.SelectedIndex, view => view.TopTab.SelectedIndex)
                    .DisposeWith(dispose);

                // Bind solution existing pane
                this.WhenAnyValue(x => x.ViewModel!.ExistingSolution.SolutionPath)
                    .BindToStrict(this, x => x.SolutionPathPicker.PickerVM)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.ExistingSolution.ProjectName, view => view.ExistingProjectNameBox.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.ExistingSolution.ProjectError)
                    .BindError(this.ExistingProjectNameBox)
                    .DisposeWith(dispose);

                // Bind new pane
                this.WhenAnyValue(x => x.ViewModel!.New.ParentDirPath)
                    .BindToStrict(this, x => x.ParentDirPicker.PickerVM)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.New.SolutionName, view => view.SolutionNameBox.Text)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.New.ProjectName, view => view.NewProjectNameBox.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.New.SolutionPath)
                    .BindError(this.SolutionNameBox)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.New.ProjectError)
                    .BindError(this.NewProjectNameBox)
                    .DisposeWith(dispose);

                // Bind both existing pane
                this.WhenAnyValue(x => x.ViewModel!.ExistingProject.SolutionPath)
                    .BindToStrict(this, x => x.BothExistingSolutionPathPicker.PickerVM)
                    .DisposeWith(dispose);
                var vis = this.WhenAnyValue(x => x.ViewModel!.ExistingProject.SolutionPath.ErrorState)
                    .Select(x => x.Succeeded ? Visibility.Visible : Visibility.Collapsed);
                vis.BindToStrict(this, x => x.AvailableProjects.Visibility)
                    .DisposeWith(dispose);
                vis.BindToStrict(this, x => x.AvailableProjectsText.Visibility)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.ExistingProject.AvailableProjects)
                    .BindToStrict(this, x => x.AvailableProjects.ItemsSource)
                    .DisposeWith(dispose);
                this.AvailableProjects.Events().SelectionChanged
                    .Throttle(TimeSpan.FromMilliseconds(300), RxApp.MainThreadScheduler)
                    .WithLatestFrom(
                        this.WhenAnyValue(x => x.ViewModel),
                        (change, vm) => (change, vm))
                    .Subscribe(u =>
                    {
                        u.vm?.ExistingProject.SelectedProjects.Clear();
                        u.vm?.ExistingProject.SelectedProjects.AddRange(this.AvailableProjects.SelectedItems.Cast<string>());
                    })
                    .DisposeWith(dispose);

                // Bind open after checkbox
                this.BindStrict(this.ViewModel, vm => vm.OpenCodeAfter, view => view.OpenCodeAfter.IsChecked)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.IdeOptions)
                    .BindToStrict(this, view => view.OpenWithComboBox.ItemsSource)
                    .DisposeWith(dispose);
                this.BindStrict(ViewModel, vm => vm.Ide, view => view.OpenWithComboBox.SelectedValue)
                    .DisposeWith(dispose);

                // Set up discard/confirm clicks
                this.WhenAnyValue(x => x.ViewModel!.Init.CancelConfiguration)
                    .BindToStrict(this, x => x.CancelAdditionButton.Command)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel!.Init.CompleteConfiguration)
                    .BindToStrict(this, x => x.ConfirmButton.ConfirmAdditionButton.Command)
                    .DisposeWith(dispose);
            });
        }
    }
}
