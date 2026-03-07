using System.Reactive.Linq;
using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

public interface ISolutionPathInputVm : ISolutionFilePathFollower
{
    PathPickerVM Picker { get; }
}

public class SolutionPathInputVm : ViewModel, ISolutionPathInputVm
{
    public PathPickerVM Picker { get; }

    public SolutionPathInputVm(
        ISchedulerProvider schedulerProvider)
    {
        Picker = new PathPickerVM(schedulerProvider)
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };
        Picker.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));
    }

    public IObservable<FilePath> Path => Picker.WhenAnyValue(x => x.TargetPath)
        .Select(x => new FilePath(x));
}