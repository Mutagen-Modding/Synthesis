using Noggog;
using Noggog.IO;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.GUI.Settings;

public class PipelineSettingsPathProvider : IPipelineSettingsPath
{
    private readonly ICurrentDirectoryProvider _currentDirectoryProvider;

    public PipelineSettingsPathProvider(ICurrentDirectoryProvider currentDirectoryProvider)
    {
        _currentDirectoryProvider = currentDirectoryProvider;
    }

    public FilePath Path =>
        System.IO.Path.Combine(_currentDirectoryProvider.CurrentDirectory, "PipelineSettings.json");
}