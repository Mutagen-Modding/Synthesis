using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog.WPF;
using ReactiveUI;
using System;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution
{
    public interface ISelectedProjectInputVm
    {
        string ProjectSubpath { get; set; }
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
            ILogger logger,
            ISolutionFilePathFollower solutionFilePathFollower,
            ISolutionProjectPath projectPath)
        {
            Picker.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));
            
            projectPath.Process(
                    solutionPath: solutionFilePathFollower.Path,
                    projectSubpath: this.WhenAnyValue(x => x.ProjectSubpath))
                .Subscribe(p =>
                {
                    logger.Information($"Setting target project path to: {p}");
                    Picker.TargetPath = p;
                })
                .DisposeWith(this);
        }
    }
}