using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog.WPF;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Git
{
    public interface IGitSelectedProjectInputVm
    {
        PathPickerVM Picker { get; }
    }

    public class GitSelectedProjectInputVm : ViewModel, IGitSelectedProjectInputVm
    {
        public PathPickerVM Picker { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public GitSelectedProjectInputVm()
        {
            Picker.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));
        }
    }
}