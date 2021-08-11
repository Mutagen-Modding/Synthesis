using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Synthesis.Bethesda.Execution.Json;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Cli
{
    public interface IRunProfileProvider
    {
        ISynthesisProfileSettings Get();
    }

    public class RunProfileProvider : IRunProfileProvider
    {
        public IProfileNameProvider ProfileNameProvider { get; }
        public IPipelineSettingsImporter PipelineSettingsImporter { get; }
        public ISynthesisProfileImporter SynthProfileImporter { get; }
        public IProfileDefinitionPathProvider ProfileDefinitionPathProvider { get; }
        private readonly IFileSystem _fileSystem;

        public RunProfileProvider(
            IFileSystem fileSystem,
            IProfileNameProvider profileNameProvider,
            IPipelineSettingsImporter pipelineSettingsImporter,
            ISynthesisProfileImporter synthProfileImporter,
            IProfileDefinitionPathProvider profileDefinitionPathProvider)
        {
            ProfileNameProvider = profileNameProvider;
            PipelineSettingsImporter = pipelineSettingsImporter;
            SynthProfileImporter = synthProfileImporter;
            ProfileDefinitionPathProvider = profileDefinitionPathProvider;
            _fileSystem = fileSystem;
        }

        public ISynthesisProfileSettings Get()
        {
            // Locate profile
            if (!_fileSystem.File.Exists(ProfileDefinitionPathProvider.Path))
            {
                throw new FileNotFoundException("Could not locate profile to run", ProfileDefinitionPathProvider.Path);
            }
                
            ISynthesisProfileSettings? profile;
            if (string.IsNullOrWhiteSpace(ProfileNameProvider.Name))
            {
                profile = SynthProfileImporter.Import(ProfileDefinitionPathProvider.Path);
            }
            else
            {
                var settings = PipelineSettingsImporter.Import(ProfileDefinitionPathProvider.Path);
                profile = settings.Profiles.FirstOrDefault(profile =>
                {
                    if (ProfileNameProvider.Name.Equals(profile.Nickname)) return true;
                    if (ProfileNameProvider.Name.Equals(profile.ID)) return true;
                    return false;
                });
            }

            if (string.IsNullOrWhiteSpace(profile?.ID))
            {
                throw new ArgumentException("File did not point to a valid profile");
            }

            return profile;
        }
    }
}