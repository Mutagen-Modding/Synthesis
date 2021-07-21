using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel
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