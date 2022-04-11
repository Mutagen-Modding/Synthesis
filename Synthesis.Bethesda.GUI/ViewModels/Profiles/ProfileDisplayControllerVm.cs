using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public interface IProfileDisplayControllerVm
{
    ViewModel? SelectedObject { get; set; }
}

public class ProfileDisplayControllerVm : ViewModel, IProfileDisplayControllerVm
{
    private bool _midSwap;

    private ViewModel? _selectedObject;
    public ViewModel? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (_midSwap) return;
            _midSwap = true;
            this.RaiseAndSetIfChanged(ref _selectedObject, value);
            _midSwap = false;
        } 
    }
}