using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public interface ISelectedProfileControllerVm
    {
        ProfileVm? SelectedProfile { get; set; }
    }

    public class SelectedProfileControllerVm : ViewModel, ISelectedProfileControllerVm
    {
        [Reactive]
        public ProfileVm? SelectedProfile { get; set; }
    }
}