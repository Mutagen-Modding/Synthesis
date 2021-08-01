using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services.Main
{
    public class WorkingDirectoryProviderOverride : IWorkingDirectoryProvider
    {
        public DirectoryPath WorkingDirectory { get; }
        
        public WorkingDirectoryProviderOverride(
            ISettingsSingleton settings)
        {
            WorkingDirectory = settings.Gui.WorkingDirectory.IsNullOrWhitespace() ? new Execution.Pathing.WorkingDirectoryProvider().WorkingDirectory : settings.Gui.WorkingDirectory;
        }
    }
}