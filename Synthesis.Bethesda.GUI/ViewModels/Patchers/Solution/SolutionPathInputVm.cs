using System.Reactive.Linq;
using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog;
using Noggog.Reactive;
using Noggog.UI;
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
        ISchedulerProvider schedulerProvider,
        IPathPickerDialogProvider pathPickerDialogProvider)
    {
        Picker = new PathPickerVM(schedulerProvider, pathPickerDialogProvider)
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };
        Picker.Filters.Add(new DialogFileFilter("Solution", ".sln"));
    }

    public IObservable<FilePath> Path => Picker.WhenAnyValue(x => x.TargetPath)
        .Select(x => new FilePath(x));
}