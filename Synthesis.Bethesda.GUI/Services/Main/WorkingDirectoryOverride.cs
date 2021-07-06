using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services.Main
{
    public class WorkingDirectoryOverride : IProvideWorkingDirectory
    {
        public string WorkingDirectory { get; }
        
        public WorkingDirectoryOverride(
            ISettingsSingleton settings)
        {
            WorkingDirectory = settings.Gui.WorkingDirectory.IsNullOrWhitespace() ? new Execution.Pathing.ProvideWorkingDirectory().WorkingDirectory : settings.Gui.WorkingDirectory;
        }
    }
}