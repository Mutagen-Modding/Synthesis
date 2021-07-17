using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers
{
    public interface IPatcherNameVm : IPatcherNameProvider
    {
        new string Name { get; set; }
    }
    
    public class PatcherNameVm : ViewModel, IPatcherNameVm
    {
        [Reactive]
        public string Name { get; set; } = string.Empty;
    }
}