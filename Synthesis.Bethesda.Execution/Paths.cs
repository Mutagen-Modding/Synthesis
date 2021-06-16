using System;
using System.IO;

namespace Synthesis.Bethesda.Execution
{
    public interface IPaths
    {
        string SettingsFileName { get; }
        string TypicalExtraData { get; }
        string WorkingDirectory { get; }
        string LoadingFolder { get; }
        string ProfileWorkingDirectory(string id);
    }

    public class Paths : IPaths
    {
        public string SettingsFileName { get; } = "PipelineSettings.json";
        public string TypicalExtraData { get; } = Path.Combine(Environment.CurrentDirectory, "Data");
        public string WorkingDirectory { get; } = Path.Combine(Path.GetTempPath(), "Synthesis")!;
        public string LoadingFolder { get; }
        public string ProfileWorkingDirectory(string id) => Path.Combine(WorkingDirectory, id, "Workspace");

        public Paths()
        {
            LoadingFolder = Path.Combine(WorkingDirectory, "Loading");;
        }
    }
}
