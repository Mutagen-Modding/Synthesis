using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog.WPF;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution
{
    public interface ISolutionPathInputVm
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
    }
}