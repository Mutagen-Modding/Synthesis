using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI
{
    public interface ISelectedProfileControllerVm
    {
        ProfileVM? SelectedProfile { get; set; }
    }

    public class SelectedProfileControllerVm : ViewModel, ISelectedProfileControllerVm
    {
        [Reactive]
        public ProfileVM? SelectedProfile { get; set; }
    }
}