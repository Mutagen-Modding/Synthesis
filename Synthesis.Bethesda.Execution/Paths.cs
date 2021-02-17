using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public static class Paths
    {
        public const string SettingsFileName = "PipelineSettings.json";
        public readonly static string TypicalExtraData = Path.Combine(Environment.CurrentDirectory, "Data");
        public readonly static string WorkingDirectory = Path.Combine(Path.GetTempPath(), "Synthesis")!;
        public static string ProfileWorkingDirectory(string id) => Path.Combine(WorkingDirectory, id, "Workspace");
    }
}
