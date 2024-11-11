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
    private readonly IProfileDefinitionPathProvider _profileDefinitionPathProvider;
    private readonly IPipelineSettingsV2Reader _pipelineSettingsV2Reader;
    private readonly IPipelineSettingsExporter _pipelineSettingsExporter;

    public PipelineSettingsModifier(
        IFileSystem fileSystem,
        IProfileDefinitionPathProvider profileDefinitionPathProvider,
        IPipelineSettingsV2Reader pipelineSettingsV2Reader,
        IPipelineSettingsExporter pipelineSettingsExporter)
    {
        _fileSystem = fileSystem;
        _profileDefinitionPathProvider = profileDefinitionPathProvider;
        _pipelineSettingsV2Reader = pipelineSettingsV2Reader;
        _pipelineSettingsExporter = pipelineSettingsExporter;
    }

    public async Task DoModification(Func<PipelineSettings, FilePath, Task> toDo)
    {
        var pipelineSettingsPath = _profileDefinitionPathProvider.Path;
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