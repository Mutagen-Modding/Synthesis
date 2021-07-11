using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public interface IActivePanelControllerVm
    {
        ViewModel? ActivePanel { get; set; }
    }

    public class ActivePanelControllerVm : ViewModel, IActivePanelControllerVm
    {
        [Reactive]
        public ViewModel? ActivePanel { get; set; }
    }
}