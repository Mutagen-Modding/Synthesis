using System.IO.Abstractions;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.CLI.Common;

public class PipelineSettingsModifier
{
    private readonly IFileSystem _fileSystem;
    private readonly IPipelineSettingsPath _pipelineSettingsPath;
    private readonly IPipelineSettingsV2Reader _pipelineSettingsV2Reader;
    private readonly IPipelineSettingsExporter _pipelineSettingsExporter;

    public PipelineSettingsModifier(
        IFileSystem fileSystem,
        IPipelineSettingsPath pipelineSettingsPath,
        IPipelineSettingsV2Reader pipelineSettingsV2Reader,
        IPipelineSettingsExporter pipelineSettingsExporter)
    {
        _fileSystem = fileSystem;
        _pipelineSettingsPath = pipelineSettingsPath;
        _pipelineSettingsV2Reader = pipelineSettingsV2Reader;
        _pipelineSettingsExporter = pipelineSettingsExporter;
    }

    public async Task DoModification(DirectoryPath settingsPath, Func<PipelineSettings, FilePath, Task> toDo)
    {
        var pipelineSettingsPath = Path.Combine(settingsPath, _pipelineSettingsPath.Name);
        PipelineSettings settings;
        if (_fileSystem.File.Exists(pipelineSettingsPath))
        {
            settings = _pipelineSettingsV2Reader.Read(pipelineSettingsPath);
        }
        else
        {
            settings = new PipelineSettings();
        }

        await toDo(settings, pipelineSettingsPath);
        
        _pipelineSettingsExporter.Write(pipelineSettingsPath, settings);
    }
}