using Noggog;
using Noggog.IO;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.GUI.Settings;

public class PipelineSettingsPathProvider : IPipelineSettingsPath
{
    private ICurrentDirectoryProvider _currentDirectoryProvider;
    private IPipelineSettingsNameProvider _pipelineSettingsNameProvider;

    public PipelineSettingsPathProvider(
        ICurrentDirectoryProvider currentDirectoryProvider, 
        IPipelineSettingsNameProvider pipelineSettingsNameProvider)
    {
        _currentDirectoryProvider = currentDirectoryProvider;
        _pipelineSettingsNameProvider = pipelineSettingsNameProvider;
    }

    public FilePath Path =>
        System.IO.Path.Combine(_currentDirectoryProvider.CurrentDirectory, _pipelineSettingsNameProvider.Name);
}