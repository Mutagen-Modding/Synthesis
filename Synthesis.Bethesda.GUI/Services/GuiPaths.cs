using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IGuiPaths
    {
        string GuiSettingsPath { get; }
        string RegistryFolder { get; }
    }

    public class GuiPaths : IGuiPaths
    {
        public string GuiSettingsPath { get; } = "GuiSettings.json";
        public string RegistryFolder { get; }

        public GuiPaths(IPaths paths)
        {
            RegistryFolder = Path.Combine(paths.WorkingDirectory, "Registry");
        }
    }
}
