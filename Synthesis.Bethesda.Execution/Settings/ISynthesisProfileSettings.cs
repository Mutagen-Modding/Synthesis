using System.Collections.Generic;
using Mutagen.Bethesda;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface ISynthesisProfileSettings : IProfileIdentifier
    {
        string Nickname { get; set; }
        new string ID { get; set; }
        GameRelease TargetRelease { get; set; }
        public List<PatcherSettings> Patchers { get; set; }
        NugetVersioningEnum MutagenVersioning { get; set; }
        string? MutagenManualVersion { get; set; }
        NugetVersioningEnum SynthesisVersioning { get; set; }
        string? SynthesisManualVersion { get; set; }
        string? DataPathOverride { get; set; }
        bool ConsiderPrereleaseNugets { get; set; }
        bool LockToCurrentVersioning { get; set; }
        PersistenceMode Persistence { get; set; }
    }
}