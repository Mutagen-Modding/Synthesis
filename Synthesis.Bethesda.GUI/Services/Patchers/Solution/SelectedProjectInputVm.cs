using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using Noggog;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Solution;

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
            IProjectPathConstructor pathConstructor,
            ISolutionFilePathFollower solutionFilePathFollower)
        {
            Picker.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));
            
            this.WhenAnyValue(x => x.ProjectSubpath)
                // Need to throttle, as bindings flip to null quickly, which we want to skip
                .NotNull()
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .CombineLatest(solutionFilePathFollower.Path.DistinctUntilChanged(),
                    (subPath, slnPath) => pathConstructor.Construct(slnPath, subPath))
                .Subscribe(p =>
                {
                    logger.Information($"Setting target project path to: {p}");
                    Picker.TargetPath = p;
                })
                .DisposeWith(this);
        }
    }
}