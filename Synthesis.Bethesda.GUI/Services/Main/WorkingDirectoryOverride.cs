using System;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services.Main
{
    public class WorkingDirectoryProviderOverride : IWorkingDirectoryProvider
    {
        public DirectoryPath WorkingDirectory { get; }
        
        public WorkingDirectoryProviderOverride(
            Func<WorkingDirectoryProvider> workingDir,
            ISettingsSingleton settings)
        {
            WorkingDirectory = settings.Gui.WorkingDirectory.IsNullOrWhitespace() ? workingDir().WorkingDirectory : settings.Gui.WorkingDirectory;
        }
    }
}