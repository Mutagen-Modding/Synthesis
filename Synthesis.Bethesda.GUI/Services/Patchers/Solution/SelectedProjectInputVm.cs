using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using Noggog;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution
{
    public interface ISelectedProjectInputVm : IProjectSubpathProvider
    {
        new string ProjectSubpath { get; set; }
        PathPickerVM Picker { get; }
    }

    public class SelectedProjectInputVm : ViewModel, ISelectedProjectInputVm
    {
        [Reactive]
        public string ProjectSubpath { get; set; } = string.Empty;
        
        public PathPickerVM Picker { get; } = new()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public SelectedProjectInputVm(
            IProjectPathConstructor pathConstructor,
            ISolutionFilePathFollower solutionFilePathFollower)
        {
            Picker.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));
            
            this.WhenAnyValue(x => x.ProjectSubpath)
                // Need to filter nulls, as bindings flip to null temporarily, which we want to skip
                .NotNull()
                .DistinctUntilChanged()
                .CombineLatest(solutionFilePathFollower.Path.DistinctUntilChanged(),
                    (subPath, slnPath) => pathConstructor.Construct(slnPath, subPath))
                .Subscribe(p =>
                {
                    Picker.TargetPath = p;
                })
                .DisposeWith(this);
        }
    }
}