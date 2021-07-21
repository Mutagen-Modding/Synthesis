using Noggog.WPF;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Cli
{
    public interface IPathToExecutableInputVm
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
    }
}