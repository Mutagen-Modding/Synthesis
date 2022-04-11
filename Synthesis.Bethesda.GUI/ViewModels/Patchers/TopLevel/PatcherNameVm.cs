using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

public interface IPatcherNameVm : IPatcherNameProvider
{
    public string Nickname { get; set; }
}