using System.IO.Abstractions;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public interface IPipelineProfilesProvider
{
    IEnumerable<ISynthesisProfileSettings> Get();
}

public interface IPipelineSettingsProvider
{
    IPipelineSettings Settings { get; }
}

public class PipelineProfilesProvider : IPipelineProfilesProvider, IPipelineSettingsProvider
{
    private readonly Lazy<IPipelineSettings> _settings;

    public IPipelineSettings Settings => _settings.Value;
    public IPipelineSettingsImporter PipelineSettingsImporter { get; }
    public IProfileDefinitionPathProvider ProfileDefinitionPathProvider { get; }

    public PipelineProfilesProvider(
        IFileSystem fileSystem,
        IPipelineSettingsImporter pipelineSettingsImporter,
        IProfileDefinitionPathProvider profileDefinitionPathProvider)
    {
        PipelineSettingsImporter = pipelineSettingsImporter;
        ProfileDefinitionPathProvider = profileDefinitionPathProvider;
        _settings = new Lazy<IPipelineSettings>(() =>
        {
            if (!fileSystem.File.Exists(ProfileDefinitionPathProvider.Path))
            {
                throw new FileNotFoundException("Could not locate pipeline settings to run", ProfileDefinitionPathProvider.Path);
            }
            
            return PipelineSettingsImporter.Import(ProfileDefinitionPathProvider.Path);
        });
    }
        
    public IEnumerable<ISynthesisProfileSettings> Get()
    {
        return _settings.Value.Profiles;
    }

    public override string ToString()
    {
        return nameof(PipelineProfilesProvider);
    }
}