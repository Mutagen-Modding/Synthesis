using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.Execution.Running.Cli
{
    public interface IPipelineProfilesProvider
    {
        IEnumerable<ISynthesisProfileSettings> Get();
    }

    public class PipelineProfilesProvider : IPipelineProfilesProvider
    {
        public IPipelineSettingsImporter PipelineSettingsImporter { get; }
        public IProfileDefinitionPathProvider ProfileDefinitionPathProvider { get; }
        private readonly IFileSystem _fileSystem;

        public PipelineProfilesProvider(
            IFileSystem fileSystem,
            IPipelineSettingsImporter pipelineSettingsImporter,
            IProfileDefinitionPathProvider profileDefinitionPathProvider)
        {
            PipelineSettingsImporter = pipelineSettingsImporter;
            ProfileDefinitionPathProvider = profileDefinitionPathProvider;
            _fileSystem = fileSystem;
        }
        
        public IEnumerable<ISynthesisProfileSettings> Get()
        {
            // Locate profile
            if (!_fileSystem.File.Exists(ProfileDefinitionPathProvider.Path))
            {
                throw new FileNotFoundException("Could not locate profile to run", ProfileDefinitionPathProvider.Path);
            }
            
            return PipelineSettingsImporter.Import(ProfileDefinitionPathProvider.Path).Profiles;
        }
    }
}