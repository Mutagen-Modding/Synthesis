using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Cli
{
    public interface ICliInputSourceVm
    {
        IPathToExecutableInputVm ExecutableInput { get; }
        IShowHelpSetting ShowHelpSetting { get; }
    }
}