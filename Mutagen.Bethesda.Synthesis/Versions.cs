using Noggog;
using System;
using System.Diagnostics;
using BaseSynthesis = Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis
{
    public static class Versions
    {
        public static string MutagenVersion => FileVersionInfo.GetVersionInfo(typeof(FormKey).Assembly.Location)!.ProductVersion.TrimEnd(".0").TrimEnd(".0");
        public static string SynthesisVersion => FileVersionInfo.GetVersionInfo(typeof(BaseSynthesis.Constants).Assembly.Location)!.ProductVersion.TrimEnd(".0").TrimEnd(".0");
    }
}
