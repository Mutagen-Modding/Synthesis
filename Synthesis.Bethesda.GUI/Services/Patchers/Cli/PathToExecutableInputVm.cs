using Noggog;
using Noggog.Reactive;
using Noggog.UI;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Cli;

public interface IPathToExecutableInputVm : IPathToExecutableInputProvider
{
    PathPickerVM Picker { get; }
}

public class PathToExecutableInputVm : ViewModel, IPathToExecutableInputVm
{
    public PathPickerVM Picker { get; }

    public FilePath Path => Picker.TargetPath;

    public PathToExecutableInputVm(
        ISchedulerProvider schedulerProvider,
        IPathPickerDialogProvider pathPickerDialogProvider,
        CliPatcherSettings settings)
    {
        Picker = new(schedulerProvider, pathPickerDialogProvider)
        {
            PathType = PathPickerVM.PathTypeOptions.File,
            ExistCheckOption = PathPickerVM.CheckOptions.On,
        };
        Picker.TargetPath = settings.PathToExecutable;
    }
}