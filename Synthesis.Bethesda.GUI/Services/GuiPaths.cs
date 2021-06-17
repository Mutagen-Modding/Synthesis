using System.IO;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IGuiPaths
    {
        string RegistryFolder { get; }
    }

    public class GuiPaths : IGuiPaths
    {
        public string RegistryFolder { get; }

        public GuiPaths(
            IProvideWorkingDirectory workingDirectory)
        {
            RegistryFolder = Path.Combine(workingDirectory.WorkingDirectory, "Registry");
        }
    }
}
