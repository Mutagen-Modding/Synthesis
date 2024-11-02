using System.IO.Abstractions;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.CLI.Common;

public class GetOrCreatePipelineSettings
{
    private readonly IFileSystem _fileSystem;
    private readonly IPipelineSettingsPath _pipelineSettingsPath;
    private readonly IPipelineSettingsV2Reader _pipelineSettingsV2Reader;

    public GetOrCreatePipelineSettings(
        IFileSystem fileSystem,
        IPipelineSettingsPath pipelineSettingsPath,
        IPipelineSettingsV2Reader pipelineSettingsV2Reader)
    {
        _fileSystem = fileSystem;
        _pipelineSettingsPath = pipelineSettingsPath;
        _pipelineSettingsV2Reader = pipelineSettingsV2Reader;
    }

    public PipelineSettings GetWithin(DirectoryPath settingsPath)
    {
        var pipelineSettingsPath = Path.Combine(settingsPath, _pipelineSettingsPath.Name);
        if (!_fileSystem.File.Exists(pipelineSettingsPath))
        {
            return new PipelineSettings();
        }

        return _pipelineSettingsV2Reader.Read(pipelineSettingsPath);
    }
}