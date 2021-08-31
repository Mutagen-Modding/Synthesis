using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IProfileDisplayControllerVm
    {
        ViewModel? SelectedObject { get; set; }
    }

    public class ProfileDisplayControllerVm : ViewModel, IProfileDisplayControllerVm
    {
        private bool _midSwap;

        private ViewModel? _SelectedObject;
        public ViewModel? SelectedObject
        {
            get => _SelectedObject;
            set
            {
                if (_midSwap) return;
                _midSwap = true;
                this.RaiseAndSetIfChanged(ref _SelectedObject, value);
                _midSwap = false;
            } 
        }
    }
}