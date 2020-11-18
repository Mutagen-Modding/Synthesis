using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class SynthesisProfile
    {
        public string Nickname = string.Empty;
        public string ID = string.Empty;
        public GameRelease TargetRelease;
        public List<PatcherSettings> Patchers = new List<PatcherSettings>();
        public NugetVersioningEnum MutagenVersioning = NugetVersioningEnum.Match;
        public string ManualMutagenVersion = string.Empty;
        public NugetVersioningEnum SynthesisVersioning = NugetVersioningEnum.Manual;
        public string ManualSynthesisVersion = string.Empty;
    }
}
