using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;

namespace Synthesis.Bethesda.Execution.Settings.V2
{
    [ExcludeFromCodeCoverage]
    public class SynthesisProfile : ISynthesisProfileSettings
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

        GameRelease IGameReleaseContext.Release => TargetRelease;
    }
}