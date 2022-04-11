using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System;
using System.Windows.Controls.Primitives;
using System.Linq;
using DynamicData;
using System.Windows;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;

namespace Synthesis.Bethesda.GUI.Views;

public class SolutionInitViewBase : NoggogUserControl<SolutionPatcherInitVm> { }

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
            UtilityBindings.HelpWiring(this.ViewModel!.ShowHelpSetting, this.HelpButton, this.HelpText)
                .DisposeWith(dispose);

            this.Bind(this.ViewModel, vm => vm.SelectedIndex, view => view.TopTab.SelectedIndex)
                .DisposeWith(dispose);

            // Bind solution existing pane
            this.WhenAnyValue(x => x.ViewModel!.ExistingSolution.SolutionPath)
                .BindTo(this, x => x.SolutionPathPicker.PickerVM)
                .DisposeWith(dispose);
            this.Bind(this.ViewModel, vm => vm.ExistingSolution.ProjectName, view => view.ExistingProjectNameBox.Text)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.ExistingSolution.ProjectError)
                .BindError(this.ExistingProjectNameBox)
                .DisposeWith(dispose);

            // Bind new pane
            this.WhenAnyValue(x => x.ViewModel!.New.ParentDirPath)
                .BindTo(this, x => x.ParentDirPicker.PickerVM)
                .DisposeWith(dispose);
            this.Bind(this.ViewModel, vm => vm.New.SolutionName, view => view.SolutionNameBox.Text)
                .DisposeWith(dispose);
            this.Bind(this.ViewModel, vm => vm.New.ProjectName, view => view.NewProjectNameBox.Text)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.New.SolutionPath)
                .BindError(this.SolutionNameBox)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.New.ProjectError)
                .BindError(this.NewProjectNameBox)
                .DisposeWith(dispose);

            // Bind both existing pane
            this.WhenAnyValue(x => x.ViewModel!.ExistingProject.SolutionPath)
                .BindTo(this, x => x.BothExistingSolutionPathPicker.PickerVM)
                .DisposeWith(dispose);
            var vis = this.WhenAnyValue(x => x.ViewModel!.ExistingProject.SolutionPath.ErrorState)
                .Select(x => x.Succeeded ? Visibility.Visible : Visibility.Collapsed);
            vis.BindTo(this, x => x.AvailableProjects.Visibility)
                .DisposeWith(dispose);
            vis.BindTo(this, x => x.AvailableProjectsText.Visibility)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.ExistingProject.AvailableProjects)
                .BindTo(this, x => x.AvailableProjects.ItemsSource)
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
            this.Bind(this.ViewModel, vm => vm.OpenCodeAfter, view => view.OpenCodeAfter.IsChecked)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.IdeOptions)
                .BindTo(this, view => view.OpenWithComboBox.ItemsSource)
                .DisposeWith(dispose);
            this.Bind(ViewModel, vm => vm.Ide, view => view.OpenWithComboBox.SelectedValue)
                .DisposeWith(dispose);

            // Set up discard/confirm clicks
            this.WhenAnyValue(x => x.ViewModel!.CancelConfiguration)
                .BindTo(this, x => x.CancelAdditionButton.Command)
                .DisposeWith(dispose);
            this.WhenAnyValue(x => x.ViewModel!.CompleteConfiguration)
                .BindTo(this, x => x.ConfirmButton.ConfirmAdditionButton.Command)
                .DisposeWith(dispose);
        });
    }
}