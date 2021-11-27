using Noggog;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public class RunDisplayControllerVm : ViewModel
    {
        private bool _midSwap;

        private IRunItem? _selectedObject;
        public IRunItem? SelectedObject
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

        public RunDisplayControllerVm()
        {
            this.WhenAnyValue(x => x.SelectedObject)
                .WireSelectionTracking()
                .DisposeWith(this);
        }
    }
}