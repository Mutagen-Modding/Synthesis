using Mutagen.Bethesda;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface ISynthesisProfile
    {
        string Nickname { get; set; }
        string ID { get; set; }
        GameRelease TargetRelease { get; set; }
        List<PatcherSettings> Patchers { get; set; }
        NugetVersioningEnum MutagenVersioning { get; set; }
        string? MutagenManualVersion { get; set; }
        NugetVersioningEnum SynthesisVersioning { get; set; }
        string? SynthesisManualVersion { get; set; }
        string? DataPathOverride { get; set; }
        bool ConsiderPrereleaseNugets { get; set; }
        bool LockToCurrentVersioning { get; set; }
        PersistenceMode Persistence { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SynthesisProfile : ISynthesisProfile
    {
        public string Nickname { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
        public GameRelease TargetRelease { get; set; }
        public List<PatcherSettings> Patchers { get; set; } = new();
        public NugetVersioningEnum MutagenVersioning { get; set; } = NugetVersioningEnum.Manual;
        public string? MutagenManualVersion { get; set; }
        public NugetVersioningEnum SynthesisVersioning { get; set; } = NugetVersioningEnum.Manual;
        public string? SynthesisManualVersion { get; set; }
        public string? DataPathOverride { get; set; }
        public bool ConsiderPrereleaseNugets { get; set; }
        public bool LockToCurrentVersioning { get; set; }
        public PersistenceMode Persistence { get; set; } = PersistenceMode.None;
    }
}