using System.Collections.Generic;
using Mutagen.Bethesda;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface ISynthesisProfileSettings : IProfileIdentifier
    {
        string Nickname { get; set; }
        new string ID { get; set; }
        GameRelease TargetRelease { get; set; }
        public List<PatcherGroupSettings> Groups { get; set; }
        NugetVersioningEnum MutagenVersioning { get; set; }
        string? MutagenManualVersion { get; set; }
        NugetVersioningEnum SynthesisVersioning { get; set; }
        string? SynthesisManualVersion { get; set; }
        string? DataPathOverride { get; set; }
        bool ConsiderPrereleaseNugets { get; set; }
        bool LockToCurrentVersioning { get; set; }
        PersistenceMode Persistence { get; set; }
        bool IgnoreMissingMods { get; set; }
    }
}