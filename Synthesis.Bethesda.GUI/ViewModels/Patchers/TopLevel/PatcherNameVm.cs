using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel
{
    public interface IPatcherNameVm : IPatcherNameProvider
    {
        new string Name { get; }
    }
}