using System.IO.Abstractions;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class PipelineSettingsProvider : IDotNetPathSettingsProvider
{
    public Lazy<IPipelineSettings> Settings { get; }

    public PipelineSettingsProvider(
        IFileSystem fileSystem,
        IPipelineSettingsV2Reader pipelineSettingsV2Reader,
        RunPatcherPipelineCommand command)
    {
        Settings = new Lazy<IPipelineSettings>(() =>
        {
            var pipelineSettingsPath = command.PipelineSettingsPath;
            
            if (!fileSystem.File.Exists(pipelineSettingsPath))
            {
                throw new FileNotFoundException("Could not find settings", pipelineSettingsPath);
            }
            return pipelineSettingsV2Reader.Read(pipelineSettingsPath);

        });
    }
    
    public string DotNetPathOverride => Settings.Value.DotNetPathOverride;
}