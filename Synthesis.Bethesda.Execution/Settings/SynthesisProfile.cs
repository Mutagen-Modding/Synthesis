using Mutagen.Bethesda;
using System.Collections.Generic;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class SynthesisProfile
    {
        public string Nickname = string.Empty;
        public string ID = string.Empty;
        public GameRelease TargetRelease;
        public List<PatcherSettings> Patchers = new List<PatcherSettings>();
        public NugetVersioningEnum MutagenVersioning = NugetVersioningEnum.Manual;
        public string? MutagenManualVersion;
        public NugetVersioningEnum SynthesisVersioning = NugetVersioningEnum.Manual;
        public string? SynthesisManualVersion;
        public string? DataPathOverride;
        public bool ConsiderPrereleaseNugets;
        public bool LockToCurrentVersioning;
    }
}
