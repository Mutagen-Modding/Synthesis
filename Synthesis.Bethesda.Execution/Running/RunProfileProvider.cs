using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Json;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running
{
    public interface IRunProfileProvider
    {
        ISynthesisProfileSettings Profile { get; }
    }

    public class RunProfileProvider : IRunProfileProvider
    {
        private readonly Lazy<ISynthesisProfileSettings> _profile;

        public ISynthesisProfileSettings Profile => _profile.Value;
        
        public RunProfileProvider(
            IFileSystem fileSystem,
            IProfileNameProvider profileNameProvider,
            IPipelineSettingsImporter pipelineSettingsImporter,
            ISynthesisProfileImporter synthProfileImporter,
            IProfileDefinitionPathProvider profileDefinitionPathProvider)
        {
            _profile = new Lazy<ISynthesisProfileSettings>(() =>
            {
                // Locate profile
                if (!fileSystem.File.Exists(profileDefinitionPathProvider.Path))
                {
                    throw new FileNotFoundException("Could not locate profile to run", profileDefinitionPathProvider.Path);
                }
                
                ISynthesisProfileSettings? profile;
                if (string.IsNullOrWhiteSpace(profileNameProvider.Name))
                {
                    profile = synthProfileImporter.Import(profileDefinitionPathProvider.Path);
                }
                else
                {
                    var settings = pipelineSettingsImporter.Import(profileDefinitionPathProvider.Path);
                    profile = settings.Profiles.FirstOrDefault(profile =>
                    {
                        if (profileNameProvider.Name.Equals(profile.Nickname)) return true;
                        if (profileNameProvider.Name.Equals(profile.ID)) return true;
                        return false;
                    });
                }

                if (string.IsNullOrWhiteSpace(profile?.ID))
                {
                    throw new ArgumentException("File did not point to a valid profile");
                }

                return profile;
            });
        }
    }
}