﻿using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive;
using System;
using Noggog;

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
                UtilityBindings.HelpWiring(this.ViewModel.Patcher, this.HelpButton, this.HelpText)
                    .DisposeWith(dispose);

                this.BindStrict(this.ViewModel, vm => vm.SelectedIndex, view => view.TopTab.SelectedIndex)
                    .DisposeWith(dispose);

                // Bind solution existing pane
                this.WhenAnyValue(x => x.ViewModel.ExistingSolution.SolutionPath)
                    .BindToStrict(this, x => x.SolutionPathPicker.PickerVM)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.ExistingSolution.ProjectName, view => view.ExistingProjectNameBox.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel.ExistingSolution.ProjectError)
                    .BindError(this.ExistingProjectNameBox)
                    .DisposeWith(dispose);

                // Bind new pane
                this.WhenAnyValue(x => x.ViewModel.New.ParentDirPath)
                    .BindToStrict(this, x => x.ParentDirPicker.PickerVM)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.New.SolutionName, view => view.SolutionNameBox.Text)
                    .DisposeWith(dispose);
                this.BindStrict(this.ViewModel, vm => vm.New.ProjectName, view => view.NewProjectNameBox.Text)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel.New.SolutionPath)
                    .BindError(this.SolutionNameBox)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel.New.ProjectError)
                    .BindError(this.NewProjectNameBox)
                    .DisposeWith(dispose);

                // Bind both existing pane
                this.WhenAnyValue(x => x.ViewModel.ExistingProject.SolutionPath)
                    .BindToStrict(this, x => x.BothExistingSolutionPathPicker.PickerVM)
                    .DisposeWith(dispose);
                this.WhenAnyValue(x => x.ViewModel.ExistingProject.ProjectPath)
                    .BindToStrict(this, x => x.BothExistingProjectPathPicker.PickerVM)
                    .DisposeWith(dispose);

                // Bind open after checkbox
                /*this.BindStrict(this.ViewModel, vm => vm.OpenVsAfter, view => view.OpenVsAfter.IsChecked)
                    .DisposeWith(dispose);*/

                this.BindStrict(ViewModel, vm => vm.OpenWith, view => view.OpenWithComboBox.SelectedValue)
                    .DisposeWith(dispose);
            });
        }
    }
}
