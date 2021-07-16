using System;
using System.IO.Abstractions;
using System.Linq;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running
{
    public interface IRunProfileProvider
    {
        ISynthesisProfile Profile { get; }
    }

    public class RunProfileProvider : IRunProfileProvider
    {
        private readonly Lazy<ISynthesisProfile> _profile;

        public ISynthesisProfile Profile => _profile.Value;
        
        public RunProfileProvider(
            IFileSystem fileSystem,
            IProfileNameProvider profileNameProvider,
            IProfileDefinitionPathProvider profileDefinitionPathProvider)
        {
            _profile = new Lazy<ISynthesisProfile>(() =>
            {
                ISynthesisProfile? profile;
                if (string.IsNullOrWhiteSpace(profileNameProvider.Name))
                {
                    profile = JsonConvert.DeserializeObject<SynthesisProfile>(
                        fileSystem.File.ReadAllText(profileDefinitionPathProvider.Path),
                        Constants.JsonSettings)!;
                }
                else
                {
                    var settings = JsonConvert.DeserializeObject<PipelineSettings>(
                        fileSystem.File.ReadAllText(profileDefinitionPathProvider.Path), 
                        Constants.JsonSettings)!;
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