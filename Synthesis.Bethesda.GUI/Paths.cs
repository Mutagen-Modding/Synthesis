using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public static class Paths
    {
        public const string GuiSettingsPath = "GuiSettings.json";
        public readonly static string LoadingFolder = Path.Combine(Synthesis.Bethesda.Execution.Paths.WorkingDirectory, "Loading");
    }
}
