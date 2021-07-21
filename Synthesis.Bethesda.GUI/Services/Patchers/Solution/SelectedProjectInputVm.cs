using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog.WPF;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution
{
    public interface ISelectedProjectInputVm
    {
        PathPickerVM Picker { get; }
    }

    public class SelectedProjectInputVm : ViewModel, ISelectedProjectInputVm
    {
        public PathPickerVM Picker { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public SelectedProjectInputVm()
        {
            Picker.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));
        }
    }
}