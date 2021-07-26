using Noggog;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.Patchers.Cli;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Cli
{
    public interface IPathToExecutableInputVm : IPathToExecutableInputProvider
    {
        PathPickerVM Picker { get; }
    }

    public class PathToExecutableInputVm : ViewModel, IPathToExecutableInputVm
    {
        public PathPickerVM Picker { get; } = new()
        {
            PathType = PathPickerVM.PathTypeOptions.File,
            ExistCheckOption = PathPickerVM.CheckOptions.On,
        };

        public FilePath Path => Picker.TargetPath;
    }
}