using Noggog;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public class RunDisplayControllerVm : ViewModel
    {
        private bool _midSwap;

        private IRunItem? _SelectedObject;
        public IRunItem? SelectedObject
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

        public RunDisplayControllerVm()
        {
            this.WhenAnyValue(x => x.SelectedObject)
                .WireSelectionTracking()
                .DisposeWith(this);
        }
    }
}