using System.IO;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface IGuiPaths
{
    string RegistryFolder { get; }
}

public class GuiPaths : IGuiPaths
{
    public string RegistryFolder { get; }

    public GuiPaths(
        IWorkingDirectoryProvider workingDirectoryProvider)
    {
        RegistryFolder = Path.Combine(workingDirectoryProvider.WorkingDirectory, "Registry");
    }
}