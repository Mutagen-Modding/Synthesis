using System;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public interface ISolutionPathInputVm : ISolutionFilePathFollower
    {
        PathPickerVM Picker { get; }
    }

    public class SolutionPathInputVm : ViewModel, ISolutionPathInputVm
    {
        public PathPickerVM Picker { get; } = new()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public SolutionPathInputVm()
        {
            Picker.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));
        }

        public IObservable<FilePath> Path => Picker.WhenAnyValue(x => x.TargetPath)
            .Select(x => new FilePath(x));
    }
}